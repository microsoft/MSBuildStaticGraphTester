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

. ..\..\..\Test.ps1

# generate caches for references
BuildWithCacheRoundtrip "false" $root $caches "proj"

# build top project in isolation reusing the caches generated in the previous step
Measure-Command { BuildSingleProject "false" "$root\0.proj" $caches }