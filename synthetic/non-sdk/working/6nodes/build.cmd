setlocal

set msbuild=%1

echo %msbuild%

%msbuild% 1.proj /graph /isolate

%msbuild% 6.proj /orc:6
%msbuild% 5.proj /orc:5
%msbuild% 4.proj /orc:4 /irc:6
%msbuild% 3.proj /orc:3 /irc:5;6
%msbuild% 2.proj /orc:2 /irc:3;4
%msbuild% 1.proj /orc:1 /irc:5;3;4
