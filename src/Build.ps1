& "$PSScriptRoot\BuildRepos.ps1" -configuration Release -BuildMSBuild -BuildSdk -RedirectEnvironmentToBuildOutputs

& "$env:MSBuildBootstrapBinDirectory/msbuild.exe" /restore "$PSScriptRoot\msb\msb.csproj"

$env:GraphTestApp = "$PSScriptRoot\msb\bin\Debug\net472\msb.exe"
