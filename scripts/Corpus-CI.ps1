. "$PSScriptRoot\Common.ps1"

ExecuteAndExitOnFailure "$PSScriptRoot\Build.ps1 -BuildRepos"
ExecuteAndExitOnFailure "$PSScriptRoot\Test.ps1 -includeMSBuildTests"