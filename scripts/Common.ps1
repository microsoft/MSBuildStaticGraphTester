Set-StrictMode -Version Latest

function Combine
{
    [string[]] $argsAsStrings = $args
    return [System.IO.Path]::Combine($argsAsStrings)
}

function ToGitHash([string] $branchOrCommit)
{
    # on valid branch name, prints commit sha
    # else errors and prints nothing
    $refsString = (git show-ref --hash $branchOrCommit)

    if (-not $refsString)
    {
        $returnVal =  $branchOrCommit
    }
    else
    {
        # there can be multiple shas returned, pick the first one
        $refs = $refsString -split '\s'

        $firstRef = $refs[0]

        $returnVal = $firstRef
    }

    $discardedOutput =  (git cat-file -e $returnVal 2>&1)

    if (-Not ($?))
    {
        throw "Not a valid git object: [$branchOrCommit]"
    }

    if ($returnVal -notmatch ".*\d.*")
    {
        Write-Error "[$branchCommit] was converted to the [$returnVal] hash, but the hash does not have numbers. Probably something went wrong."
        exit
    }

    return $returnVal
}

function CloneOrUpdateRepo([string]$address, [string] $location, [string] $repoPath, [bool] $updateIfExists = $true)
{
    $currentDirectory = Get-Location

    try
    {
        if ((Test-Path  $repoPath) -and $updateIfExists)
        {
            Push-Location $repoPath

            ExecuteAndExitOnFailure "git remote -v"

            ExecuteAndExitOnFailure "git fetch origin"

            $locationHash = ToGitHash $location
            Write-Information "[$location] converted to [$locationHash]"

            ExecuteAndExitOnFailure "git checkout $locationHash"

            Pop-Location
        }
        elseif (-not (Test-Path $repoPath))
        {
            ExecuteAndExitOnFailure "git clone $address $repoPath"

            Push-Location $repoPath

            $locationHash = ToGitHash $location
            Write-Information "[$location] converted to [$locationHash]"

            ExecuteAndExitOnFailure "git fetch origin"

            ExecuteAndExitOnFailure "git checkout $locationHash"

            Pop-Location
        }
    }
    finally
    {
        Set-Location $currentDirectory
    }
}

function PrintStackTrace(){
    foreach($line in (Get-PSCallStack))
    {
        Write-Information $line
    }
}

function VariableIsDeclared([string] $aVariable, [string]$scope="local")
{
    if (Get-Variable -Name $aVariable -scope $scope -ErrorAction SilentlyContinue)
    {
        return $true
    }
    else
    {
        return $false
    }
}

function ExitOnFailure
{
    if (-Not ($?))
    {
        exit 1
    }

    if ((VariableIsDeclared "LastExitCode" "global") -and ($global:LastExitCode -ne 0))
    {
        exit 1
    }
}

function ExecuteAndExitOnFailure([string] $command)
{
    Write-Information "Running: [$command]"
    
    Invoke-Expression $command

    ExitOnFailure
}

function GetRepoInfo([string] $pathToRepo)
{
    $repoInfoFile = Combine $pathToRepo "repoInfo"

    if (Test-Path $repoInfoFile)
    {
        $repoInfo = [PSCustomObject](Get-Content $repoInfoFile | ConvertFrom-Json)

        $repoDirectory = Combine $pathToRepo "repo"
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

function MaterializeRepoIfNecessary([PSCustomObject]$repoInfo, [string] $repoRoot)
{
    if ($null -ne $repoInfo.RepoAddress)
    {
        Write-Information "Materializing $($repoInfo.RepoAddress)"
        MaterializeRepo $repoInfo $repoRoot
    }
}

function RunProjectSetupIfPresent([string]$projectRoot, [PSCustomObject]$repoInfo)
{
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

        Write-Information "Running setup script $setupScript"
        ExecuteAndExitOnFailure "$setupScript -repoDirectory $projectDir -solutionFile $($repoInfo.SolutionFile)"
    }
}

if(-not (VariableIsDeclared "commonIsInitialized" "script"))
{
    
    $ErrorActionPreference = "Stop"
    $InformationPreference = "Continue"
    $DebugPreference = "Continue"

    $repoPath = [System.IO.Path]::GetFullPath((Combine $PSScriptRoot ".."))
    $projectsDirectory = Combine $repoPath "projects"
    $sourceDirectory = Combine $repoPath "src"

    $script:commonIsInitialized = "initialized"
}