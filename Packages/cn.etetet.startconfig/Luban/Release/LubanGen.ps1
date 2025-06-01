# 设置变量
$PACKAGE = "Packages/cn.etetet.startconfig"
$CONFIG_NAME = "Release"


$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll"
$CUSTOM = "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom"

# powershell判断是不是Mac平台
$DotNet = "dotnet.exe"
if ($IsWindows -ne $true) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/luban.conf -x outputCodeDir=$PACKAGE/Scripts/Model/Server/StartConfig -x bin.outputDataDir=$PACKAGE/Bundles/Luban/$CONFIG_NAME/Server/Binary/ -x json.outputDataDir=$PACKAGE/Bundles/Luban/$CONFIG_NAME/Server/Json/
Write-Host "==================== $CONFIG_NAME 完成 ===================="