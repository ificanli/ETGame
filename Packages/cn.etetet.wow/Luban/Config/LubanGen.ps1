# 设置变量
$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$WORKSPACEGEN = "Packages/cn.etetet.wow"
$GEN_CLIENT = "$WORKSPACE/.Tools/Luban/Luban.dll"
$CONF_ROOT = "$WORKSPACEGEN"
$CUSTOM = "$WORKSPACE/.ToolsGen/Custom"

# powershell判断是不是Mac平台
$DotNet = "dotnet.exe"
if ($IsWindows -ne $true) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

# 客户端
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t client -c cs-bin -d bin -d json --conf $CONF_ROOT/Luban/Config/luban.conf -x outputCodeDir=$WORKSPACEGEN/CodeMode/Model/Client/Config/ConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Client/Config -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Client/Config
Write-Host "==================== 客户端 完成 ===================="

# 服务器
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t server -c cs-bin -d bin -d json --conf $CONF_ROOT/Luban/Config/luban.conf -x outputCodeDir=$WORKSPACEGEN/CodeMode/Model/Server/Config/ConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Server/Config -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Server/Config
Write-Host "==================== 服务器 完成 ===================="

# 所有
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $CONF_ROOT/Luban/Config/luban.conf -x outputCodeDir=$WORKSPACEGEN/CodeMode/Model/ClientServer/Config/ConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/ClientServer/Config -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/ClientServer/Config
Write-Host "==================== 所有 完成 ===================="