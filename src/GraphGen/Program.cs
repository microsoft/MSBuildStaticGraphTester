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
            
            var outFile = args.Length > 1 ? args[2] : "out.png";

            // GraphGen.exe <proj-file> ?<out.png> ?<msbuild-path>
            if (args.Length == 0)
            {
                Console.WriteLine("GraphGen.exe <proj-file> ?<out.png> ?<msbuild-path>");
                Environment.Exit(1);
            }

            var projectFile = new FileInfo(args[0]);

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
            
            var files = new List<string>();
            if (projectFile.Extension == ".sln")
            {
                files.AddRange(SolutionParser.GetProjectFiles(projectFile.FullName));
            }
            else
            {
                files.Add(projectFile.FullName);
            }

            var graphText = new Program().LoadGraph(files);

            GraphVis.SaveAsPng(graphText, outFile);
        }

        private string LoadGraph(List<string> files)
        {
            Console.WriteLine("Loading graph...");
            var sw = Stopwatch.StartNew();
            var graph = new ProjectGraph(files, ProjectCollection.GlobalProjectCollection);
            Console.WriteLine($@"{files.First()} loaded {graph.ProjectNodes.Count} node(s) in {sw.ElapsedMilliseconds}ms.");

            return GraphVis.Create(graph);
        }
    }
}
