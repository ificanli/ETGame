# 设置变量
$PACKAGE = "Packages/cn.etetet.wow"
$CONFIG_NAME = "Config"


$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll"
$CUSTOM = "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom"

# powershell判断是不是Mac平台
$DotNet = "dotnet.exe"
if ($IsWindows -ne $true) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

# 客户端
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t client -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/Client/$CONFIG_NAME/ConfigGen -x bin.outputDataDir=$PACKAGE/Assets/Config/Binary/Client/$CONFIG_NAME -x json.outputDataDir=$PACKAGE/Assets/Config/Json/Client/$CONFIG_NAME
Write-Host "==================== 客户端 完成 ===================="

# 服务器
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t server -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/Server/$CONFIG_NAME/ConfigGen -x bin.outputDataDir=$PACKAGE/Assets/Config/Binary/Server/$CONFIG_NAME -x json.outputDataDir=$PACKAGE/Assets/Config/Json/Server/$CONFIG_NAME
Write-Host "==================== 服务器 完成 ===================="

# 所有
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/ClientServer/$CONFIG_NAME/ConfigGen -x bin.outputDataDir=$PACKAGE/Assets/Config/Binary/ClientServer/$CONFIG_NAME -x json.outputDataDir=$PACKAGE/Assets/Config/Json/ClientServer/$CONFIG_NAME
Write-Host "==================== 所有 完成 ===================="