[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RootDir,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$mainPackageFile = Join-Path $RootDir 'MainPackage.txt'
if (-not (Test-Path -LiteralPath $mainPackageFile)) {
    throw "MainPackage.txt not found: $mainPackageFile"
}

$packageNames = Get-Content -LiteralPath $mainPackageFile -Encoding UTF8 |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -ne '' }

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('<Project>')
$lines.Add('  <ItemGroup>')

foreach ($packageName in $packageNames) {
    $lines.Add(('    <Compile Include="$(ETRootDir)Packages/{0}/Scripts/Loader/Share/**/*.cs" Link="Packages\{0}\Scripts\Loader\Share\%(RecursiveDir)%(Filename)%(Extension)" />' -f $packageName))
    $lines.Add(('    <Compile Include="$(ETRootDir)Packages/{0}/Scripts/Loader/Server/**/*.cs" Link="Packages\{0}\Scripts\Loader\Server\%(RecursiveDir)%(Filename)%(Extension)" />' -f $packageName))
    $lines.Add(('    <Compile Include="$(ETRootDir)Packages/{0}/CodeMode/Loader/Server/**/*.cs" Link="Packages\{0}\CodeMode\Loader\Server\%(RecursiveDir)%(Filename)%(Extension)" />' -f $packageName))
}

$lines.Add('  </ItemGroup>')
$lines.Add('</Project>')

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
