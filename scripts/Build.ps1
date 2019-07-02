param (
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [switch]$BuildRepos
)

Set-StrictMode -Version Latest

. "$PSScriptRoot\Common.ps1"

if ($BuildRepos)
{
    & "$PSScriptRoot\BuildRepos.ps1" -configuration $Configuration -BuildMSBuild -BuildSdk -RedirectEnvironmentToBuildOutputs
    ExitOnFailure
}
else
{
    & "$PSScriptRoot\BuildRepos.ps1" -configuration $Configuration -RedirectEnvironmentToBuildOutputs
    ExitOnFailure
}

rm -Recurse -Force -Path "$sourceDirectory" -Include "bin"
rm -Recurse -Force -Path "$sourceDirectory" -Include "obj"

& "$env:MSBuildBootstrapExe" /restore "$sourceDirectory\src.sln" /p:"Configuration=$Configuration"

$env:MSBuildGraphTestApp = "$sourceDirectory\out\$Configuration\bin\msb\net472\msb.exe"
