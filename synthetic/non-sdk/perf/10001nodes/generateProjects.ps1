$project =
@'
<Project>
    <Import Project="$([MSBuild]::GetPathOfFileAbove('protocol.props'))"/>
</Project>
'@

$projectWithReferences =
@'
<Project>
    <Import Project="$([MSBuild]::GetPathOfFileAbove('protocol.props'))"/>
    <ItemGroup>
{0}
    </ItemGroup>
</Project>
'@

$count=100

$root = "$PSScriptRoot\generatedProjects"
$caches = "$PSScriptRoot\caches"

if (Test-Path $root) {
    Remove-Item -Recurse -Force $root
}

if (Test-Path $caches) {
    Remove-Item -Recurse -Force $caches
}

mkdir $root

$sb = [System.Text.StringBuilder]::new();

foreach($i in 1..$count)
{
    $projectPath = "$root\$i.proj"

    Set-Content -Path $projectPath -Value $project

    $sb.AppendLine("<ProjectReference Include=""$i.proj""/>")
}

$rootProject = [System.String]::Format($projectWithReferences, $sb)

echo $rootProject > "$root\0.proj"

..\..\..\..\src\msb\bin\Debug\net472\msb.exe "E:\projects\msbuild\artifacts\Debug\bootstrap\net472\MSBuild\Current\Bin" "-noConsoleLogger" "-buildWithCacheRoundtrip" $caches $root "proj"
