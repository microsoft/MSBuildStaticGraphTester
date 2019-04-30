config({
    resolvers: [
        {
            kind: "MsBuild",
            moduleName: "Orchard",
            root: d`.`,

            // These variables are set by the solution file in the original Orchard repo, so we mimic those here
            environment: Map.empty<string, string>()
                            .add("BuildPlatform", "x64")
                            .add("Configuration", "retail")
                            .add("OutputPath", "..\\..\\build\\compile")
                            // We only need to set UseCommonOutputDirectory if containers are not used. This is to avoid many
                            // double writes that happen otherwise that Domino may not handle well.
                            .add("UseCommonOutputDirectory", Environment.hasVariable("RunInContainer")? "false" : "true"),
            
            // An explicit entry point avoids cached graph misses due to changes in the directory root membership
            fileNameEntryPoints: [r`Orchard.traversal.proj`],
            
            // If the location of MSBuild is set beforehand, honor it. Otherwise leave it to the default behavior.
            msBuildSearchLocations: Environment.hasVariable("MSBuildBootstrapBinDirectory") ? [Environment.getDirectoryValue("MSBuildBootstrapBinDirectory")] : undefined,

            // Run all pips in a Helium container if required
            runInContainer: Environment.hasVariable("RunInContainer"), 
            
            // We only need to untrack stuff if pips don't run in a container
            untrackedFiles: Environment.hasVariable("RunInContainer")? [] : 
            [
                f`${Context.getSpecFileDirectory()}\build\compile\x86\Microsoft.VC90.CRT\Microsoft.VC90.CRT.manifest`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\Microsoft.VC90.CRT\msvcr90.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\Microsoft.VC90.CRT\README_ENU.txt`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\sqlcecompact40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\sqlceer40EN.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\sqlceme40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\sqlceqp40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\x86\sqlcese40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\Microsoft.VC90.CRT\Microsoft.VC90.CRT.manifest`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\Microsoft.VC90.CRT\msvcr90.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\Microsoft.VC90.CRT\README_ENU.txt`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\sqlcecompact40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\sqlceer40EN.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\sqlceme40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\sqlceqp40.dll`,
                f`${Context.getSpecFileDirectory()}\build\compile\amd64\sqlcese40.dll`, 
            ],
        },  
    ],
});