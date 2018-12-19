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

namespace msb
{
    internal class Program
    {
        private const string SingleProjectArg = "-singleProject";
        private const string CacheRoundtripArg = "-buildWithCacheRoundtrip";
        private const string NoConsoleLoggerArg = "-noConsoleLogger";
        private const int MinimumArgumentCount = 4;

        private static readonly bool DebugBuild = false;
        private static bool _noConsoleLogger;
        private static int _executionTypeIndex;

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
            if (args.Length < MinimumArgumentCount || (args.Length > 0 && args[0].IndexOfAny(new[] {'h', '?'}) >= 0))
            {
                Console.WriteLine($"usage: <msbuild binaries root> [{NoConsoleLoggerArg}] {SingleProjectArg} <project file> <cache root>");
                Console.WriteLine($"usage: <msbuild binaries root> [{NoConsoleLoggerArg}] {CacheRoundtripArg} <cache root> <project root> [project file extension without dot]");
                return;
            }

            var msbuildBinaries = args[0];
            Trace.Assert(Directory.Exists(msbuildBinaries));
            MSBuildLocator.RegisterMSBuildPath(msbuildBinaries);

            _noConsoleLogger = args[1] == NoConsoleLoggerArg;

            //Debugger.Launch();

            _executionTypeIndex = _noConsoleLogger
                ? 2
                : 1;

            switch (args[_executionTypeIndex])
            {
                case SingleProjectArg:
                    BuildSingleProjectWithCaches(args);
                    break;
                case CacheRoundtripArg:
                    BuildWithCacheRoundtrip(args);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void BuildSingleProjectWithCaches(string[] args)
        {
            Trace.Assert(_executionTypeIndex + 2 == args.Length - 1);
            Trace.Assert(args[_executionTypeIndex] == SingleProjectArg);

            var projectFile = args[_executionTypeIndex + 1];
            var cacheRoot = args[_executionTypeIndex + 2];

            Trace.Assert(File.Exists(projectFile));
            Trace.Assert(Directory.Exists(cacheRoot));

            var cacheFiles = Directory.GetFiles(cacheRoot);

            BuildProject(projectFile, null, null, cacheFiles, null);
        }

        private static void BuildWithCacheRoundtrip(string[] args)
        {
            Trace.Assert(_executionTypeIndex + 2 <= args.Length - 1);
            Trace.Assert(args[_executionTypeIndex] == CacheRoundtripArg);

            var cacheRootIndex = _executionTypeIndex + 1;
            var projectRootIndex = _executionTypeIndex + 2;
            var projectExtensionIndex = _executionTypeIndex + 3;

            var cacheRoot = args[cacheRootIndex];
            var projectRoot = args[projectRootIndex];
            var projectFileExtension = projectExtensionIndex == args.Length - 1
                ? args[projectExtensionIndex]
                : "csproj";

            Trace.Assert(Directory.Exists(projectRoot), $"Directory does not exist: {projectRoot}");
            Trace.Assert(projectFileExtension[0] != '.');

            if (DebugBuild)
            {
                Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");
            }

            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, true);
            }

            Directory.CreateDirectory(cacheRoot);

            var projectFiles = Directory.GetFiles(projectRoot, $"*.{projectFileExtension}", SearchOption.AllDirectories);

            BuildGraphWithCacheFileRoundtrip(projectFiles, cacheRoot);
        }

        private static void BuildGraphWithCacheFileRoundtrip(IReadOnlyCollection<string> projectFiles, string cacheRoot)
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
                var outputCacheFile = graph.GraphRoots.Contains(node) ? null : Path.Combine(cacheRoot, node.CacheFileName());
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
            globalProperties = globalProperties ?? new Dictionary<string, string>();
            entryTargets = entryTargets ?? new string[0];

            using (var buildManager = new BuildManager())
            {
                var loggers = new List<ILogger>();

                if (!_noConsoleLogger)
                {
                    loggers.Add(new ConsoleLogger(LoggerVerbosity.Normal));
                }

                var buildParameters = new BuildParameters
                {
                    InputResultsCacheFiles = inputCachesFiles.ToArray(),
                    Loggers = loggers
                };

                if (outputCacheFile != null)
                {
                    buildParameters.OutputResultsCacheFile = outputCacheFile;
                }

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
