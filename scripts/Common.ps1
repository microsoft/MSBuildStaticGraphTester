Set-StrictMode -Version Latest

$InformationPreference = "Continue"
$DebugPreference = "Continue"

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

    if ($LASTEXITCODE -ne 0)
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

function ExitOnFailure
{
    if ($LASTEXITCODE -ne 0)
    {
        exit
    }
}

function ExecuteAndExitOnFailure([string] $command, [bool] $captureOutput = $false)
{
    Write-Information "Running: [$command]"
    $output = $null

    if ($captureOutput)
    {
        $output = Invoke-Expression $command
    }
    else
    {
        Invoke-Expression $command
    }

    ExitOnFailure

    return $output
}

$repoPath = [System.IO.Path]::GetFullPath((Combine $PSScriptRoot ".."))
$projectsDirectory = Combine $repoPath "projects"
$sourceDirectory = Combine $repoPath "src"