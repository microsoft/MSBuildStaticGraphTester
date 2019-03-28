param(
    [string]$repoDirectory,
    [string]$solutionFile
    )

& $env:MSBuildBootstrapExe /t:restore $solutionFile