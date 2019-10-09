# projectRoot is expected to be a path to the project info to build. E.g. projects/sdk/roslyn.bxl
# pathToBxl is the path to bxl.exe
param (
    [Parameter(Mandatory=$true)]
    [string]$projectRoot,
    [Parameter(Mandatory=$true)]
    [string]$pathToBxl,
    [Parameter(Mandatory=$false)]
    [string]$bxlExtraArgs
)

# set up the environment
Write-Host "Setting up the environment..."
& "$PSScriptRoot\BuildRepos.ps1" -configuration "Release" -RedirectEnvironmentToBuildOutputs
Write-Host "Using MSBuild at " $env:MSBuildBootstrapBinDirectory
Write-Host "Using BuildXL at " $pathToBxl

. "$PSScriptRoot\Common.ps1"

# look for a repo on the given path
$repoInfo = GetRepoInfo $projectRoot

if ($null -eq $repoInfo.RepoDirectory) {
    Write-Host "Could not find repo:" $projectRoot
    exit 1
}

# Materialize and setup the repo
MaterializeRepoIfNecessary $repoInfo $projectRoot
RunProjectSetupIfPresent $projectRoot $repoInfo

# Run bxl
Write-Host "Running bxl"
$pathToConfig = $repoInfo.RepoDirectory + "\config.dsc"
& $pathToBxl /c:$pathToConfig $bxlExtraArgs