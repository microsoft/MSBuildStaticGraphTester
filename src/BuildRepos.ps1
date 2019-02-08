param (
    [string]$Repos = [System.IO.Path]::Combine((pwd).Path, "repos"),
    [string]$MSBuildBranch = "graphCrossTargetting",
    [string]$MSBuildRepoAddress = "https://github.com/cdmihai/msbuild.git",
    [string]$SDKBranch = "master",
    [string]$SDKRepoAddress = "https://github.com/dotnet/sdk.git",
    [string]$Configuration = "Release",
    [switch]$PackMSBuild = $false
)
function Combine([string]$root, [string]$subdirectory)
{
    return [System.IO.Path]::Combine($root, $subdirectory)
}

function CloneRepoIfNecessary([string]$address, [string] $branch, [string] $repoPath)
{
    if (Test-Path  $repoPath) {
        return
    }

    & git clone $address $repoPath

    Push-Location $repoPath

    & git checkout $branch

    Pop-Location
}

function BuildMSBuildRepo([string]$MSBuildRepo)
{
    & "$MSBuildRepo\eng\common\build.ps1" -build -restore -configuration $Configuration /p:CreateBootstrap=true
    if ($PackMSBuild)
    {
        & "$MSBuildRepo\eng\common\build.ps1" -pack -configuration $Configuration
    }
}

function BuildSdkRepo([string]$SdkRepo)
{
    & "$SdkRepo\eng\common\build.ps1" -build -restore -configuration $Configuration
    & "$SdkRepo\eng\dogfood.ps1" -configuration $Configuration
}

$env:SDK_REPO_ROOT = ""
$env:SDK_CLI_VERSION = ""
$env:MSBuildSDKsPath = ""
$env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = ""
$env:NETCoreSdkBundledVersionsProps = ""
$env:MicrosoftNETBuildExtensionsTargets = ""

$MSBuildRepo = Combine $Repos "MSBuild"
$SdkRepo = Combine $Repos "sdk"

CloneRepoIfNecessary $MSBuildRepoAddress $MSBuildBranch $MSBuildRepo
BuildMSBuildRepo $MSBuildRepo

CloneRepoIfNecessary $SDKRepoAddress $SDKBranch $SdkRepo
BuildSdkRepo $SdkRepo

$env:MSBuildBootstrapDirectory = "$msbuildRepo\artifacts\bin\bootstrap\net472\MSBuild\Current\Bin"
$env:MSBuildNugetPackages = "$msbuildRepo\artifacts\packages\Release\Shipping"