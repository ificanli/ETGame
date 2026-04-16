# YIUI MCP完整人工体验流程

**创建时间**：2026-04-10
**最后更新**：2026-04-10
**状态**：进行中
**关联任务**：M0.2-W3 #12
**涉及包**：cn.etetet.yiuimcp, cn.etetet.yiuiframework

## 目标

把当前工程里 `YIUIMCP` 的人工体验路径收敛成一条可重复执行的端到端流程，覆盖：

1. Unity Editor 侧服务是否正常启动
2. `.port` 是否与 Unity 实际监听端口同步
3. `Config/*.ps1` 脚本链路是否可用
4. UTO HTTP 层是否可用
5. 单工具调用与编译流程是否可用

本流程当前以基础设施链路为主，但已补充一条“通过 `ExecuteMenu` 驱动实际页面资源构建”的实测样例。

## 前置条件

### 环境要求

- Unity Editor 已打开当前工程
- `Packages/cn.etetet.yiuimcp` 已导入且无当前包自身编译错误
- 本机可用 `node`
- `Packages/cn.etetet.yiuimcp/UTO/build` 已存在
- 使用 PowerShell 执行脚本

### 当前已知前提

- Unity MCP 默认监听 `127.0.0.1:3212`
- UTO HTTP 默认监听 `3213`
- `Packages/cn.etetet.yiuimcp/UTO/.port` 会记录 Unity 实际端口
- 2026-04-10 已实测删除 `.port` 后，触发一次 `TriggerCompile`，文件会自动重建为 `3212`

## 通过标准

完成以下 6 条即可判定“YIUI MCP 基础人工体验流程跑通”：

- Unity Console 出现 `端口配置已保存` 与 `启动成功，端口`
- `UTO/.port` 存在且内容正确
- `GET http://127.0.0.1:3212/health` 返回 `status=ok`
- `Config/get_console_log.ps1` 能返回真实日志
- `Config/invoke-uto-tool.ps1` 能成功调用单工具
- `Config/compile-unity-flow.ps1` 能完成 `StopPlayMode -> TriggerCompile -> GetCompileResult`

## 人工流程

### 步骤1：确认 Unity Editor 侧已启动 YIUIMCP

人工动作：

- 打开 Unity 工程并等待编译完成
- 查看 Unity Console
- 如需打开工具面板，优先看 `ET/YIUIMCP`

预期结果：

- Console 中出现类似以下日志：
  - `[YIUIMCP] 端口配置已保存: 3212`
  - `[YIUIMCP] 启动成功，端口: 3212`
- 如果打开 `YIUIMCP` 面板，能看到：
  - 服务器状态为“运行中”
  - 端口显示为 `3212`
  - 可见启动/停止/重启/强制启动等按钮

失败先查：

- 是否仍有 `cn.etetet.yiuimcp` 自身编译错误
- Unity Console 是否有 `YIUIMCP` 启动失败日志
- `127.0.0.1:3212` 是否已被其他进程占用

### 步骤2：确认 `.port` 自动写回

人工动作：

- 检查 `Packages/cn.etetet.yiuimcp/UTO/.port`
- 文件应存在且内容应为当前 Unity MCP 实际端口

PowerShell：

```powershell
Get-Content -Raw .\Packages\cn.etetet.yiuimcp\UTO\.port
```

预期结果：

- 输出 `3212`

补充验证：

- 如需验证自动写回，可删除 `.port` 后执行一次编译流程
- 当前工程已在 2026-04-10 实测通过该验证

### 步骤3：确认 Unity MCP `/health`

