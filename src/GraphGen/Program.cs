using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using CommonUtilities;

namespace GraphGen
{
    class CommandLineArguments
    {
        [Value(0, MetaName = "input file",
            HelpText = "Input file to generate a project from (solution or msbuild project)",
            Required = true)]
        public string InputFile { get; set; }

        [Value(1, MetaName = "output file",
            HelpText = "Output file containing the graph picture.",
            Required = false)]
        public  string OutputFile { get; set; } = "out.png";
        
        [Value(2, MetaName = "msbuild bin directory",
            HelpText = "Directory containing msbuild dlls used to parse the graph. If absent, a VS instance gets used.",
            Required = false)]
        public string MSBuildBinDirectory { get; set; }

        [Option('e', "end-nodes",
            HelpText = "Only print the paths from the graph roots to this semicolon separated list of end nodes. End nodes can be specified in full path or partial paths (e.g. foo.csproj, or src/foo/foo.csproj, etc)")]
        public string EndNodes { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
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

                var projectFile = args.InputFile;

                if (string.IsNullOrEmpty(args.MSBuildBinDirectory))
                {
                    MSBuildLocatorUtils.RegisterMSBuild();
                }
                else
                {
                    MSBuildLocator.RegisterMSBuildPath(args.MSBuildBinDirectory);
                }

                var graphText = new Program().LoadGraph(new FileInfo(projectFile), string.IsNullOrEmpty(args.EndNodes) ? null : args.EndNodes.Split(';'));

                if (outFile.EndsWith(".txt"))
                {
                    File.WriteAllText(outFile, graphText);
                    Console.Out.WriteLine($"Dot notation written to {outFile}");
                }
                else
                {
                    GraphVis.Save(graphText, outFile);
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

        private string LoadGraph(FileInfo projectFile, string[] endNodes)
        {
            Console.WriteLine("Loading graph...");

            var sw = Stopwatch.StartNew();
            var graph = new ProjectGraph(projectFile.FullName, ProjectCollection.GlobalProjectCollection);

            Console.WriteLine($@"{projectFile} loaded {graph.ProjectNodes.Count} node(s) in {sw.ElapsedMilliseconds}ms.");

            if (endNodes != null)
            {
                var endGraphNodes = graph.ProjectNodes.Where(n => endNodes.Any(en => n.ProjectInstance.FullPath.Contains(en)));
                var paths = GraphPaths.FindAllPathsBetween(graph.GraphRoots, endGraphNodes);

                var deduplicatedNodes = paths.SelectMany(p => p).ToHashSet();
                return GraphVis.Create(deduplicatedNodes);
            }
            else
            {
                return GraphVis.Create(graph);
            }
        }
    }
}
