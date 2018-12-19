setlocal

set msbuild="E:\projects\msbuild\artifacts\Release\bootstrap\net472\MSBuild\Current\Bin"

echo %msbuild%

E:\projects\MSBuildTestProjects\src\msb\bin\Debug\net472\msb.exe %msbuild% "-singleProject" "%~dp0generatedProjects\0.proj" "%~dp0caches"