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
    $refsString = (git show-ref --hash $branchOrCommit) | Out-String

    if (-not $refsString)
    {
        $returnVal =  $branchOrCommit
    }
    else
    {
        $refs = $refsString -split '\s'

        $firstRef = $refs[0]

        $returnVal = $firstRef
    }

    return $returnVal
}

function CloneOrUpdateRepo([string]$address, [string] $location, [string] $repoPath, [bool] $updateIfExists = $true)
{
    $locationHash = ToGitHash $location

    if ((Test-Path  $repoPath) -and $updateIfExists)
    {
        Push-Location $repoPath

        & git remote -v

        & git fetch origin

        $locationHash = ToGitHash $location
        Write-Information "[$location] converted to [$locationHash]"

        & git checkout $locationHash

        Pop-Location
    }
    elseif (-not (Test-Path $repoPath))
    {
        & git clone $address $repoPath

        Push-Location $repoPath

        $locationHash = ToGitHash $location
        Write-Information "[$location] converted to [$locationHash]"

        & git fetch origin

        & git checkout $locationHash

        Pop-Location
    }
}

$repoPath = [System.IO.Path]::GetFullPath((Combine $PSScriptRoot ".."))
$projectsDirectory = Combine $repoPath "projects"
$sourceDirectory = Combine $repoPath "src"