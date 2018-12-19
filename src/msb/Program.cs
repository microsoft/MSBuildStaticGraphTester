using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.Build.Utilities;

namespace msb
{
    internal class Program
    {
        private static readonly bool DebugBuild = false;

        private class EvaluationLogger : ILogger
        {
            public readonly ConcurrentDictionary<int, ConcurrentQueue<string>> LogsByEvaluationId = new ConcurrentDictionary<int, ConcurrentQueue<string>>();

            public void Initialize(IEventSource eventSource)
            {
                if (!DebugBuild)
                {
                    return;
                }

                var importsOfInterest = new[] {"microsoft.common.props", "microsoft.common.currentversion.targets", ""};

                eventSource.MessageRaised += (sender, args) =>
                {
                    if (args is ProjectImportedEventArgs importedEvent
                        && importedEvent.Message.StartsWith("Importing project")
                        && importsOfInterest.Any(i => importedEvent.ImportedProjectFile.ToLower().Contains(i))
                        )
                    {
                        AddMessageToQueue(
                            importedEvent.BuildEventContext.EvaluationId,
                            $"-<-<-< {importedEvent.ImportedProjectFile ?? ""}:\n{importedEvent.Message}");
                    }
                };

                eventSource.StatusEventRaised += (sender, args) =>
                {
                    if (args is ProjectEvaluationStartedEventArgs evaluationStarted)
                    {
                        AddMessageToQueue(evaluationStarted.BuildEventContext.EvaluationId, $"\t\t\t {evaluationStarted.ProjectFile}");
                    }
                };
            }

            public void Shutdown()
            {
                foreach (var kvp in LogsByEvaluationId)
                {
                    foreach (var message in kvp.Value)
                    {
                        Console.WriteLine(message);
                    }
                }

                LogsByEvaluationId.Clear();
            }

            public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Detailed;
            public string Parameters { get; set; }

            public void AddMessageToQueue(int id, string message)
            {
                LogsByEvaluationId.AddOrUpdate(
                    id,
                    i =>
                    {
                        var queue = new ConcurrentQueue<string>();
                        queue.Enqueue(message);

                        return queue;
                    },
                    (i, queue) =>
                    {
                        queue.Enqueue(message);

                        return queue;
                    }
                    );
            }
        }

        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("usage: <msbuild binaries root> <cache root> <project root>");
                return;
            }

            var msbuildBinaries = args[0];
            var cacheRoot = args[1];
            var projectRoot = args[2];

            Trace.Assert(Directory.Exists(projectRoot), $"Directory does not exist: {projectRoot}");

            MSBuildLocator.RegisterMSBuildPath(msbuildBinaries);

            if (DebugBuild)
            {
                Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");
            }

            var cacheDirectory = Path.Combine(cacheRoot, Path.GetFileName(projectRoot));

            if (Directory.Exists(cacheDirectory))
            {
                Directory.Delete(cacheDirectory, true);
            }

            Directory.CreateDirectory(cacheDirectory);

            var csprojes = Directory.GetFiles(projectRoot, "*.csproj", SearchOption.AllDirectories);

            BuildGraphWithCacheFileRountrip(csprojes, cacheRoot);
        }

        private static void BuildGraphWithCacheFileRountrip(IReadOnlyCollection<string> projectFiles, string cacheRoot)
        {
            ProjectGraph graph;

            using (var collection = new ProjectCollection())
            {
                collection.RegisterLogger(new EvaluationLogger());

                graph = new ProjectGraph(projectFiles, null, collection);
            }

            var cacheFiles = new Dictionary<ProjectGraphNode, string>(graph.ProjectNodes.Count);

            var entryPointTargets = graph.GetTargetLists(new[] {"Build"});

            var topoSortedNodes = graph.ProjectNodesTopologicallySorted;

            foreach (var node in topoSortedNodes)
            {
                var outputCacheFile = Path.Combine(cacheRoot, node.CacheFileName());
                var inputCachesFiles = node.ProjectReferences.Select(r => cacheFiles[r]);

                cacheFiles[node] = outputCacheFile;

                var entryTargets = entryPointTargets[node];

                PrintProjectInstanceContents(node);

                BuildProject(node.ProjectInstance.FullPath, node.GlobalProperties, entryTargets.ToArray(), inputCachesFiles, outputCacheFile);
            }
        }

        private static void BuildProject(
            string projectInstanceFullPath,
            IReadOnlyDictionary<string, string> globalProperties,
            string[] entryTargets,
            IEnumerable<string> inputCachesFiles,
            string outputCacheFile)
        {
            using (var buildManager = new BuildManager())
            {
                var buildParameters = new BuildParameters
                {
                    OutputResultsCacheFile = outputCacheFile,
                    InputResultsCacheFiles = inputCachesFiles.ToArray(),
                    Loggers = new[] { new ConsoleLogger(LoggerVerbosity.Normal) }
                };

                var buildRequestData = new BuildRequestData(
                    projectInstanceFullPath,
                    globalProperties.ToDictionary(k => k.Key, k => k.Value),
                    "Current",
                    entryTargets,
                    null);

                var result = buildManager.Build(buildParameters, buildRequestData);
            }
        }

        public static void PrintProjectInstanceContents(ProjectGraphNode node)
        {
            if (!DebugBuild)
            {
                return;
            }

            Console.WriteLine(
                $"->->-> {node.CacheFileName()}.ProjectReferenceTargetsForBuild = {node.ProjectInstance.GetProperty("ProjectReferenceTargetsForBuild").EvaluatedValue}");

            var projectReferenceTargetsItems = node.ProjectInstance.GetItems("ProjectReferenceTargets");
            Console.WriteLine($"->->-> ProjectReferenceTargets count: {projectReferenceTargetsItems.Count}");

            foreach (var item in projectReferenceTargetsItems)
            {
                Console.WriteLine($"->->-> {node.CacheFileName()}.{item.ItemType} = {item.EvaluatedInclude}");

                foreach (var metadata in item.Metadata)
                {
                    Console.WriteLine($"\t{metadata.Name}={metadata.EvaluatedValue}");
                }
            }
        }
    }

    internal static class ProjectGraphNodeExtensions
    {
        public static string CacheFileName(this ProjectGraphNode node)
        {
            return $"{Path.GetFileNameWithoutExtension(node.ProjectInstance.FullPath)}-{node.GetHashCode()}";
        }
    }
}
