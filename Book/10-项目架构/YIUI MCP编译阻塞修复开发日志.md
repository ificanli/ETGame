# YIUI MCP编译阻塞修复开发日志

**功能**：YIUI MCP编译阻塞修复  
**关联设计文档**：[YIUI MCP编译阻塞修复设计文档](YIUI%20MCP编译阻塞修复设计文档.md)  
**关联任务**：M0.2-W3 #12  
**开始时间**：2026-04-10  
**开发者**：AI

## 开发进度

- [x] 步骤1：确认当前真实编译阻塞与修复边界
- [x] 步骤2：移除 `EditorCoroutines` 编译期依赖
- [x] 步骤3：补齐 `.port` 自动写回
- [x] 步骤4：执行编译/日志验证并回写文档

## 决策记录

### 2026-04-10 - 先修真正挡住编译的依赖错误
- **背景**：用户明确要求先修当前真正挡住编译的问题，不处理 `UTO/node_modules` 导入过重。
- **方案**：本轮只处理 `Unity.EditorCoroutines.Editor` 缺依赖，不改 `UTO` 目录结构。
- **原因**：当前首个失败点已经明确，先解除编译阻塞才能进入后续人工体验流程。
- **替代方案**：先处理 `node_modules` 导入过重；放弃原因：这不是当前编译失败的根因，处理范围会被不必要放大。

### 2026-04-10 - 依赖版本采用 1.0.0
- **背景**：目标包 `package.json` 声明 `unity: 2022.3`，但当前 `Editor.log` 显示实际导入使用 Unity `6000.0.58f2`。
- **方案**：新增 `com.unity.editorcoroutines: 1.0.0`。
- **原因**：Unity 官方包文档可见 `1.0.0` 同时存在于 2022.3 与 6000.0 的 `Editor Coroutines` 文档版本中，是当前更稳妥的共同版本。
- **替代方案**：使用 `1.0.1`；放弃原因：该版本不适合直接作为 2022.3 包声明的保守选择。

### 2026-04-10 - 最终改为零外部依赖实现
- **背景**：实测过程中，仅补依赖声明并不能作为当前工程里的最终收口方案，而修复目标只是解除 `YIUIMCPLog.cs` 当前编译阻塞。
- **方案**：把通知自动移除逻辑改为 `EditorApplication.update` 轮询，到时后再 `RemoveNotification`。
- **原因**：这样可以保留原有使用体验，同时完全去掉 `Unity.EditorCoroutines.Editor` 编译期依赖。
- **替代方案**：继续坚持走 `com.unity.editorcoroutines` 方案；放弃原因：会把当前修复绑到额外的包解析状态上，收口不够稳。

### 2026-04-10 - `YIUIMCPToolBar` 改为反射兼容
- **背景**：首个阻塞解除后，继续暴露出 `YIUIMCPToolBar.cs` 对 `YIUIToolbarExtender` 和 `YIUIAutoTool.SelectModule` 的强依赖，而当前工程里的 `yiuiframework` 并不提供这两个公开 API。
- **方案**：toolbar 注册改为反射调用；打开模块改为 `EditorPrefs` 记住路径后，反射调用 `YIUIAutoTool.SelectChainByPath`；同时补一个 `ET/YIUIMCP` 菜单入口兜底。
- **原因**：这样既能适配当前 YIUI 版本，也不会把 `YIUIMCP` 绑死到某一版编辑器扩展 API。
- **替代方案**：继续直接依赖缺失 API；放弃原因：会持续导致编译失败。

### 2026-04-10 - `.port` 写回收口到 Unity 服务启动成功点
- **背景**：UTO 和 `Config/*.ps1` 会优先读取 `Packages/cn.etetet.yiuimcp/UTO/.port` 决定 Unity 端口；如果文件不同步，脚本链路会回退默认端口，动态端口场景会失真。
- **方案**：在 `YIUIMCPServer.Start()` 成功后立即调用 `YIUIMCPServerConfig.SavePortToFile(port)`，并统一 `.port` 路径与写回逻辑。
- **原因**：端口写回与“服务实际启动成功”绑定最稳，能确保 UTO 侧读到的是当前 Unity 实际监听端口。
- **替代方案**：只依赖默认端口 `3212`；放弃原因：这会让动态端口链路形同虚设，后续换端口时脚本侧会产生假成功或超时。

### 2026-04-10 - `invoke-uto-tool.ps1` 改为持有 UTO 进程句柄
- **背景**：在梳理完整人工体验流程时，`get_console_log.ps1` 与 `compile-unity-flow.ps1` 能稳定拉起 UTO，但 `invoke-uto-tool.ps1` 多次出现“UTO 启动超时”。
- **方案**：将 `invoke-uto-tool.ps1` 的 UTO 启动方式改为保存 `$utoProcess`，并统一在超时、调用失败和正常结束时按进程对象回收。
- **原因**：该脚本原先直接把 `Process` 对象丢给 `Out-Null`，而又开启了标准输出/错误重定向；这会让子进程生命周期管理不稳定，影响人工单工具体验。
- **替代方案**：仅在文档里标记该脚本不稳定；放弃原因：这是当前人工流程的一个真实入口，应该直接修掉而不是留给使用者绕开。

## 问题日志

### 2026-04-10 - 导入后 Unity 编译直接失败
- **现象**：`Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPLog.cs(2,13)` 报 `CS0234`。
- **原因**：`YIUIMCPLog.cs` 使用了 `Unity.EditorCoroutines.Editor`，但 `package.json` 未声明 `com.unity.editorcoroutines`。
- **解决**：最终直接移除该命名空间依赖，改为使用 `EditorApplication.update` 管理通知超时。

