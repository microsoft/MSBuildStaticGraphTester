param(
    [string]$repoDirectory,
    [string]$solutionFile
    )

$env:Path = "$env:MSBuildBootstrapBinDirectory;$env:Path"

& "$repoDirectory\Restore.cmd"