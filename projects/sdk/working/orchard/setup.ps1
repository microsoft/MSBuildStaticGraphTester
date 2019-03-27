param(
    [string]$repoDirectory,
    [string]$solutionFile
    )

& "$repoDirectory\lib\nuget\nuget.exe" restore $solutionFile