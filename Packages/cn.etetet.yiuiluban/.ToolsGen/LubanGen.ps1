# 设置变量
$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$WORKSPACEGEN = "Packages/cn.etetet.yiuilubangen"
$GEN_CLIENT = "$WORKSPACE/.Tools/Luban/Luban.dll"
$CONF_ROOT = "$WORKSPACEGEN/Assets/Editor/Luban/Base"
$CUSTOM = "$WORKSPACE/.ToolsGen/Custom"

# powershell判断是不是Mac平台
$DotNet = "dotnet.exe"
if ($IsWindows -ne $true) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

# 客户端
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t client -c cs-bin -d bin -d json --conf $CONF_ROOT/luban.conf -x outputCodeDir=$WORKSPACEGEN/CodeMode/Model/Client/ConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Client -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Client
Write-Host "==================== 客户端 完成 ===================="

# 服务器
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t server -c cs-bin -d bin -d json --conf $CONF_ROOT/luban.conf -x outputCodeDir=$WORKSPACEGEN/CodeMode/Model/Server/ConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Server -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Server
Write-Host "==================== 服务器 完成 ===================="

# 所有
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $CONF_ROOT/luban.conf -x outputCodeDir=$WORKSPACEGEN/CodeMode/Model/ClientServer/ConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/ClientServer -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/ClientServer
Write-Host "==================== 所有 完成 ===================="

# Localhost
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $CONF_ROOT/../StartConfig/Localhost/Base/luban.conf -x outputCodeDir=$WORKSPACEGEN/Scripts/Model/Server/StartConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Server/StartConfig/Localhost -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Server/StartConfig/Localhost
Write-Host "==================== Localhost 完成 ===================="

# Release
& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $CONF_ROOT/../StartConfig/Release/Base/luban.conf -x outputCodeDir=$WORKSPACEGEN/Scripts/Model/Server/StartConfigGen -x bin.outputDataDir=$WORKSPACEGEN/Assets/Config/Binary/Server/StartConfig/Release -x json.outputDataDir=$WORKSPACEGEN/Assets/Config/Json/Server/StartConfig/Release
Write-Host "==================== Release 完成 ===================="