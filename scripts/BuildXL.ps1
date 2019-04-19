# projectRoot is expected to be a path to the project info to build. E.g. projects/sdk/roslyn.bxl
# pathToBxl is the path to bxl.exe
param (
    [Parameter(Mandatory=$true)]
    [string]$projectRoot,
    [Parameter(Mandatory=$true)]
    [string]$pathToBxl
)

# set up the environment
$setupEnv = $PSScriptRoot + "\BuildRepos.ps1 RedirectEnvironmentToBuildOutputs"
Invoke-Expression $setupEnv

. "$PSScriptRoot\Common.ps1"

# look for a repo on the given path
$repoInfo = GetRepoInfo $projectRoot

if ($null -eq $repoInfo.RepoDirectory) {
    Write-Host "Could not find repo:" $projectRoot
    exit 1
}

# Materialize and setup the repo
MaterializeRepoIfNecessary $repoInfo
RunProjectSetupIfPresent $projectRoot $repoInfo

# Run bxl
Write-Information "Running bxl"
$pathToConfig = $repoInfo.RepoDirectory + "\config.dsc"
& $pathToBxl /c:$pathToConfig /disableProcessRetryOnResourceExhaustion+