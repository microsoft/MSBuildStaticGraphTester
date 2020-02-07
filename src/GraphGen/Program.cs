using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using CommonUtilities;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;

namespace GraphGen
{
    internal class CommandLineArguments
    {
        [Value(0, MetaName = "input file",
            HelpText = "Input file to generate a project from (solution or msbuild project)",
            Required = true)]
        public string InputFile { get; set; }

        [Value(1, MetaName = "output file",
            HelpText = "Output file containing the graph picture.",
            Required = false)]
        public string OutputFile { get; set; } = "out.png";

        [Value(2, MetaName = "msbuild bin directory",
            HelpText = "Directory containing msbuild dlls used to parse the graph. If absent, a VS instance gets used.",
            Required = false)]
        public string MSBuildBinDirectory { get; set; }

        [Option('t', "targets",
            HelpText = "Semicolon delimited list of entry targets. Default targets are used if this parameters is not set.")]
        public string Targets { get; set; }
        
        [Option('p', "global-properties",
            HelpText = "Semicolon delimited list of <key>=<value> pairs of global properties.")]
        public string GlobalProperties { get; set; }

        [Option('e', "end-nodes",
            HelpText =
                "Only print the paths from the graph roots to this semicolon separated list of end nodes. End nodes can be specified in full path or partial paths (e.g. foo.csproj, or src/foo/foo.csproj, etc)"
            )]
        public string EndNodes { get; set; }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            var parseResult = Parser.Default.ParseArguments<CommandLineArguments>(args);
            var returnValue = parseResult
                .MapResult(
                    Run,
                    _ => 1);

            return returnValue;
        }

        private static int Run(CommandLineArguments args)
        {
            try
            {
                var outFile = Path.GetFullPath(args.OutputFile);

                var directory = Path.GetDirectoryName(outFile);
                var filename = Path.GetFileNameWithoutExtension(outFile);
                var extension = Path.GetExtension(outFile);

                var projectFile = args.InputFile;

                if (string.IsNullOrEmpty(args.MSBuildBinDirectory))
                {
                    MSBuildLocatorUtils.RegisterMSBuild();
                }
                else
                {
                    MSBuildLocator.RegisterMSBuildPath(args.MSBuildBinDirectory);
                }

                var globalProperties = string.IsNullOrEmpty(args.GlobalProperties)
                    ? ImmutableDictionary<string, string>.Empty
                    : args.GlobalProperties.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(
                        propertyPair =>
                        {
                            var kvp = propertyPair.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);

                            Trace.Assert(kvp.Length == 2, $"Expected <key>=<value> format in {propertyPair}");

                            return (kvp[0], kvp[1]);
                        }).ToImmutableDictionary(kvp => kvp.Item1, kvp => kvp.Item2);

                var targets = string.IsNullOrEmpty(args.Targets)
                    ? null
                    : args.Targets.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                var endNodes = string.IsNullOrEmpty(args.EndNodes)
                    ? null
                    : args.EndNodes.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

                var dotNotations = new Program().GetDotNotations(new FileInfo(projectFile), globalProperties, targets, endNodes);

                var renderingFunction = extension.EndsWith(".txt")
                    ? (path, dotNotation) => File.WriteAllText(path, dotNotation)
                    : (Action<string, string>) ((path, dotNotation) => GraphVis.Save(dotNotation, path));

                foreach (var dotNotation in dotNotations)
                {
                    var outputFile = Path.Combine(directory, $"{filename}{dotNotation.pathPostfix}{extension}");
                    renderingFunction(outputFile, dotNotation.contents);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                Console.ResetColor();
                return 1;
            }
        }

        private IEnumerable<(string pathPostfix, string contents)> GetDotNotations(
            FileInfo projectFile,
            ImmutableDictionary<string, string> globalProperties,
            string[] targets,
            string[] endNodes)
        {
            Console.WriteLine("Loading graph...");

            var sw = Stopwatch.StartNew();
            var graph = new ProjectGraph(new ProjectGraphEntryPoint(projectFile.FullName, globalProperties), ProjectCollection.GlobalProjectCollection);
            sw.Stop();

            Console.WriteLine($@"{projectFile} loaded {graph.ProjectNodes.Count} node(s) in {sw.ElapsedMilliseconds}ms.");

            var entryTargetsPerNode = graph.GetTargetLists(targets);

            if (endNodes != null)
            {
                var endGraphNodes = graph.ProjectNodes.Where(n => endNodes.Any(en => n.ProjectInstance.FullPath.Contains(en)));
                var paths = GraphPaths.FindAllPathsBetween(graph.GraphRoots, endGraphNodes);

                var deduplicatedNodes = paths.SelectMany(p => p).ToHashSet();
                yield return ($"_PathsEndingIn_{string.Join(",", endNodes)}", GraphVis.Create(deduplicatedNodes, entryTargetsPerNode));
            }

            yield return ("", GraphVis.Create(graph, entryTargetsPerNode));
        }
    }
}
