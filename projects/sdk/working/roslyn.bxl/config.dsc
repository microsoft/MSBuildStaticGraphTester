config({
    resolvers: [
        {
            kind: "MsBuild",
            moduleName: "Compilers",
            root: d`.`,
            msBuildSearchLocations: [Environment.getDirectoryValue("MSBuildBootstrapBinDirectory")],
            fileNameEntryPoints: [r`Compilers.sln`],

            // The following are the required env variables the repo needs
            environment: Map.empty<string, string>()
                            .add("Path", Environment.getStringValue("PATH"))
                            .add("USERPROFILE", Environment.getStringValue("USERPROFILE"))
                            // Env vars that redirect the SDK lookup to point to the latest available
                            .add("DOTNET_INSTALL_DIR", Environment.getStringValue("DOTNET_INSTALL_DIR"))
                            .add("DOTNET_MULTILEVEL_LOOKUP", Environment.getStringValue("DOTNET_MULTILEVEL_LOOKUP"))
                            .add("SDK_REPO_ROOT", Environment.getStringValue("SDK_REPO_ROOT"))
                            .add("SDK_CLI_VERSION", Environment.getStringValue("SDK_CLI_VERSION"))
                            .add("MSBuildSDKsPath", Environment.getStringValue("MSBuildSDKsPath"))
                            .add("DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR", Environment.getStringValue("DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR"))
                            .add("NETCoreSdkBundledVersionsProps", Environment.getStringValue("NETCoreSdkBundledVersionsProps"))
                            .add("MicrosoftNETBuildExtensionsTargets", Environment.getStringValue("MicrosoftNETBuildExtensionsTargets"))
                            .add("DOTNET_ROOT", Environment.getStringValue("DOTNET_ROOT"))
        },  
    ],
});