# 单局结束体验优化-隐藏结算Loading修复设计文档

**创建时间**：2026-03-29
**最后更新**：2026-03-29
**状态**：已完成
**关联任务**：M0.2-W1 #11
**涉及包**：cn.etetet.map, cn.etetet.equipment, cn.etetet.statesync

## 需求概述

当前“单局结束体验优化：死亡 / 撤离 / 超时后直接弹结算，不显示 Loading 界面”在死亡路径经历了两轮验收调整。

上一轮实际现象：

- 玩家死亡后回到大厅流程中，`Settlement` 结算界面会先弹出；
- 随后结算界面瞬间消失；
- 客户端最终停留在大厅界面；
- 当前已确认的复现口径是“死亡路径”。

用户最新确认的目标行为为：

- 死亡后先显示 `Settlement` 结算界面；
- 后台继续切场回 `Home`；
- 切场过程中不显示 `Loading`；
- 玩家点击关闭 `Settlement` 后，再进入大厅主界面。

本次修复目标不是回退“隐藏 Loading”的方案，而是重排结算页与大厅 UI 的时序，让“结算优先展示、主界面延后打开”成为正式流程。

## 技术方案

### 整体思路

现有实现里的关键节点如下：

1. 服务端发 `M2C_DeathSettlement` / `M2C_EvacuationSettlement`
2. 客户端写入 `SettlementClientComponent`，并发 `EventSettlementPopup`
3. `SceneChangeStart_AddComponent` 决定是否显示 `LoadingPanel`
4. `SceneChangeFinishEvent_CreateUIHelp` 在 `Home` 到达后默认打开 `LobbyPanel`
5. `SettlementPanelComponent.OnEventClickExitPanelInvoke` 在玩家点击结算关闭按钮时执行

旧实现的主要问题有两个：

1. `EventSettlementPopup_OpenPanel` 只允许在 `Home` 打开结算页，导致死亡瞬间看不到结算，只能等回城。
2. `LobbyPanel` 与 `SettlementPanel` 都在 `EPanelLayer.Panel`，如果 `Home` 到达后自动先开大厅，再开结算，就一定会出现两者互相顶替的问题。

### 修复策略

修复原则：

1. 不改服务端结算消息时机；
2. 不回退 `skipLoadingForSettlement`；
3. 让 `SettlementPanel` 成为回城过程中的主展示 UI；
4. `LobbyPanel` 必须延后到结算关闭后再打开。

具体方案：

- `EventSettlementPopup_OpenPanel`
  - 改为收到结算事件后就直接尝试打开 `SettlementPanel`，不再要求当前场景必须已经是 `Home`
- `SceneChangeStart_AddComponent`
  - 只要当前存在结算态并且目标场景是 `Home`，就跳过 `LoadingPanel`
- `SceneChangeFinishEvent_CreateUIHelp`
  - 回到 `Home` 后，如果结算仍未关闭，则不自动打开 `LobbyPanel`
  - 只有“没有结算态”时才走默认大厅打开流程
- `SettlementPanelComponent.OnEventClickExitPanelInvoke`
  - 关闭结算时，如果当前已经回到 `Home`，则立即补开 `LobbyPanel`
  - 如果当前尚未回到 `Home`，则不额外处理，等场景切换完成后由默认 `Home` 流程打开大厅

这样时序会变成：

1. 死亡消息到达，立刻显示 `SettlementPanel`
2. 服务端继续调度回 `Home`
3. 客户端后台完成切场，但不显示 `Loading`
4. `Home` 到达后先保持 `SettlementPanel`
5. 玩家点击关闭 `SettlementPanel`
6. 此时再打开 `LobbyPanel`

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/EventSettlementPopup_OpenPanel.cs` | 修改 | 收到结算事件时立刻尝试打开结算页 |
| `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeStart_AddComponent.cs` | 修改 | 有结算态回 `Home` 时跳过 `LoadingPanel` |
| `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs` | 修改 | `Home` 到达后若结算未关闭则先不打开大厅 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SceneChangeFinish_ShowSettlementPanel.cs` | 修改 | 在 `Home` 到达后可补开结算页，但不再依赖大厅先打开 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SettlementPanelComponentSystem.cs` | 修改 | 点击关闭结算时在 `Home` 补开大厅 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SettlementPanelHelper.cs` | 核对 | 保持 `MarkShown()` 只在真正打开/刷新结算页后执行 |

## Entity/Component 设计

本次修复不新增 Entity / Component，只调整现有 UI 打开时序。

涉及的现有运行时组件：

- `SettlementClientComponent`
- `LobbyPanelComponent`
- `SettlementPanelComponent`

## 接口设计

本次修复不新增协议，不修改消息结构：

- `M2C_DeathSettlement`
- `M2C_EvacuationSettlement`

## 数据结构

本次修复不新增配置、不新增字段，继续复用：

- `SettlementClientComponent.HasSettlement`
- `SettlementClientComponent.PendingOpen`

## 实现步骤

1. 创建任务 11 的设计文档和开发日志，明确根因与修复边界。
2. 修改结算事件、切场开始、切场结束和结算关闭这 4 个入口，完成“结算优先、主界面延后”的时序调整。
3. 用 `dotnet build ET.sln` 验证编译通过。
4. 回写开发日志和周计划，等待死亡路径实机回归。

## 验收标准

- [ ] 死亡后第一时间显示 `Settlement`，不再先看到空白或主界面
- [ ] 后台切场回 `Home` 时不显示 `Loading`
- [ ] `Home` 到达后，在用户关闭结算前不自动打开 `LobbyPanel`
- [ ] 点击关闭 `Settlement` 后再进入主界面
- [ ] 隐藏结算 `Loading` 的现有行为保留
- [ ] `dotnet build ET.sln` 通过

## 关联文档

- [M0.2-W1周计划.md](../08-版本计划/M0.2-W1周计划.md)
- [模块现状总览.md](../10-项目架构/模块现状总览.md)

## 实现追踪

> 开发完成后补充

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 创建设计文档与开发日志 | 2026-03-29 | `Book/04-起装与装备/单局结束体验优化-隐藏结算Loading修复设计文档.md`、`Book/04-起装与装备/单局结束体验优化-隐藏结算Loading开发日志.md` | 无 |
| 修正“结算优先、主界面延后”时序 | 2026-03-29 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/EventSettlementPopup_OpenPanel.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SceneChangeFinish_ShowSettlementPanel.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SettlementPanelComponentSystem.cs`、`Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeStart_AddComponent.cs`、`Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs` | 无 |
| 编译验证与回写计划 | 2026-03-29 | `Book/08-版本计划/M0.2-W1周计划.md`、`Book/08-版本计划/M0.2版本计划.md` | 无 |
