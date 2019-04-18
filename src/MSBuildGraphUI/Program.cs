using Microsoft.Build.Locator;
using System;
using System.Linq;
using System.Windows.Forms;

namespace MSBuildGraphUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //var msbuildPath = @"D:\src\msbuild.fork\artifacts\bin\bootstrap\net472\MSBuild\Current\Bin";
            //var msbuildPath = @"D:\src\msbuild\artifacts\Debug\bootstrap\net472\MSBuild\15.0\Bin";
            //var msbuildPath = @"D:\src\DomTest\SGEC\src\rps\MSBuild\artifacts\bin\bootstrap\net472\MSBuild\Current\Bin";
            //MSBuildLocator.RegisterMSBuildPath(msbuildPath);

            var instances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default);
            var instance = instances.FirstOrDefault(i => i.Version.Major == 16);
            MSBuildLocator.RegisterInstance(instance);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
