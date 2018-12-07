setlocal

set msbuild=%1

echo %msbuild%

msbuild 1.proj

%msbuild% 3.proj /orc:3
%msbuild% 2.proj /orc:2
%msbuild% 1.proj /orc:1 /irc:2,3
