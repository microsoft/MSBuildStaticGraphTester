param (
    [switch]$BuildSdk,
    [switch]$BuildMSBuild,
    [switch]$RedirectEnvironmentToBuildOutputs,
    [string]$Repos,
    [string]$MSBuildBranch = "wpfIsolation",
    [string]$MSBuildRepoAddress = "https://github.com/cdmihai/msbuild.git",
    [string]$SDKBranch = "master",
    [string]$SDKRepoAddress = "https://github.com/dotnet/sdk.git",
    [string]$Configuration = "Release"
)

. "$PSScriptRoot\Common.ps1"

if (-not $Repos)
{
    $Repos = Combine $repoPath "repo"
}

function BuildMSBuildRepo([string]$MSBuildRepo)
{
    & "$MSBuildRepo\eng\common\build.ps1" -build -restore -ci -pack -configuration $Configuration /p:CreateBootstrap=true /p:ApplyPartialNgenOptimization=false
    # & "$MSBuildRepo\eng\common\build.ps1" -build -restore -configuration $Configuration /p:CreateBootstrap=true
    # & "$MSBuildRepo\eng\common\build.ps1" -ci -pack -configuration $Configuration /p:ApplyPartialNgenOptimization=false
    ExitOnFailure
}

function BuildSdkRepo([string]$SdkRepo)
{
    & "$SdkRepo\eng\common\build.ps1" -build -restore -configuration $Configuration
    ExitOnFailure
}

function DogfoodSdk([string]$SdkRepo)
{
    & "$SdkRepo\eng\dogfood.ps1" -configuration $Configuration
    ExitOnFailure
}

function GetNugetVersionFromFirstFileName([string] $nugetPackageRoot)
{
    $nugetPackage = Get-ChildItem $nugetPackageRoot | Select-Object -First 1

    return GetNugetVersion $nugetPackage.FullName
}

function GetNugetVersion([string] $nugetPackageFile)
{
    $versionStart = $nugetPackageFile.IndexOf("16.")
    $versionEnd = $nugetPackageFile.IndexOf(".nupkg")

    if ($versionStart -lt 0)
    {
        throw "Cannot find start of '16.' when extracting msbuild version from '$nugetPackageFile'"
    }

    $version = $nugetPackageFile.SubString($versionStart, $versionEnd - $versionStart)

    return $version
}

function RemoveItemIfExists([string] $item)
{
    if (Test-Path $item)
    {
        Remove-Item $item
    }
}

function ResetUnwantedBuildScriptSideEffects()
{
    $env:DOTNET_INSTALL_DIR = ""
    $env:DOTNET_MULTILEVEL_LOOKUP = ""
    $env:SDK_REPO_ROOT = ""
    $env:SDK_CLI_VERSION = ""
    $env:MSBuildSDKsPath = ""
    $env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = ""
    $env:NETCoreSdkBundledVersionsProps = ""
    $env:MicrosoftNETBuildExtensionsTargets = ""

    RemoveItemIfExists "Variable:_DotNetInstallDir"
    RemoveItemIfExists "Variable:_ToolsetBuildProj"
    RemoveItemIfExists "Variable:_BuildTool"
}

$MSBuildRepo = Combine $Repos "MSBuild"
$MSBuildBootstrapRoot = Combine $msbuildRepo "artifacts\bin\bootstrap\net472"

if ($BuildMSBuild)
{
    ResetUnwantedBuildScriptSideEffects

    CloneOrUpdateRepo $MSBuildRepoAddress $MSBuildBranch $MSBuildRepo

    # incrementality of copying to bootstrap does not work
    if (Test-Path $MSBuildBootstrapRoot)
    {
        # can't delete bootstrap if its dlls were used to build
        taskkill /F /IM msbuild.exe
        taskkill /F /IM vbcscompiler.exe

        Write-Host "Deleting $MSBuildBootstrapRoot"

        Remove-item -force -recurse $MSBuildBootstrapRoot
    }

    BuildMSBuildRepo $MSBuildRepo
}

$SdkRepo = Combine $Repos "sdk"

if ($BuildSdk)
{
    ResetUnwantedBuildScriptSideEffects

    CloneOrUpdateRepo $SDKRepoAddress $SDKBranch $SdkRepo
    BuildSdkRepo $SdkRepo
}

if ($RedirectEnvironmentToBuildOutputs)
{
    ResetUnwantedBuildScriptSideEffects

    DogfoodSdk $SdkRepo

    $env:MSBuildBootstrapRoot = Combine $MSBuildRepo "artifacts\bin\bootstrap\net472"
    $env:MSBuildBootstrapBinDirectory = Combine $env:MSBuildBootstrapRoot "MSBuild\Current\Bin"
    $env:MSBuildBootstrapExe = Combine $env:MSBuildBootstrapBinDirectory "MSBuild.exe"
    $env:MSBuildNugetPackages = Combine $MSBuildRepo "artifacts\packages\$Configuration\Shipping"
    $env:MSBuildNugetVersion = GetNugetVersionFromFirstFileName $env:MSBuildNugetPackages

    $env:SdkRepoBinDirectory = Combine $SdkRepo "artifacts\bin\Release\Sdks"
    $env:SdkRepo = $SdkRepo

    $env:AllowedReads = "$env:MSBuildBootstrapRoot;$env:SdkRepoBinDirectory;$env:SdkRepo"
}