### 2026-04-10 - 项目仍存在与本次修复无关的编译错误
- **现象**：`dotnet build ET.sln` 失败于 `Packages/cn.etetet.statesync\Scripts\HotfixView\Client\M2C_WeaponFireHandler.cs`；Unity 最新编译又前移到 `M2C_WeaponHitHandler.cs` 与 `HitFeedbackHelper.cs` 等现有错误。
- **原因**：工程当前本就存在 `cn.etetet.statesync` 方向的未收口改动，与 `cn.etetet.yiuimcp` 当前阻塞无关。
- **解决**：本轮不扩散处理范围，只记录为后续体验前置阻塞。

### 2026-04-10 - `.port` 自动写回验证完成
- **现象**：前一轮排查时，`.port` 一度缺失，导致脚本链路只能依赖默认端口判断。
- **原因**：当时缺少“删文件后再通过真实服务重启验证”的闭环，无法确认写回逻辑是否真的生效。
- **解决**：补齐 `YIUIMCPServer.Start()` -> `YIUIMCPServerConfig.SavePortToFile(port)` 的写回链路后，实测删除 `Packages/cn.etetet.yiuimcp/UTO/.port`，再调用 `TriggerCompile` 触发 Unity 编译/域重载，文件会在 `2026-04-10 11:00:01` 自动重建，内容为 `3212`；随后 `/health` 与 `Config/get_console_log.ps1` 继续可用。

### 2026-04-10 - 完整人工体验流程已形成首版
- **现象**：基础链路虽然已经能用，但还缺一份按人工操作顺序执行的统一手册，后续体验和排查容易每次都从头摸索。
- **原因**：现有文档更偏架构说明、接入排查和局部脚本说明，缺少“先做什么、看到什么算成功、失败先查哪里”的执行视角。
- **解决**：新增 `Book/10-项目架构/YIUI MCP完整人工体验流程.md`，把 Unity 面板、`.port`、`/health`、`Config/*.ps1` 和 UTO `/call` 串成一条标准人工体验流程。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-10 | `Book/10-项目架构/YIUI MCP编译阻塞修复设计文档.md` | 新增 | 建立本轮修复设计文档 |
| 2026-04-10 | `Book/10-项目架构/YIUI MCP编译阻塞修复开发日志.md` | 新增 | 建立本轮修复开发日志 |
| 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPLog.cs` | 修改 | 去掉 `EditorCoroutineUtility`，改为 `EditorApplication.update` 驱动通知超时移除 |
| 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/YIUIModule/YIUIMCPToolBar.cs` | 修改 | 去掉对缺失 YIUI 编辑器 API 的强依赖，改为反射兼容和菜单兜底 |
| 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServer.cs` | 修改 | 服务启动成功后自动写回 `UTO/.port` |
| 2026-04-10 | `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServerConfig.cs` | 修改 | 统一 `.port` 路径与写回逻辑 |
| 2026-04-10 | `Packages/cn.etetet.yiuimcp/Config/invoke-uto-tool.ps1` | 修改 | 持有并回收临时 UTO 进程，修复单工具入口的启动不稳定问题 |
| 2026-04-10 | `Packages/cn.etetet.yiuimcp/AGENTS.md` | 新增 | 补齐包级规范文件 |
| 2026-04-10 | `Book/10-项目架构/YIUI MCP完整人工体验流程.md` | 新增 | 沉淀当前工程真实可执行的人工体验流程 |
| 2026-04-10 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 记录任务 12 当前修复结果与后续阻塞 |
| 2026-04-10 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 记录任务 12 当前修复结果 |

## 开发总结

> 开发结束后填写

- **实际完成**：
- 解除 `YIUIMCPLog.cs` 对 `Unity.EditorCoroutines.Editor` 的编译阻塞
- 解除 `YIUIMCPToolBar.cs` 对缺失 YIUI 编辑器 API 的编译阻塞
- 补齐 `UTO/.port` 自动写回链路，并完成删文件后自动重建验证
- 修复 `invoke-uto-tool.ps1` 的 UTO 启动不稳定问题
- 保留编辑器通知自动移除能力
- 通过 Unity 最新 `Editor.log` 确认当前 `YIUIMCP` 编译错误已消失
- 实测 `/health` 可访问，`Config/get_console_log.ps1` 已返回真实结果
- 产出《YIUI MCP完整人工体验流程》，沉淀当前工程真实可执行的端到端人工验证路径
- **未完成**：
- `dotnet build ET.sln` 仍未通过，原因是 `cn.etetet.statesync` 存在与本任务无关的现有错误
- 还未进入 YIUI MCP 的实际功能体验与测试页面方案
- **与设计的偏差**：
- 初始设想是补包依赖，最终改为代码内去依赖实现，原因是这样更稳、更小范围
- 新增了一处未在初始设计里显式列出的 `YIUIMCPToolBar` API 漂移兼容修复
- 为了验证动态端口链路，额外补做了“删除 `.port` -> `TriggerCompile` -> 自动重建”的端到端验证
- 在梳理人工流程过程中，新增收口了 `invoke-uto-tool.ps1` 的进程管理问题
- **后续待办**：
- 在不处理 `UTO/node_modules` 导入过重的前提下，继续清理会挡住“试用流程”的其他现有编译错误
- 继续设计完整人工体验流程，再进入测试页面方案
