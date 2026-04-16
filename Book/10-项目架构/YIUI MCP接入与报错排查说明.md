# YIUI MCP接入与报错排查说明

**创建时间**：2026-04-10  
**最后更新**：2026-04-10  
**状态**：已完成  
**关联任务**：M0.2-W3 #12  
**涉及包**：cn.etetet.yiuimcp, cn.etetet.yiuiframework

## 需求概述

- 排查导入 `cn.etetet.yiuimcp` 后的当前报错
- 理清 `YIUI MCP` 在本项目中的真实职责、调用链和使用方式
- 为后续“接入 YIUI MCP 并进行体验”沉淀一份可复用说明

## 当前结论

### 1. 当前导入报错

Unity Editor 当前明确报错：

```text
Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPLog.cs(2,13):
error CS0234: The type or namespace name 'EditorCoroutines' does not exist in the namespace 'Unity'
```

直接原因：

- `YIUIMCPLog.cs` 引用了 `using Unity.EditorCoroutines.Editor;`
- `Packages/cn.etetet.yiuimcp/package.json` 当前只声明了 `com.unity.nuget.newtonsoft-json`
- 当前没有声明 `com.unity.editorcoroutines`

结论：

- 这是导入后第一优先级阻塞项
- 在缺少 `EditorCoroutines` 依赖时，Unity 侧代码无法通过编译，后续 MCP 服务和脚本链路都无法稳定工作

### 2. 当前脚本为什么会超时，以及现在是什么状态

`Config/get_console_error.ps1`、`get_console_log.ps1`、`invoke-uto-tool.ps1` 这几条脚本都依赖以下前提：

1. Unity 侧 MCP 服务成功启动
2. `Packages/cn.etetet.yiuimcp/UTO/.port` 已写入当前 Unity MCP 端口
3. UTO 能按 `Unity端口 + 1` 启动 HTTP 服务，并连到 Unity `/health`

首次排查时观察到：

- `UTO/.port` 不存在
- 直接调用上述脚本时出现超时

当时的根因链如下：

1. Unity 侧先被 `CS0234` 编译错误挡住
2. Unity MCP 没有形成稳定服务，也没有写出 `.port`
3. `Config/*.ps1` 只能按默认 `3212/3213` 启动 UTO 并等待
4. 最终表现为 UTO 或调用链超时

2026-04-10 最新状态：

- Unity 侧编译阻塞已解除
- `YIUIMCP` `/health` 已恢复可用
- `Packages/cn.etetet.yiuimcp/UTO/.port` 已接入自动写回
- 实测删除 `.port` 后，再触发一次 `TriggerCompile`，文件会在 `2026-04-10 11:00:01` 自动重建，内容为 `3212`
- `Config/get_console_log.ps1` 已能在当前链路下稳定返回真实结果

### 3. 当前还有一个非编译阻塞问题

`Packages/cn.etetet.yiuimcp/UTO/node_modules` 目前直接位于 Unity Package 目录下。导入时，Unity 把整包 `node_modules` 也视为资源参与扫描和导入。

本次 `Editor.log` 可见：

- 单次刷新新增导入文件 `4528` 个
- `Asset Pipeline Refresh` 总耗时约 `55` 秒

这不是当前编译错误的直接根因，但会显著拉低导入体验、刷新速度和版本库体积。

## 这个工具实际在做什么

### 1. 它不是单纯的“YIUI 页面编辑器”

`cn.etetet.yiuimcp` 实际是一个“Unity Editor 自动化基础设施”包，由三层组成：

1. UnityMCP：Unity Editor 进程内的本地 HTTP JSON-RPC 服务
2. UTO：Node 侧编排/代理层
3. `Config/*.ps1`：PowerShell 高聚合 CLI 入口

它提供的是“让外部脚本、AI、CLI 去调 Unity”的底层通道，而不是只面向某一个具体 YIUI 页面。

### 2. 各层职责

#### UnityMCP

- 位置：`Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/`
- 作用：
  - 在 Unity Editor 内暴露 `/health` 与 `/rpc`
  - 真正执行 Unity 原子工具
  - 把所有 Unity API 调用切回主线程

核心类：

- `YIUIMCPServer`
- `YIUIMCPServerHelper`
- `YIUIMCPDispatcher`
- `YIUIMCPToolsRegistry`
- `YIUIMCPExecutor`

#### UTO

- 位置：`Packages/cn.etetet.yiuimcp/UTO/`
- 作用：
  - 提供 HTTP `/call`、`/batch`、`/tools`
  - 通过 MCP stdio 子进程把调用转发到 Unity `/rpc`
  - 负责批量调用、心跳检测、Domain Reload 恢复

核心文件：

- `UTO/src/index.ts`
- `UTO/src/mcp-client.ts`
- `UTO/src/http-server.ts`
- `UTO/src/heartbeat-manager.ts`

#### Config 脚本

- 位置：`Packages/cn.etetet.yiuimcp/Config/*.ps1`
- 作用：
  - 提供 CLI-first 的高聚合入口
  - 让调用方优先用“一条脚本命令拿最终结果”，而不是手工串低层 RPC

当前脚本入口：

- `compile-unity-flow.ps1`
- `get_console_log.ps1`
- `get_console_error.ps1`
- `invoke-uto-tool.ps1`

### 3. 当前真实可用的 Unity 原子工具

