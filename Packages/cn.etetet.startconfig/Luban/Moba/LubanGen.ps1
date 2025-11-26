# 设置变量
$PACKAGE = "Packages/cn.etetet.startconfig"
$CONFIG_NAME = "Release"


$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll"
$CUSTOM = "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom"

# powershell判断是不是Mac平台
$DotNet = "dotnet.exe"
if ($null -ne $IsMacOS) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-simple-json -d json --conf $PACKAGE/Luban/$CONFIG_NAME/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/Server/StartConfig -x json.outputDataDir=$PACKAGE/Bundles/Luban/$CONFIG_NAME/Server/Json/
Write-Host "==================== $CONFIG_NAME Server完成 ===================="

& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-simple-json -d json --conf $PACKAGE/Luban/$CONFIG_NAME/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/ClientServer/StartConfig -x json.outputDataDir=$PACKAGE/Bundles/Luban/$CONFIG_NAME/ClientServer/Json/
Write-Host "==================== $CONFIG_NAME ClientServer完成 ===================="