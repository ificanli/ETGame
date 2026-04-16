# YIUI MCP编译阻塞修复设计文档

**创建时间**：2026-04-10  
**最后更新**：2026-04-10  
**状态**：已完成  
**关联任务**：M0.2-W3 #12  
**涉及包**：cn.etetet.yiuimcp

## 需求概述

- 修复导入 `cn.etetet.yiuimcp` 后当前真正挡住 Unity 编译的依赖错误
- 本轮不处理 `UTO/node_modules` 导入过重问题
- 为后续完整人工体验 `Unity MCP -> UTO -> Config/*.ps1` 链路清掉首个阻塞点

## 技术方案

### 整体思路

当前明确报错来自 `YIUIMCPLog.cs` 对 `Unity.EditorCoroutines.Editor` 的引用。理论上可以通过补 `com.unity.editorcoroutines` 解决，但在当前工程实测中，单纯依赖声明并不是最稳妥的最终收口方案，因此本轮改为直接移除该编译期依赖。

在修完首个阻塞后，继续暴露出第二个 `YIUIMCP` 编译阻塞：`YIUIMCPToolBar.cs` 仍强依赖当前工程里不存在的 `YIUIToolbarExtender` 与 `YIUIAutoTool.SelectModule`。这一层本质是 YIUI 编辑器 API 漂移，因此同样采用“去强依赖、保留功能”的兼容改法。

本轮采用最小修复方案：

