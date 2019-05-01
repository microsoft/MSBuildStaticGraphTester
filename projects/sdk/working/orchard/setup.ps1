param(
    [string]$repoDirectory,
    [string]$solutionFile
    )

# Copy the main bxl configuration file and a traversal file (solution files are not supported yet)
Copy-Item "$PSScriptRoot\config.dsc" -Destination $repoDirectory
Copy-Item "$PSScriptRoot\Orchard.traversal.proj" -Destination $repoDirectory

& "$repoDirectory\lib\nuget\nuget.exe" restore $solutionFile