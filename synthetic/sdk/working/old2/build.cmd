setlocal

set msbuild="E:\projects\msbuild\artifacts\Debug\bootstrap\net472\MSBuild\Current\Bin"

echo %msbuild%

E:\projects\MSBuildTestProjects\src\msb\bin\Debug\net472\msb.exe %msbuild% "E:\projects\MSBuildTestProjects\synthetic\sdk\working\old2\caches" "E:\projects\MSBuildTestProjects\synthetic\sdk\working\old2"