PowerShell：

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:3212/health' -Method Get -TimeoutSec 5
```

预期结果：

- 返回 `status = ok`
- 返回 `pid`
- 返回 `serverId`

当前实测示例：

```json
{
  "status": "ok",
  "pid": 18724,
  "serverId": "9ceabce4-03f2-4b01-8b0a-cef32d50020f"
}
```

### 步骤4：确认日志回读脚本可用

PowerShell：

```powershell
& .\Packages\cn.etetet.yiuimcp\Config\get_console_log.ps1
```

预期结果：

- 能自动启动临时 UTO HTTP Server
- 能输出 `UTO 已就绪`
- 能返回 `GetConsoleLog` 的结果文本

说明：

- 这是当前最稳妥的“只读检查”入口
- 适合先确认脚本层、UTO 层、Unity 层三段链路是否同时可用

### 步骤5：确认单工具调用可用

推荐入口：

```powershell
$json = '{"message":"ManualFlowSmoke"}'
$base64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
& .\Packages\cn.etetet.yiuimcp\Config\invoke-uto-tool.ps1 -Tool 'Log' -ParamsBase64 $base64
```

预期结果：

- 输出 `UTO 已就绪`
- 输出 `SUCCESS`
- 返回 `Log, success`

回读确认：

```powershell
& .\Packages\cn.etetet.yiuimcp\Config\get_console_log.ps1
```

预期应能在 Console 日志里看到：

- `ManualFlowSmoke`

当前实测结果：

- 2026-04-10 已验证 `invoke-uto-tool.ps1` 可成功调用 `Log`
- 2026-04-10 已验证 `get_console_log.ps1` 可回读到 `ManualFlowSmoke`

### 步骤6：确认完整编译流程可用

PowerShell：

```powershell
& .\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1 -Force $false
```

预期结果：

- 输出 `UTO 已就绪`
- 顺序执行：
  - `StopPlayMode`
  - `TriggerCompile`
  - `GetCompileResult`
- 最终输出 `Result: Success, No errors!`

当前实测结果：

```text
✓ StopPlayMode
✓ TriggerCompile
✓ GetCompileResult
Result: Success, No errors!
```

说明：

- 这是当前最推荐的“编译相关人工体验入口”
- 比直接手敲 UTO `/batch` 更稳，也更贴近项目现有使用方式

### 步骤7：可选的 HTTP 层直连验证

适用场景：

- 想把“脚本包装层”与“UTO/Unity 本体链路”拆开验证

#### 7.1 启动 UTO HTTP

```powershell
Set-Location .\Packages\cn.etetet.yiuimcp\UTO
node build/index.js --http
```

另开一个 PowerShell 验证：

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:3213/health' -Method Get -TimeoutSec 5
```

预期结果：

- 返回 `status = ok`
- 返回 `heartbeatReady = true`
- 返回 `unityPort = 3212`

#### 7.2 直接调用 `/call`

```powershell
$body = @{
    tool = 'Log'
    params = @{
        message = 'ManualFlowSmokeViaHttp'
    }
} | ConvertTo-Json -Depth 10 -Compress

Invoke-RestMethod -Uri 'http://127.0.0.1:3213/call' -Method Post -Body $body -ContentType 'application/json' -TimeoutSec 30
```

预期结果：

- `success = true`
- `result = "Log, success"`

说明：

- 当前人工流程的强制项到 `Step 6` 即可
- `/call` 直连主要用于拆层定位问题

### 步骤8：可选的实际页面资源构建验证

适用场景：

- 想确认 `ExecuteMenu` 不只是能调通空工具，而是能真实驱动某个 YIUI 页面资源构建

PowerShell：

```powershell
$json = '{"menuPath":"ET/YIUI/Build Home Resources"}'
$base64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
& .\Packages\cn.etetet.yiuimcp\Config\invoke-uto-tool.ps1 -Tool 'ExecuteMenu' -ParamsBase64 $base64
```

预期结果：

- 返回 `ExecuteMenu, 成功执行菜单命令: ET/YIUI/Build Home Resources`
- Unity Console 中出现 `[HomeYiuiBuilder] Home resources generated.`
- 目标 prefab 中可看到 `HomeRuntimeRoot` 等节点已落盘

## 当前推荐顺序

日常人工体验或回归时，建议按下面顺序执行：

1. 看 Unity Console，确认 YIUIMCP 已启动
2. 看 `UTO/.port`
3. 打 Unity `/health`
4. 跑 `get_console_log.ps1`
5. 跑 `invoke-uto-tool.ps1 -Tool Log`
6. 再跑 `compile-unity-flow.ps1`

这样从最轻量的读检查逐步进入写操作和编译操作，定位问题更快。

## 当前观察结论

### 已确认可用

- Unity `/health`
- `.port` 自动写回
- `get_console_log.ps1`
- `invoke-uto-tool.ps1`
- `compile-unity-flow.ps1`
- UTO `/health`
- UTO `/call`
- `ExecuteMenu -> ET/YIUI/Build Home Resources`

### 当前不在本轮处理范围

- `UTO/node_modules` 导入过重
- 新测试页面方案
- 与 `cn.etetet.statesync` 现有错误相关的整仓编译问题

## 下一步建议

在这份流程跑通后，再进入下一阶段：

1. 继续选择更多 YIUI 页面作为 `ExecuteMenu` 体验对象
2. 定义每个页面需要验证的资源构建和生成功能集合
3. 再输出更系统的“测试页面方案”

## 关联文档

- [YIUI MCP接入与报错排查说明](YIUI%20MCP接入与报错排查说明.md)
- [YIUI MCP编译阻塞修复设计文档](YIUI%20MCP编译阻塞修复设计文档.md)
- [YIUI MCP编译阻塞修复开发日志](YIUI%20MCP编译阻塞修复开发日志.md)
