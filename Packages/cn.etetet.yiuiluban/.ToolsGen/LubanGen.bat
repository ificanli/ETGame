set WORKSPACE=Packages\cn.etetet.yiuiluban
set WORKSPACEGEN=Packages\cn.etetet.yiuilubangen
set GEN_CLIENT=%WORKSPACE%\.Tools\Luban\Luban.dll
set CONF_ROOT=%WORKSPACEGEN%\Assets\Editor\Luban\Base
set CUSTOM=%WORKSPACE%\.ToolsGen\Custom

:: 客户端
dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t client ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\CodeMode\Model\Client\ConfigGen ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\Config\Binary\Client ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\Config\Json\Client
echo ==================== 客户端 完成 ====================

:: 服务器
dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t server ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\CodeMode\Model\Server\ConfigGen ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\Config\Binary\Server ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\Config\Json\Server
echo ==================== 服务器 完成 ====================

:: 所有
dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t all ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\CodeMode\Model\ClientServer\ConfigGen ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\Config\Binary\ClientServer ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\Config\Json\ClientServer
echo ==================== 所有 完成 ====================

:: Localhost
dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t all ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\..\StartConfig\Localhost\Base\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\Scripts\Model\Server\StartConfigGen ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\Config\Binary\Server\StartConfig\Localhost ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\Config\Json\Server\StartConfig\Localhost
echo ==================== Localhost 完成 ====================

:: Release
dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t all ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\..\StartConfig\Release\Base\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\Scripts\Model\Server\StartConfigGen ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\Config\Binary\Server\StartConfig\Release ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\Config\Json\Server\StartConfig\Release
echo ==================== Release 完成 ====================

pause