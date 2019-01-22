# dotnet restore $PSScriptRoot\new-xtarget-5.sln

taskkill /f /im StructuredLogViewer.exe

Remove-Item -Force -Recurse **\bin
dotnet restore $PSScriptRoot\1\1.csproj
msbuild 1\1.csproj /bl:working.binlog

Remove-Item -Force -Recurse **\bin
dotnet restore $PSScriptRoot\1\1.csproj
msbuild 1\1.csproj /bl:failing.binlog /p:DisableTransitiveProjectReferences=true

start working.binlog
start failing.binlog