1. 先补齐设计文档、开发日志和计划状态，满足 `et-design` 门禁
2. 将 `YIUIMCPLog.ShowNotification` 从 `EditorCoroutineUtility` 改为基于 `EditorApplication.update` 的定时移除通知
3. 保留通知能力，但彻底移除对 `Unity.EditorCoroutines.Editor` 的编译期依赖
4. 将 `YIUIMCPToolBar` 改为反射注册 toolbar、反射切换模块，并补一个 `ET/YIUIMCP` 菜单兜底入口
5. 在 `YIUIMCPServer.Start()` 成功后自动把当前端口写回 `Packages/cn.etetet.yiuimcp/UTO/.port`
6. 用 `Editor.log`、`/health`、删 `.port` 后触发 `TriggerCompile` 的重建结果，以及 `Config/get_console_log.ps1`、`Config/invoke-uto-tool.ps1`、`Config/compile-unity-flow.ps1` 验证 `YIUIMCP` 编译与基础调用链
7. 形成一份完整人工体验流程文档，作为后续测试页面方案的前置基线

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPLog.cs` | 修改 | 移除 `EditorCoroutineUtility` 依赖，改为 `EditorApplication.update` 计时移除通知 |
| `Packages/cn.etetet.yiuimcp/Editor/YIUIModule/YIUIMCPToolBar.cs` | 修改 | 去掉对缺失 YIUI 编辑器 API 的强依赖，改为反射兼容和菜单兜底 |
| `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServer.cs` | 修改 | 服务启动成功后自动写回 `UTO/.port` |
| `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServerConfig.cs` | 修改 | 统一端口文件路径与写回逻辑，保证 `.port` 自动生成 |
| `Packages/cn.etetet.yiuimcp/Config/invoke-uto-tool.ps1` | 修改 | 持有并回收临时 UTO 进程，修复单工具入口的启动不稳定问题 |
| `Packages/cn.etetet.yiuimcp/AGENTS.md` | 新增 | 补齐包级规范文件，约束 UnityMCP / UTO / PowerShell 三层修改边界 |
| `Book/10-项目架构/YIUI MCP完整人工体验流程.md` | 新增 | 沉淀当前工程真实可执行的人工体验流程 |
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 任务 12 状态推进到开发中并切换设计文档链接 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 记录任务 12 进入开发中 |

### Entity/Component 设计

本轮不新增 Entity、Component 或 System。

### 接口设计

本轮不新增消息、Handler 或 HTTP 接口。

### 数据结构

- 编辑器更新回调：`EditorApplication.update`
- 通知移除调度：`PendingNotifications`
- 反射兼容入口：`YIUIToolbarExtender.AddRightToolbarGUI`
- 反射切换模块：`YIUIAutoTool.SelectChainByPath`
- 动态端口同步文件：`Packages/cn.etetet.yiuimcp/UTO/.port`

## 实现步骤

1. 补齐 `et-plan` / `et-design` 所需的设计文档、开发日志和计划状态
2. 改造 `YIUIMCPLog.ShowNotification`，去掉对 `Unity.EditorCoroutines.Editor` 的引用
3. 改造 `YIUIMCPToolBar`，去掉对缺失 YIUI 编辑器 API 的强依赖
4. 结合 `Editor.log` 复核最新 `YIUIMCP` 阻塞是否已消失，再用 `/health` 与 `Config/get_console_log.ps1` 验证基础调用链
5. 验证删除 `UTO/.port` 后，触发一次 `TriggerCompile` / 域重载，文件会被自动写回
6. 修复单工具脚本入口的进程管理问题，确保 `invoke-uto-tool.ps1` 可稳定跑通
7. 新增完整人工体验流程文档
8. 更新设计文档实现追踪、开发日志和计划状态

## 验收标准

- [x] `YIUIMCPLog.cs` 已不再引用 `Unity.EditorCoroutines.Editor`
- [x] `ShowNotification` 仍保留“显示后自动移除”的行为
- [x] `YIUIMCPToolBar.cs` 已不再强依赖 `YIUIToolbarExtender` 与 `YIUIAutoTool.SelectModule`
- [x] `YIUIMCP` `/health` 可访问
- [x] 删除 `UTO/.port` 后，服务重启能自动写回当前端口
- [x] `Config/get_console_log.ps1` 可返回真实结果
- [x] `Config/invoke-uto-tool.ps1` 可稳定调用单工具
- [x] `Config/compile-unity-flow.ps1` 可完成基础编译流程
- [x] 已沉淀完整人工体验流程文档
- [ ] `dotnet build ET.sln` 完成一次基线编译
- [x] Unity 最新编译结果已不再因 `YIUIMCP` 当前代码报编译错误

## 关联文档

- [YIUI MCP接入与报错排查说明](YIUI%20MCP接入与报错排查说明.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1：文档与计划门禁补齐 | 2026-04-10 | `Book/10-项目架构/YIUI MCP编译阻塞修复设计文档.md`、`Book/10-项目架构/YIUI MCP编译阻塞修复开发日志.md`、`Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md` | 无偏差 |
| 步骤2：移除 `EditorCoroutines` 编译期依赖 | 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPLog.cs` | 与初始设想存在偏差；最终未保留“补包依赖”方案，而是改为代码内去依赖 |
| 步骤3：修复 `YIUIMCPToolBar` API 漂移 | 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/YIUIModule/YIUIMCPToolBar.cs` | 直接改为反射兼容和菜单兜底，避免继续依赖当前工程不存在的 YIUI 编辑器 API |
| 步骤4：补齐 `.port` 自动写回 | 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServer.cs`、`Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServerConfig.cs` | 写回点落在 Unity 服务成功启动后，避免 UTO 与 Unity 端口状态脱节 |
| 步骤5：修复单工具入口稳定性 | 2026-04-10 | `Packages/cn.etetet.yiuimcp/Config/invoke-uto-tool.ps1` | 通过持有 `$utoProcess` 解决脚本自身的 UTO 启动不稳定问题 |
| 步骤6：编译与链路验证 | 2026-04-10 | `Editor.log`、`Config/get_console_log.ps1`、`Config/invoke-uto-tool.ps1`、`Config/compile-unity-flow.ps1`、`Packages/cn.etetet.yiuimcp/UTO/.port` | `dotnet build ET.sln` 仍被其他包现有错误挡住；但 Unity 最新 `YIUIMCP` 编译错误已消失，`/health` 与脚本调用已可用；实测删除 `.port` 后，触发 `TriggerCompile` 可在 2026-04-10 11:00:01 自动重建 `3212` |
| 步骤7：人工体验流程沉淀 | 2026-04-10 | `Book/10-项目架构/YIUI MCP完整人工体验流程.md` | 以 Unity Console、`.port`、`/health`、CLI 脚本和 UTO `/call` 为主线，形成首版人工体验基线 |
