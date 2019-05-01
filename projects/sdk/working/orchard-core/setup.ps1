param(
    [string]$repoDirectory,
    [string]$solutionFile
    )

# Copy the main bxl configuration file and a traversal file (solution files are not supported yet)
Copy-Item "$PSScriptRoot\config.dsc" -Destination $repoDirectory
Copy-Item "$PSScriptRoot\OrchardCore.traversal.proj" -Destination $repoDirectory

& $env:MSBuildBootstrapExe /t:restore $solutionFile