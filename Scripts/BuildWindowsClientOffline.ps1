param(
    [string]$UnityExe = $env:UNITY_EXE,
    [string]$ProjectPath = ".",
    [string]$OutputDir = ".\\Release\\WindowsClient",
    [string]$BuildVersion
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $PathValue))
}

function Assert-OutputDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot,
        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory
    )

    $projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot)
    $outputRootFull = [System.IO.Path]::GetFullPath($OutputDirectory)
    $driveRoot = [System.IO.Path]::GetPathRoot($outputRootFull)

    if ($outputRootFull -eq $projectRootFull) {
        throw "输出目录不能是项目根目录: $outputRootFull"
    }

    if ($outputRootFull -eq $driveRoot) {
        throw "输出目录不能是磁盘根目录: $outputRootFull"
    }
}

$resolvedProjectPath = [System.IO.Path]::GetFullPath((Resolve-Path $ProjectPath).Path)
$resolvedOutputDir = Resolve-FullPath -BasePath $resolvedProjectPath -PathValue $OutputDir

if ([string]::IsNullOrWhiteSpace($UnityExe)) {
    throw "请通过 -UnityExe 参数或环境变量 UNITY_EXE 提供 Unity.exe 路径。"
}

$resolvedUnityExe = [System.IO.Path]::GetFullPath($UnityExe)
if (-not (Test-Path -LiteralPath $resolvedUnityExe)) {
    throw "未找到 Unity 可执行文件: $resolvedUnityExe"
}

Assert-OutputDirectory -ProjectRoot $resolvedProjectPath -OutputDirectory $resolvedOutputDir

if (Test-Path -LiteralPath $resolvedOutputDir) {
    Remove-Item -LiteralPath $resolvedOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $resolvedOutputDir -Force | Out-Null

$logDir = Join-Path $resolvedProjectPath "Logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logPath = Join-Path $logDir ("WindowsClientOfflineBuild-{0}.log" -f (Get-Date -Format "yyyyMMdd-HHmmss"))

$unityArguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $resolvedProjectPath,
    "-executeMethod", "ET.ClientBuildAutomation.BuildWindowsOfflineFromCommandLine",
    "-logFile", $logPath,
    "-outputDir", $resolvedOutputDir
)

if (-not [string]::IsNullOrWhiteSpace($BuildVersion)) {
    $unityArguments += @("-buildVersion", $BuildVersion)
}

Write-Host "Unity.exe: $resolvedUnityExe"
Write-Host "ProjectPath: $resolvedProjectPath"
Write-Host "OutputDir: $resolvedOutputDir"
Write-Host "LogFile: $logPath"

& $resolvedUnityExe @unityArguments
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    throw "Unity 离线打包失败，退出码: $exitCode。日志: $logPath"
}

Write-Host "Windows 客户端离线打包完成: $resolvedOutputDir"
Write-Host "Unity 日志: $logPath"
