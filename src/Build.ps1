& "$PSScriptRoot\BuildRepos.ps1" -configuration Release -BuildMSBuild -BuildSdk -DogfoodSdk

& "$env:MSBuildBootstrapDirectory/msbuild.exe" /restore "$PSScriptRoot\msb\msb.csproj"

$env:GraphTestApp = "$PSScriptRoot\msb\bin\Debug\net472\msb.exe"
