param(
    [string]$singleProjectDirectory,
    [string]$singleProjectEntryFile = $null,
    [string]$projectExtension,
    [switch]$includeBuildXLTests,
    [switch]$includeMSBuildTests
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

function TestProjectWithMSBuild([string] $projectRoot, [string] $projectExtension, [string] $entryProjectFile = $null)
{

    Write-Information " Testing $projectRoot with build file extension $projectExtension"
    $repoInfo = GetRepoInfo $projectRoot $entryProjectFile

    Write-Information "RepoInfo object:"
    Write-Information $repoInfo

    MaterializeRepoIfNecessary $repoInfo $projectRoot

    SetupTestProject $projectRoot $repoInfo 
    PrintHeader "BuildManager: $projectRoot"
    BuildWithBuildManager $projectRoot $projectExtension $repoInfo.EntryProjectFile

    ExitOnFailure

    SetupTestProject $projectRoot $repoInfo
    PrintHeader "Cache roundtrip: $projectRoot"
    BuildWithCacheRoundtripDefault $projectRoot $projectExtension $repoInfo.EntryProjectFile

    ExitOnFailure
}

function TestProjectWithBuildXL([string] $projectRoot)
{
    PrintHeader "BuildXL: $projectRoot"

    $BuildXLScript = Combine $PSScriptRoot "BuildXL.ps1"

    ExecuteAndExitOnFailure "$BuildXLScript -projectRoot $projectRoot -pathToBxl $env:BuildXLExe"
}

if ($singleProjectDirectory) {
    Write-Information "Single Project Mode"

    if ($includeMSBuildTests)
    {
        if (!$projectExtension)
        {
            $projectExtension = "csproj"
        }

        TestProjectWithMSBuild $singleProjectDirectory $projectExtension $singleProjectEntryFile
    }

    if ($includeBuildXLTests)
    {
        TestProjectWithBuildXL $singleProjectDirectory
    }

    exit
}

Write-Information "Batch Project Mode"

if ($includeMSBuildTests)
{
    $workingProjectRoots = @{
        "$projectsDirectory\non-sdk\working" = "proj";
        "$projectsDirectory\sdk\working" = "csproj"
    }
    
    $customProjectExtensions = @{
        "traversal" = "proj";
        "DependencyOn2New" = "customProj"
    }
    
    $brokenProjects = @(
        # needs investigation
        "new2-directInnerBuildReference",
        # requires transitive project references
        "orchard-core",
        # needs investigation
        "traversal",
        "roslyn.bxl"
    )
    
    foreach ($directoryWithProjects in $workingProjectRoots.Keys)
    {
        $projectsRootExtension = $workingProjectRoots[$directoryWithProjects]
    
        foreach ($projectRoot in Get-ChildItem -Directory $directoryWithProjects)
        {
            if ($brokenProjects.Contains($projectRoot.Name))
            {
                PrintHeader "Skipping $($projectRoot.FullName)"
                continue;
            }
    
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectRoot.Fullname)
    
            if ($customProjectExtensions[$projectName])
            {
                $projectExtension = $customProjectExtensions[$projectName]
            }
            else
            {
                $projectExtension = $projectsRootExtension
            }
    
            TestProjectWithMSBuild $projectRoot.FullName $projectExtension
        }
    }
}

if ($includeBuildXLTests)
{
    $BuildXLRepos = @(
        "sdk\working\orchard",
        "sdk\working\orchard-core",
        "sdk\working\roslyn.bxl"
    )

    foreach($BuildXLRepo in $BuildXLRepos)
    {
        $projectRoot = Combine $projectsDirectory $BuildXLRepo
        TestProjectWithBuildXL $projectRoot
    }

}