- `Log`
- `LogError`
- `EnterPlayMode`
- `StopPlayMode`
- `TriggerCompile`
- `GetCompileResult`
- `GetConsoleLog`
- `ExecuteMenu`
- `AssertConsoleContains`

注意：

- 当前 `GET /tools` 只是最小静态列表，不是完整工具发现接口
- 当前 `ListTools` 并没有在 Unity 侧实现

### 4. 它和项目里的 `yiui-unity-mcp` skill 不是一回事

项目根 `Agents/skills/yiui-unity-mcp.md` 关注的是：

- 如何用 Unity MCP + YIUI 正确改 `Panel / Item / prefab / YIUIGen / 手写层`
- 属于“YIUI 页面资源结构与生成衔接规范”

而 `cn.etetet.yiuimcp` 包本身提供的是：

- Unity 编辑器自动化能力
- UTO 编排能力
- CLI 脚本入口

两者关系是：

- `yiui-unity-mcp` skill 负责定义“该怎么用”
- `cn.etetet.yiuimcp` 包负责提供“拿什么去调 Unity”

## 当前使用方式

### 推荐调用顺序

1. 先确保 Unity Editor 无编译错误
2. 确认 Unity MCP 已正常启动
3. 确认 `UTO/.port` 已生成
4. 单步调试时，用 `invoke-uto-tool.ps1`
5. 编译、批量调用、等待 Domain Reload 恢复时，优先用 `compile-unity-flow.ps1` 或 UTO `/batch`

### 当前脚本的真实语义

- `compile-unity-flow.ps1`
  - 实际执行 `StopPlayMode -> TriggerCompile -> GetCompileResult`
- `get_console_log.ps1`
  - 实际调用 `GetConsoleLog`
- `get_console_error.ps1`
  - 名称看起来像“获取错误日志”，但当前实际调用的是 `GetCompileResult`
- `invoke-uto-tool.ps1`
  - 是最通用的单工具入口

## 本次接入体验结论

### 正向价值

- 把 Unity Editor 可编排能力从“人工点菜单”变成“脚本/AI 可调用工具”
- 对编译、日志、菜单执行、批量调用很有价值
- 适合作为后续 `YIUI prefab + 生成链 + 手写层` 接入的底层桥梁

### 当前阻塞

- `YIUIMCP` 代码层当前编译阻塞已解除，但“接入并进行体验”的完整人工流程还没开始执行
- `dotnet build ET.sln` 仍会被与本任务无关的 `cn.etetet.statesync` 现有错误挡住，因此整仓编译口径暂时不能作为 `YIUIMCP` 收口依据

### 当前噪音

- `UTO/node_modules` 直接放在 Unity Package 内，导入成本过高

## 建议处理顺序

### 1. 先修依赖问题

可选方案：

- 方案 A：在 `package.json` 增加 `com.unity.editorcoroutines`
- 方案 B：如果 `ShowNotification` 不必须依赖协程，则移除 `EditorCoroutineUtility` 依赖，改成不依赖额外包的实现

### 2. 再验证服务链路

验证目标：

- Unity 无编译错误
- Unity MCP 能成功启动
- `UTO/.port` 能自动写出并在服务重启后重建
- `get_console_log.ps1` / `invoke-uto-tool.ps1` 能拿到真实返回

### 3. 最后再进入 YIUI 体验

在基础设施通了之后，再进入项目侧 `yiui-unity-mcp` 规范验证：

- prefab 节点定位
- `YIUIGen` 生成
- 手写 `Component/System` 接线
- 旧入口 helper 收口

## 关键文件索引

| 文件 | 作用 |
|------|------|
| `Packages/cn.etetet.yiuimcp/package.json` | 包依赖声明 |
| `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPLog.cs` | 当前报错直接来源 |
| `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServer.cs` | Unity 侧 `/health` `/rpc` 入口 |
| `Packages/cn.etetet.yiuimcp/Editor/UnityMCP/Core/YIUIMCPServerHelper.cs` | Unity MCP 启停与健康管理 |
| `Packages/cn.etetet.yiuimcp/UTO/src/http-server.ts` | UTO HTTP 编排层 |
| `Packages/cn.etetet.yiuimcp/Config/get_console_error.ps1` | 当前“报错脚本”，实为编译结果摘要 |
| `Packages/cn.etetet.yiuimcp/Config/invoke-uto-tool.ps1` | 通用单工具入口 |
| `Agents/skills/yiui-unity-mcp.md` | 项目侧 YIUI 使用规范 |

## 结论

- `cn.etetet.yiuimcp` 当前不是 YIUI 业务逻辑本身，而是 Unity 编辑器自动化基础设施
- 本次导入后的首要报错是 `YIUIMCPLog.cs` 对 `Unity.EditorCoroutines.Editor` 的编译依赖
- 2026-04-10 已通过改造 `YIUIMCPLog.ShowNotification` 的实现移除该编译期依赖，Unity 最新编译阻塞已不再停在这条错误上
- 同日继续修复了 `YIUIMCPToolBar.cs` 对旧 YIUI 编辑器 API 的强依赖，当前 `YIUIMCP` 代码层编译错误已清除
- 当前 `YIUI MCP` 基础链路已可访问 `/health`，`Config/get_console_log.ps1` 也能返回真实结果；`.port` 自动写回也已通过删文件重建验证，后续重点转向完整人工体验流程与测试页面方案

补充：

- 完整人工操作步骤见：[YIUI MCP完整人工体验流程](YIUI%20MCP完整人工体验流程.md)
