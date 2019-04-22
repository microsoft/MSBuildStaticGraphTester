param(
    [string]$singleProjectDirectory,
    [string]$projectExtension
    )

. "$PSScriptRoot\Common.ps1"

$invocationDirectory=(pwd).Path
$msbuildBinaries=$env:MSBuildBootstrapBinDirectory
$msbuildApp=$env:MSBuildGraphTestApp

Write-Information "Using msbuild binaries from: $msbuildBinaries"
Write-Information "Using test app binaries from: $msbuildApp"

function BuildWithCacheRoundtripDefault([string] $projectRoot, [string] $projectFileExtension, [string] $solutionfile){

    BuildWithCacheRoundtrip "true" $projectRoot "$projectRoot\caches" $projectFileExtension $solutionFile
}

function BuildWithCacheRoundtrip([string] $useConsoleLogger, [string] $projectRoot, [string] $cacheRoot, [string] $projectFileExtension, [string] $solutionfile){
    & $msbuildapp $msbuildBinaries $useConsolelogger "-buildWithCacheRoundtrip" $projectRoot $cacheRoot $projectFileExtension $solutionFile
}

function BuildwithBuildManager([string] $projectRoot, [string] $projectFileExtension, [string] $solutionFile){
    & $msbuildapp $msbuildBinaries "true" "-buildWithBuildManager" $projectRoot $projectFileExtension $solutionFile
}

function BuildSingleProject($useConsoleLogger, $projectFile, $cacheRoot){
    # Write-Information "$msbuildapp $msbuildBinaries -buildWithCacheRoundtrip $invokingScriptDirectory\caches $invokingScriptDirectory"

    & $msbuildapp $msbuildBinaries $useConsoleLogger "-singleProject" $projectFile $cacheRoot
}

function PrintHeader([string]$text)
{
    $line = "=========================$text========================="
    Write-Information ("=" * $line.Length)
    Write-Information $line
    Write-Information ("=" * $line.Length)
}

function SetupTestProject([string]$projectRoot, [PSCustomObject]$repoInfo)
{
    Write-Information ""
    Write-Information "   Cleaning bin and obj under $projectRoot"

    Remove-Item -Force -Recurse -Path "$projectRoot" -Include "bin"
    Remove-Item -Force -Recurse -Path "$projectRoot" -Include "obj"

    RunProjectSetupIfPresent $projectRoot $repoInfo
}

function TestProject([string] $projectRoot, [string] $projectExtension)
{
    $repoInfo = GetRepoInfo $projectRoot

    Write-Information "RepoInfo object:"
    Write-Information $repoInfo

    MaterializeRepoIfNecessary $repoInfo $projectRoot

    $solutionFile = if ($null -ne $repoInfo.SolutionFile)
    {
        $repoInfo.SolutionFile
    }
    else
    {
        $null
    }

    SetupTestProject $projectRoot $repoInfo
    PrintHeader "BuildManager: $projectRoot"
    BuildWithBuildManager $projectRoot $projectExtension $solutionFile

    if ($LASTEXITCODE -ne 0)
    {
        exit
    }

    SetupTestProject $projectRoot $repoInfo
    PrintHeader "Cache roundtrip: $projectRoot"
    BuildWithCacheRoundtripDefault $projectRoot $projectExtension $solutionFile

    if ($LASTEXITCODE -ne 0)
    {
        exit
    }
}

if ($singleProjectDirectory) {
    Write-Information "Single Project Mode"

    if (!$projectExtension)
    {
        $projectExtension = "csproj"
    }

    TestProject $singleProjectDirectory $projectExtension
    exit
}

Write-Information "Batch Project Mode"

$workingProjectRoots = @{
    "$projectsDirectory\non-sdk\working" = "proj";
    "$projectsDirectory\sdk\working" = "csproj"
}

$brokenProjects = @(
    "oldWPF1",
    "oldWPF-new-2", # generates projects and msbuilds into it
    "new2-directInnerBuildReference", # needs investigation
    "orchard-core" # requires transitive project references
)

foreach ($directoryWithProjects in $workingProjectRoots.Keys)
{
    $projectExtension = $workingProjectRoots[$directoryWithProjects]

    foreach ($projectRoot in Get-ChildItem -Directory $directoryWithProjects)
    {
        if ($brokenProjects.Contains($projectRoot.Name))
        {
            PrintHeader "Skipping $($projectRoot.FullName)"
            continue;
        }

        TestProject $projectRoot.FullName $projectExtension
    }
}