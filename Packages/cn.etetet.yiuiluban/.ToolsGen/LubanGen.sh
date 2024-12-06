# 设置工作空间变量
WORKSPACE="Packages/cn.etetet.yiuiluban"
WORKSPACEGEN="Packages/cn.etetet.yiuilubangen"
GEN_CLIENT="$WORKSPACE/.Tools/Luban/Luban.dll"
CONF_ROOT="$WORKSPACEGEN/Assets/Editor/Luban/Base"
CUSTOM="$WORKSPACE/.ToolsGen/Custom"

# 客户端
/usr/local/share/dotnet/dotnet "$GEN_CLIENT" \
--customTemplateDir "$CUSTOM" \
-t client \
-c cs-bin \
-d bin \
-d json \
--conf "$CONF_ROOT/luban.conf" \
-x outputCodeDir="$WORKSPACEGEN/CodeMode/Model/Client/ConfigGen" \
-x bin.outputDataDir="$WORKSPACEGEN/Assets/Config/Bin/Client" \
-x json.outputDataDir="$WORKSPACEGEN/Assets/Config/Json/Client"
echo "==================== 客户端 完成 ===================="

# 服务器
/usr/local/share/dotnet/dotnet "$GEN_CLIENT" \
--customTemplateDir "$CUSTOM" \
-t server \
-c cs-bin \
-d bin \
-d json \
--conf "$CONF_ROOT/luban.conf" \
-x outputCodeDir="$WORKSPACEGEN/CodeMode/Model/Server/ConfigGen" \
-x bin.outputDataDir="$WORKSPACEGEN/Assets/Config/Bin/Server" \
-x json.outputDataDir="$WORKSPACEGEN/Assets/Config/Json/Server"
echo "==================== 服务器 完成 ===================="

# 所有
/usr/local/share/dotnet/dotnet "$GEN_CLIENT" \
--customTemplateDir "$CUSTOM" \
-t all \
-c cs-bin \
-d bin \
-d json \
--conf "$CONF_ROOT/luban.conf" \
-x outputCodeDir="$WORKSPACEGEN/CodeMode/Model/ClientServer/ConfigGen" \
-x bin.outputDataDir="$WORKSPACEGEN/Assets/Config/Bin/ClientServer" \
-x json.outputDataDir="$WORKSPACEGEN/Assets/Config/Json/ClientServer"
echo "==================== 所有 完成 ===================="

# Localhost
/usr/local/share/dotnet/dotnet "$GEN_CLIENT" \
--customTemplateDir "$CUSTOM" \
-t all \
-c cs-bin \
-d bin \
-d json \
--conf "$CONF_ROOT/../StartConfig/Localhost/Base/luban.conf" \
-x outputCodeDir="$WORKSPACEGEN/Scripts/Model/Server/StartConfigGen" \
-x bin.outputDataDir="$WORKSPACEGEN/Assets/Config/Bin/Server/StartConfig/Localhost" \
-x json.outputDataDir="$WORKSPACEGEN/Assets/Config/Json/Server/StartConfig/Localhost"
echo "==================== Localhost 完成 ===================="

# Release
/usr/local/share/dotnet/dotnet "$GEN_CLIENT" \
--customTemplateDir "$CUSTOM" \
-t all \
-c cs-bin \
-d bin \
-d json \
--conf "$CONF_ROOT/../StartConfig/Release/Base/luban.conf" \
-x outputCodeDir="$WORKSPACEGEN/Scripts/Model/Server/StartConfigGen" \
-x bin.outputDataDir="$WORKSPACEGEN/Assets/Config/Bin/Server/StartConfig/Release" \
-x json.outputDataDir="$WORKSPACEGEN/Assets/Config/Json/Server/StartConfig/Release"
echo "==================== Release 完成 ===================="

read -p "Press enter to continue"