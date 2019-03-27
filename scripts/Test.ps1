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

function GetRepoInfo([string] $repoRoot)
{
    $repoInfoFile = Combine $projectRoot "repoInfo"

    if (Test-Path $repoInfoFile)
    {
        $repoInfo = [PSCustomObject](Get-Content $repoInfoFile | ConvertFrom-Json)

        $repoDirectory = Combine $repoRoot "repo"
        $repoInfo | Add-Member -MemberType NoteProperty -Name "RepoDirectory" -Value $repoDirectory

        if ($null -ne $repoInfo.SolutionFile)
        {
            $repoInfo.SolutionFile = Combine $repoInfo.RepoDirectory $repoInfo.SolutionFile
        }

        return $repoInfo
    }
    else
    {
        return [PSCustomObject]@{
            RepoAddress = $null
            RepoLocation = $null
            SolutionFile = $null
            RepoDirectory = $null
        }
    }
}

function MaterializeRepo([PSCustomObject] $repoInfo, [string] $repoRoot)
{
    $repoAddress = $repoInfo.RepoAddress
    $repoCommit = $repoInfo.RepoLocation
    $repoDirectory = $repoInfo.RepoDirectory

    CloneOrUpdateRepo $repoAddress $repoCommit $repoDirectory
}

function MaterializeRepoIfNecessary([PSCustomObject]$repoInfo)
{
    if ($null -ne $repoInfo.RepoAddress)
    {
        Write-Information "Materializing $($repoInfo.RepoAddress)"
        MaterializeRepo $repoInfo $projectRoot
    }
}

function SetupTestProject([string]$projectRoot, [PSCustomObject]$repoInfo)
{
    Write-Information "   Cleaning bin and obj under $projectRoot"

    Remove-Item -Force -Recurse "$projectRoot\**\bin"
    Remove-Item -Force -Recurse "$projectRoot\**\obj"

    $setupScript = Combine $projectRoot "setup.ps1"

    if (Test-Path $setupScript)
    {
        $projectDir = if ($null -ne $repoInfo.RepoDirectory)
        {
            $repoInfo.RepoDirectory
        }
        else
        {
            $projectRoot
        }

        Write-Information "   running $setupScript"
        & $setupScript -repoDirectory $projectDir -solutionFile $repoInfo.SolutionFile
    }
}

function TestProject([string] $projectRoot, [string] $projectExtension)
{
    $repoInfo = GetRepoInfo $projectRoot

    Write-Information $repoInfo

    MaterializeRepoIfNecessary $repoInfo

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
    TestProject $singleProjectDirectory $projectExtension
    exit
}

Write-Information "Batch Project Mode"

$workingProjectRoots = @{
    "$projectsDirectory\non-sdk\working" = "proj";
    "$projectsDirectory\sdk\working" = "csproj"
}

$brokenProjects = @("oldWPF1", "oldWPF-new-2", "new2-directInnerBuildReference")

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