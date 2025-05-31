# 设置变量
$WORKSPACEGEN = "Packages/cn.etetet.startconfig"

$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "$WORKSPACE/.Tools/Luban/Luban.dll"
$CONF_ROOT = "$WORKSPACEGEN"
$CUSTOM = "$WORKSPACE/.ToolsGen/Custom"

# powershell判断是不是Mac平台
$DotNet = "dotnet.exe"
if ($IsWindows -ne $true) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

# Localhost
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $CONF_ROOT/Luban/Localhost/luban.conf -x outputCodeDir=$WORKSPACEGEN/Scripts/Model/Server/StartConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Server/Localhost -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Server/Localhost
Write-Host "==================== Localhost 完成 ===================="