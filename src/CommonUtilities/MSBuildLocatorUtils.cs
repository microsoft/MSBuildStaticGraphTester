using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Locator;

namespace CommonUtilities
{
    public static class MSBuildLocatorUtils
    {
        public static void RegisterMSBuild()
        {
            var bootstrapMSBuildBinDirectory = Environment.GetEnvironmentVariable("MSBuildBootstrapBinDirectory");

            if (string.IsNullOrEmpty(bootstrapMSBuildBinDirectory) || !Directory.Exists(bootstrapMSBuildBinDirectory))
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default);
                var instance = instances.FirstOrDefault(i => i.Version.Major == 16);
                MSBuildLocator.RegisterInstance(instance);
            }
            else
            {
                MSBuildLocator.RegisterMSBuildPath(bootstrapMSBuildBinDirectory);
            }
        }
    }
}
