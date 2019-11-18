config({
    resolvers: [
        {
            kind: "MsBuild",
            moduleName: "OrchardCore",
            root: d`.`,

            // The following are the required env variables the repo needs
            environment: Map.empty<string, string>()
                            .add("Path", Environment.getStringValue("DOTNET_INSTALL_DIR"))
                            .add("USERPROFILE", Environment.getStringValue("USERPROFILE"))
                            
                            // Env vars that redirect the SDK lookup to point to the latest available
                            .add("DOTNET_INSTALL_DIR", Environment.getStringValue("DOTNET_INSTALL_DIR"))
                            .add("DOTNET_MULTILEVEL_LOOKUP", Environment.getStringValue("DOTNET_MULTILEVEL_LOOKUP"))
                            .add("SDK_REPO_ROOT", Environment.getStringValue("SDK_REPO_ROOT"))
                            .add("SDK_CLI_VERSION", Environment.getStringValue("SDK_CLI_VERSION"))
                            .add("MSBuildSDKsPath", Environment.getStringValue("MSBuildSDKsPath"))
                            .add("DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR", Environment.getStringValue("DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR"))
                            .add("NETCoreSdkBundledVersionsProps", Environment.getStringValue("NETCoreSdkBundledVersionsProps"))
                            .add("MicrosoftNETBuildExtensionsTargets", Environment.getStringValue("MicrosoftNETBuildExtensionsTargets")),

            // An explicit entry point avoids cached graph misses due to changes in the directory root membership
            fileNameEntryPoints: [r`OrchardCore.sln`],
            
            // If the location of MSBuild is set beforehand, honor it. Otherwise leave it to the default behavior.
            msBuildSearchLocations: Environment.hasVariable("MSBuildBootstrapBinDirectory") ? [Environment.getDirectoryValue("MSBuildBootstrapBinDirectory")] : undefined,

            // This option has perf consequences, but for now we want to keep the original repo 'untouched', and currently
            // the repo relies on implicit transitive references, as this is the current MSBuild SDK behavior.
            enableTransitiveProjectReferences: true, 

            // Building in isolation is still not compatible with enabling transitive project references.
            // Keeping this building in legacy mode until MSBuild provides proper support for it at graph construction time
            useLegacyProjectIsolation: true,

            // Enabling shared compilation in this repo also starts Razor build server for code generation. This is 
            // not VBCSCompiler server process, but a Razor-specific server which then tries to survive the pip.
            useManagedSharedCompilation: false,
        },  
    ],
    disableDefaultSourceResolver: true,
});