using Microsoft.Build.Evaluation;
using Microsoft.Build.Experimental.Graph;
using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GraphGen
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var outFile = args.Length > 1 ? args[1] : "out.png";

                // GraphGen.exe <proj-file> ?<out.png> ?<msbuild-path>
                if (args.Length == 0)
                {
                    Console.WriteLine("GraphGen.exe <proj-file> ?<out.png> ?<msbuild-path>");
                    Environment.Exit(1);
                }

                var projectFile = args[0];

                if (args.Length < 3)
                {
                    var instances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default);
                    var instance = instances.FirstOrDefault(i => i.Version.Major == 16);
                    MSBuildLocator.RegisterInstance(instance);
                }
                else
                {
                    MSBuildLocator.RegisterMSBuildPath(args[2]);
                }

                var graphText = new Program().LoadGraph(new FileInfo(projectFile));

                GraphVis.Save(graphText, outFile);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private string LoadGraph(FileInfo projectFile)
        {
            var files = new List<string>();
            if (projectFile.Extension == ".sln")
            {
                files.AddRange(SolutionParser.GetProjectFiles(projectFile.FullName));
            }
            else
            {
                files.Add(projectFile.FullName);
            }

            Console.WriteLine("Loading graph...");
            var sw = Stopwatch.StartNew();
            var graph = new ProjectGraph(files, ProjectCollection.GlobalProjectCollection);
            Console.WriteLine($@"{projectFile} loaded {graph.ProjectNodes.Count} node(s) in {sw.ElapsedMilliseconds}ms.");

            return GraphVis.Create(graph);
        }
    }
}
