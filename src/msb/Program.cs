using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.Build.Experimental.Graph;

namespace msb
{
    internal class Program
    {
        private const string SingleProjectArg = "-singleProject";
        private const string CacheRoundtripArg = "-buildWithCacheRoundtrip";
        private const string NoConsoleLoggerArg = "-noConsoleLogger";
        private const string BuildManagerArg = "-buildWithBuildManager";

#if RELEASE
        private static readonly bool DebugBuild = false;
#else
        private static readonly bool DebugBuild = true;
#endif

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
            _minimumArgumentCount = 4;

            if (args.Length < _minimumArgumentCount || (args.Length > 0 && args[0].IndexOfAny(new[] {'h', '?'}) >= 0))
            {
                Console.WriteLine($"usage: <msbuild binaries root> <bool: use console logger> {SingleProjectArg} <project file> <cache root>");
                Console.WriteLine($"usage: <msbuild binaries root> <bool: use console logger> {CacheRoundtripArg} <project root> <cache root> [project file extension without dot] [solution file]");
                Console.WriteLine($"usage: <msbuild binaries root> <bool: use console logger> {BuildManagerArg} <project root> [project file extension without dot] [solution file]");
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

        private static int BuildSingleProjectWithCaches(IReadOnlyList<string> args)
        {
            Trace.Assert(_executionTypeIndex + 2 == args.Count - 1);
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
            var solutionFileIndex = _executionTypeIndex + 3;

            var projectRoot = args[projectRootIndex];
            var projectFileExtension = projectExtensionIndex <= args.Count - 1
                ? args[projectExtensionIndex]
                : "csproj";
            var solutionFile = solutionFileIndex == args.Count - 1
                ? args[solutionFileIndex]
                : null;

            Trace.Assert(Directory.Exists(projectRoot), $"Directory does not exist: {projectRoot}");
            Trace.Assert(projectFileExtension[0] != '.');

            var projectFiles = GetProjects(projectRoot, solutionFile, projectFileExtension);

            using (var buildManager = new BuildManager())
            {
                var graph = CreateProjectGraph(projectFiles);

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

        private static ImmutableList<string> GetProjects(string projectRoot, string solutionFile, string projectFileExtension)
        {
            Trace.Assert(!projectFileExtension.EndsWith(".sln"));
            Trace.Assert(!solutionFile?.EndsWith("proj") ?? true);

            ImmutableList<string> projectFiles;
            if (solutionFile == null)
            {
                var projectWildcard = $"*.{projectFileExtension}";

                Console.WriteLine($"Solution file not found. Getting projects by scanning for {projectWildcard} in {projectRoot}");

                projectFiles = Directory.GetFiles(projectRoot, projectWildcard, SearchOption.AllDirectories).ToImmutableList();
            }
            else
            {
                Console.WriteLine($"Getting projects from solution {solutionFile}");
                projectFiles = GetProjectFilesFromSolutionFile(solutionFile).ToImmutableList();
            }

            Trace.Assert(projectFiles.Count > 0, $"no projects found in {projectRoot}");

            return projectFiles;
        }

        private static int BuildWithCacheRoundtrip(IReadOnlyList<string> args)
        {
            Trace.Assert(args[_executionTypeIndex] == CacheRoundtripArg);
            Trace.Assert(_executionTypeIndex + 2 <= args.Count - 1);

            var projectRootIndex = _executionTypeIndex + 1;
            var cacheRootIndex = _executionTypeIndex + 2;
            var projectExtensionIndex = _executionTypeIndex + 3;
            var solutionFileIndex = _executionTypeIndex + 4;

            var cacheRoot = args[cacheRootIndex];
            var projectRoot = args[projectRootIndex];
            var projectFileExtension = projectExtensionIndex <= args.Count - 1
                ? args[projectExtensionIndex]
                : "csproj";
            var solutionFile = solutionFileIndex == args.Count - 1
                ? args[solutionFileIndex]
                : null;

            Trace.Assert(Directory.Exists(projectRoot), $"Directory does not exist: {projectRoot}");
            Trace.Assert(projectFileExtension[0] != '.');

            FileUtils.DeleteDirectoryWithRetry(cacheRoot);
            Directory.CreateDirectory(cacheRoot);

            var projectFiles = GetProjects(projectRoot, solutionFile, projectFileExtension);

            return BuildGraphWithCacheFileRoundtrip(projectFiles, cacheRoot)
                ? 0
                : 1;
        }

        private static bool BuildGraphWithCacheFileRoundtrip(IReadOnlyCollection<string> projectFiles, string cacheRoot)
        {
            ProjectGraph graph;

            var success = true;

            graph = CreateProjectGraph(projectFiles);

            if (DebugBuild)
            {
                Console.WriteLine(ToDot(graph));
            }

            var cacheFiles = new Dictionary<ProjectGraphNode, string>(graph.ProjectNodes.Count);

            var targetLists = graph.GetTargetLists(null);

            var topoSortedNodes = graph.ProjectNodesTopologicallySorted;

            var skippedNodesDueToEmptyTargets = new HashSet<ProjectGraphNode>();

            foreach (var node in topoSortedNodes)
            {
                var targets = targetLists[node];

                if (targets.IsEmpty)
                {
                    skippedNodesDueToEmptyTargets.Add(node);
                    continue;
                }

                var outputCacheFile = graph.GraphRoots.Contains(node) ? null : Path.Combine(cacheRoot, node.CacheFileName());

                var inputCachesFiles = new List<string>(node.ProjectReferences.Count);

                for (var i = 0; i < node.ProjectReferences.Count; i++)
                {
                    var reference = node.ProjectReferences.ElementAt(i);

                    if (skippedNodesDueToEmptyTargets.Contains(reference))
                    {
                        Trace.Assert(!cacheFiles.ContainsKey(reference), "Skipped nodes do not have cache files");
                        continue;
                    }

                    Trace.Assert(cacheFiles.ContainsKey(reference), "Each reference must propagate a cache file");

                    inputCachesFiles.Add(cacheFiles[reference]);
                }

                cacheFiles[node] = outputCacheFile;

                var result = BuildProject(node.ProjectInstance.FullPath, node.ProjectInstance.GlobalProperties, targets, inputCachesFiles.ToArray(), outputCacheFile);

                if (result.OverallResult == BuildResultCode.Failure)
                {
                    success = false;
                }
            }

            if (skippedNodesDueToEmptyTargets.Any())
            {
                Console.WriteLine("Skipped projects due to empty targets:");

                foreach (var skippedNode in skippedNodesDueToEmptyTargets)
                {
                    var nodePath = skippedNode.ProjectInstance.FullPath;
                    var targetFramework = skippedNode.ProjectInstance.GetPropertyValue("TargetFramework");
                    Console.WriteLine($"\t{Path.GetFileNameWithoutExtension(nodePath)}(TargetFramework={targetFramework}):{nodePath}");
                }
            }

            return success;
        }

        private static ProjectGraph CreateProjectGraph(IReadOnlyCollection<string> projectFiles)
        {
            using (var collection = new ProjectCollection())
            {
//                collection.RegisterLogger(new EvaluationLogger());


                var projectGraph = new ProjectGraph(projectFiles, null, collection);

                Console.WriteLine($"Root Count: {projectGraph.GraphRoots.Count}");
                Console.WriteLine($"Node Count: {projectGraph.ProjectNodes.Count}");

                return projectGraph;
            }
        }

        private static BuildResult BuildProject(
            string projectInstanceFullPath,
            IDictionary<string, string> globalProperties,
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

        internal static string ToDot(ProjectGraph graph)
        {
            var nodeCount = 0;
            return ToDot(graph, node => nodeCount++.ToString());
        }

        internal static string ToDot(ProjectGraph graph, Func<ProjectGraphNode, string> nodeIdProvider)
        {
            var nodeIds = new ConcurrentDictionary<ProjectGraphNode, string>();

            var sb = new StringBuilder();

            sb.Append("digraph g\n{\n\tnode [shape=box]\n");

            foreach (var node in graph.ProjectNodes)
            {
                var nodeId = nodeIds.GetOrAdd(node, (n, idProvider) => idProvider(n), nodeIdProvider);

                var nodeName = Path.GetFileNameWithoutExtension(node.ProjectInstance.FullPath);
                var globalPropertiesString = string.Join("<br/>", node.ProjectInstance.GlobalProperties.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}"));

                sb.AppendLine($"\t{nodeId} [label=<{nodeName}<br/>{globalPropertiesString}>]");

                foreach (var reference in node.ProjectReferences)
                {
                    var referenceId = nodeIds.GetOrAdd(reference, (n, idProvider) => idProvider(n), nodeIdProvider);

                    sb.AppendLine($"\t{nodeId} -> {referenceId}");
                }
            }

            sb.Append("}");

            return sb.ToString();
        }

        private static IEnumerable<string> GetProjectFilesFromSolutionFile(string solutionFile)
        {
            return SolutionParser.GetProjectFiles(solutionFile);
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
