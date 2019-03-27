param (
    [string]$Configuration = "Release",
    [switch]$BuildRepos
)

Set-StrictMode -Version Latest

. "$PSScriptRoot\Common.ps1"

if ($BuildRepos)
{
    & "$PSScriptRoot\BuildRepos.ps1" -configuration $Configuration -BuildMSBuild -BuildSdk -RedirectEnvironmentToBuildOutputs
}
else
{
    & "$PSScriptRoot\BuildRepos.ps1" -configuration $Configuration -RedirectEnvironmentToBuildOutputs
}

rm -Recurse -Force "$sourceDirectory\msb\bin"
rm -Recurse -Force "$sourceDirectory\msb\obj"

& "$env:MSBuildBootstrapExe" /restore "$sourceDirectory\msb\msb.csproj" /p:"Configuration=$Configuration"

$env:MSBuildGraphTestApp = "$sourceDirectory\msb\bin\$Configuration\net472\msb.exe"
