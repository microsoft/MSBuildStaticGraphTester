setlocal

set msbuild="E:\projects\msbuild\artifacts\Debug\bootstrap\net472\MSBuild\Current\Bin"

echo %msbuild%

E:\projects\MSBuildTestProjects\src\msb\bin\Debug\net472\msb.exe %msbuild% "%~dp0caches" %~dp0