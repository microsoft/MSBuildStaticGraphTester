using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Experimental.Graph;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace msb
{
    internal class Program
    {
        private const string SingleProjectArg = "-singleProject";
        private const string CacheRoundtripArg = "-buildWithCacheRoundtrip";
        private const string NoConsoleLoggerArg = "-noConsoleLogger";
        private const string BuildManagerArg = "-buildWithBuildManager";

        private static readonly bool DebugBuild = false;
        private static bool _noConsoleLogger;
        private static int _minimumArgumentCount;
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

        private static int Main(string[] args)
        {
            _minimumArgumentCount = 5;

            if (args.Length < _minimumArgumentCount || (args.Length > 0 && args[0].IndexOfAny(new[] {'h', '?'}) >= 0))
            {
                Console.WriteLine($"usage: <msbuild binaries root> <bool: use console logger> {SingleProjectArg} <project file> <cache root>");
                Console.WriteLine($"usage: <msbuild binaries root> <bool: use console logger> {CacheRoundtripArg} <project root> <cache root> [project file extension without dot]");
                Console.WriteLine($"usage: <msbuild binaries root> <bool: use console logger> {BuildManagerArg} <project root> [project file extension without dot]");
                return 0;
            }

            var msbuildBinaries = args[0];
            Trace.Assert(Directory.Exists(msbuildBinaries));
            MSBuildLocator.RegisterMSBuildPath(msbuildBinaries);

            _noConsoleLogger = bool.Parse(args[1]) == false;

            _executionTypeIndex = 2;

            switch (args[_executionTypeIndex])
            {
                case SingleProjectArg:
                    return BuildSingleProjectWithCaches(args);
                case CacheRoundtripArg:
                    return BuildWithCacheRoundtrip(args);
                case BuildManagerArg:
                    return BuildWithBuildManager(args);
                default:
                    throw new NotImplementedException();
            }
        }

        private static int BuildSingleProjectWithCaches(string[] args)
        {
            Trace.Assert(_executionTypeIndex + 2 == args.Length - 1);
            Trace.Assert(args[_executionTypeIndex] == SingleProjectArg);

            var projectFile = args[_executionTypeIndex + 1];
            var cacheRoot = args[_executionTypeIndex + 2];

            Trace.Assert(File.Exists(projectFile));
            Trace.Assert(Directory.Exists(cacheRoot));

            var cacheFiles = Directory.GetFiles(cacheRoot);

            var result = BuildProject(projectFile, null, null, cacheFiles, null);

            return result.OverallResult == BuildResultCode.Success
                ? 0
                : 1;
        }

        private static int BuildWithBuildManager(IReadOnlyList<string> args)
        {
            Trace.Assert(args[_executionTypeIndex] == BuildManagerArg);
            Trace.Assert(_executionTypeIndex + 1 <= args.Count - 1);

            var projectRootIndex = _executionTypeIndex + 1;
            var projectExtensionIndex = _executionTypeIndex + 2;

            var projectRoot = args[projectRootIndex];
            var projectFileExtension = projectExtensionIndex == args.Count - 1
                ? args[projectExtensionIndex]
                : "csproj";

            Trace.Assert(Directory.Exists(projectRoot), $"Directory does not exist: {projectRoot}");
            Trace.Assert(projectFileExtension[0] != '.');

            var projectFiles = Directory.GetFiles(projectRoot, $"*.{projectFileExtension}", SearchOption.AllDirectories);

            Trace.Assert(projectFiles.Length > 0, $"no projects found in {projectRoot}");

            using (var buildManager = new BuildManager())
            {
                var graph = new ProjectGraph(projectFiles);
                var graphRequestData = new GraphBuildRequestData(graph, new[] {"Build"});

                var parameters = new BuildParameters()
                {
                    Loggers = GetLoggers(Path.Combine(projectRoot, "logFile")),
                    IsolateProjects = true,
                    LogTaskInputs = true,
                    LogInitialPropertiesAndItems = true,
                    MaxNodeCount = 1
                };

                buildManager.BeginBuild(parameters);

                try
                {
                    var request = buildManager.PendBuildRequest(graphRequestData);

                    var graphResult = request.Execute();

                    return graphResult.OverallResult == BuildResultCode.Success
                        ? 0
                        : 1;
                }
                finally
                {
                    buildManager.EndBuild();
                }
            }
        }

        private static int BuildWithCacheRoundtrip(IReadOnlyList<string> args)
        {
            Trace.Assert(args[_executionTypeIndex] == CacheRoundtripArg);
            Trace.Assert(_executionTypeIndex + 2 <= args.Count - 1);

            var projectRootIndex = _executionTypeIndex + 1;
            var cacheRootIndex = _executionTypeIndex + 2;
            var projectExtensionIndex = _executionTypeIndex + 3;

            var cacheRoot = args[cacheRootIndex];
            var projectRoot = args[projectRootIndex];
            var projectFileExtension = projectExtensionIndex == args.Count - 1
                ? args[projectExtensionIndex]
                : "csproj";

            Trace.Assert(Directory.Exists(projectRoot), $"Directory does not exist: {projectRoot}");
            Trace.Assert(projectFileExtension[0] != '.');

            for (int i = 0; i < 3; i++)
            {
                if (Directory.Exists(cacheRoot))
                {
                    Directory.Delete(cacheRoot, true);
                }

                if (Directory.Exists(cacheRoot))
                {
                    Thread.Sleep(500);
                }
                else
                {
                    break;
                }
            }

            Directory.CreateDirectory(cacheRoot);

            var projectFiles = Directory.GetFiles(projectRoot, $"*.{projectFileExtension}", SearchOption.AllDirectories);

            Trace.Assert(projectFiles.Length > 0, $"no projects found in {projectRoot}");

            return BuildGraphWithCacheFileRoundtrip(projectFiles, cacheRoot)
                ? 0
                : 1;
        }

        private static bool BuildGraphWithCacheFileRoundtrip(IReadOnlyCollection<string> projectFiles, string cacheRoot)
        {
            ProjectGraph graph;

            var success = true;

            using (var collection = new ProjectCollection())
            {
                //collection.RegisterLogger(new EvaluationLogger());

                graph = new ProjectGraph(projectFiles, null, collection);
            }

            var cacheFiles = new Dictionary<ProjectGraphNode, string>(graph.ProjectNodes.Count);

            var nodeBuildData = graph.GetBuildData(new[] {"Build"});

            var topoSortedNodes = graph.ProjectNodesTopologicallySorted;

            foreach (var node in topoSortedNodes)
            {

                var outputCacheFile = graph.GraphRoots.Contains(node) ? null : Path.Combine(cacheRoot, node.CacheFileName());
                var inputCachesFiles = node.ProjectReferences.Select(r => cacheFiles[r]).ToArray();

                cacheFiles[node] = outputCacheFile;

                var buildData = nodeBuildData[node];

                var result = BuildProject(node.ProjectInstance.FullPath, buildData.GlobalProperties, buildData.Targets, inputCachesFiles, outputCacheFile);

                if (result.OverallResult == BuildResultCode.Failure)
                {
                    success = false;
                }
            }

            return success;
        }

        private static BuildResult BuildProject(
            string projectInstanceFullPath,
            IReadOnlyDictionary<string, string> globalProperties,
            IReadOnlyCollection<string> entryTargets,
            string[] inputCachesFiles,
            string outputCacheFile)
        {
            globalProperties = globalProperties ?? new Dictionary<string, string>();
            entryTargets = entryTargets ?? Array.Empty<string>();

            using (var buildManager = new BuildManager())
            {
                var loggers = GetLoggers(projectInstanceFullPath);

                var buildParameters = new BuildParameters
                {
                    InputResultsCacheFiles = inputCachesFiles,
                    Loggers = loggers,
                    LogTaskInputs = true,
                    LogInitialPropertiesAndItems = true,
                    MaxNodeCount = 1
                };

                if (outputCacheFile != null)
                {
                    buildParameters.OutputResultsCacheFile = outputCacheFile;
                }

                var actualGlobalProperties = globalProperties.ToDictionary(k => k.Key, k => k.Value);

                var buildRequestData = new BuildRequestData(
                    projectInstanceFullPath,
                    actualGlobalProperties,
                    "Current",
                    entryTargets.ToArray(),
                    null);

                return buildManager.Build(buildParameters, buildRequestData);
            }
        }

        private static List<ILogger> GetLoggers(string loggerPathSuffix)
        {
            var loggers = new List<ILogger>();

            if (!_noConsoleLogger)
            {
                loggers.Add(new ConsoleLogger(LoggerVerbosity.Normal));
            }

            if (DebugBuild)
            {
                Environment.SetEnvironmentVariable("MSBUILDTARGETOUTPUTLOGGING", "true");
                Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");

                var binaryLogger = new BinaryLogger
                {
                    Parameters = $"{Path.GetFileName(loggerPathSuffix)}.binlog",
                    Verbosity = LoggerVerbosity.Detailed
                };

                loggers.Add(binaryLogger);

                var fileLogger = new FileLogger
                {
                    Parameters = $"logfile={Path.GetFileName(loggerPathSuffix)}.log",
                    Verbosity = LoggerVerbosity.Detailed
                };

                loggers.Add(fileLogger);
            }
            return loggers;
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

    internal static class Extensions
    {
        public static string CacheFileName(this ProjectGraphNode node)
        {
            return $"{Path.GetFileNameWithoutExtension(node.ProjectInstance.FullPath)}-{node.GetHashCode()}";
        }

        public static void AddAll(this IDictionary<string, string> a, IReadOnlyDictionary<string, string> b)
        {
            foreach (var kvp in b)
            {
                a[kvp.Key] = kvp.Value;
            }
        }
    }
}
