using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("usage: <msbuild binaries root> <cache root> <project root>");
                return;
            }

            var msbuildBinaries = args[0];
            var cacheRoot = args[1];
            var projectRoot = args[2];

            Trace.Assert(Directory.Exists(projectRoot));

            MSBuildLocator.RegisterMSBuildPath(msbuildBinaries);

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
            var graph = new ProjectGraph(projectFiles);

            var cacheFiles = new Dictionary<ProjectGraphNode, string>(graph.ProjectNodes.Count);

            var entryPointTargets = graph.GetTargetLists(new[] {"Build"});

            var topoSortedNodes = graph.ProjectNodesTopologicallySorted;

            foreach (var node in topoSortedNodes)
            {
                var outputCacheFile = Path.Combine(cacheRoot, node.CacheFileName());
                var inputCachesFiles = node.ProjectReferences.Select(r => cacheFiles[r]);

                cacheFiles[node] = outputCacheFile;

                var entryTargets = entryPointTargets[node];

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
                    Loggers = new[] {new ConsoleLogger(LoggerVerbosity.Normal)}
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
    }

    static class ProjectGraphNodeExtensions
    {
        public static string CacheFileName(this ProjectGraphNode node) => $"{Path.GetFileNameWithoutExtension(node.ProjectInstance.FullPath)}-{node.GetHashCode()}";
    }
}
