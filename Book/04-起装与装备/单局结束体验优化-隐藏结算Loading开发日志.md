# 单局结束体验优化-隐藏结算Loading开发日志

**功能**：单局结束体验优化（隐藏结算 Loading）修复
**关联设计文档**：[单局结束体验优化-隐藏结算Loading修复设计文档.md](./单局结束体验优化-隐藏结算Loading修复设计文档.md)
**关联任务**：M0.2-W1 #11
**开始时间**：2026-03-29
**开发者**：AI

## 开发进度

- [x] 步骤1：补设计文档与开发日志，明确当前复现现象与修复边界
- [x] 步骤2：修正结算页在回到 Home 过程中的打开时序
- [x] 步骤3：编译验证并回写计划/文档

## 决策记录

### 2026-03-29 - 结算页不在 SceneChangeFinish 早阶段抢开
- **背景**：死亡结算路径中，`Settlement` 界面会先弹出，再被大厅界面顶掉。
- **方案**：不改服务端消息，不回退隐藏 Loading，只约束结算页必须在 `Home` 场景且 `LobbyPanel` 已就绪后再打开。
- **原因**：问题本质是 UI 初始化时序冲突，不是结算数据丢失；只修打开条件能最小化改动范围。
- **替代方案**：直接在 `SceneChangeFinishEvent_CreateUIHelp` 中无条件重开结算页。放弃原因是这会掩盖过早打开的问题，仍可能产生闪屏。

### 2026-03-29 - 结算页优先于主界面展示
- **背景**：用户进一步确认期望流程是“先看结算，后台换场景，点击后再回主界面”，而不是“回到大厅后自动弹结算”。
- **方案**：改为收到结算消息就立即打开 `SettlementPanel`；`Home` 到达时若结算未关闭则不自动打开 `LobbyPanel`；关闭结算后再补开大厅。
- **原因**：`LobbyPanel` 与 `SettlementPanel` 同属 `EPanelLayer.Panel`，只有把大厅打开时机延后，才能稳定满足用户体验要求。
- **替代方案**：把 `SettlementPanel` 改到更高 UI 层。放弃原因是这会引入 prefab / 框架层语义调整，本轮没有必要扩大改动面。

## 问题日志

### 2026-03-29 - 死亡结算界面弹出后瞬间消失
- **现象**：死亡后回到大厅时，`Settlement` 会闪一下，然后只剩大厅界面。
- **原因**：初步定位为 `SceneChangeFinish_ShowSettlementPanel` 先打开并清掉 `PendingOpen`，后续 `LobbyPanel` 打开时把结算页顶掉，导致不会重新打开。
- **解决**：在 `SceneChangeFinish_ShowSettlementPanel` 中增加 `Home + LobbyPanel 已激活` 前置条件，避免大厅初始化前抢先打开结算页。

### 2026-03-29 - 最新目标调整为“结算先展示，点击后再回主界面”
- **现象**：上一轮修完后，死亡时会先空一段 UI，再先看到主界面，最后才看到结算界面，不符合验收预期。
- **原因**：当前主链路仍然是 `Home` 到达后自动打开 `LobbyPanel`，结算页只是在回城后的附加展示。
- **解决**：改为结算页立即打开、后台切场、`Home` 到达时压住 `LobbyPanel`，等结算关闭后再打开大厅。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-03-29 | `Book/04-起装与装备/单局结束体验优化-隐藏结算Loading修复设计文档.md` | 新增 | 建立任务 11 的设计文档 |
| 2026-03-29 | `Book/04-起装与装备/单局结束体验优化-隐藏结算Loading开发日志.md` | 新增 | 建立任务 11 的开发日志 |
| 2026-03-29 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/EventSettlementPopup_OpenPanel.cs` | 修改 | 收到结算事件后立刻尝试打开结算页 |
| 2026-03-29 | `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeStart_AddComponent.cs` | 修改 | 有结算态回 `Home` 时跳过 `LoadingPanel` |
| 2026-03-29 | `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs` | 修改 | `Home` 到达后若结算仍未关闭则先不自动打开大厅 |
| 2026-03-29 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SceneChangeFinish_ShowSettlementPanel.cs` | 修改 | 在 `Home` 到达后可补开结算页，但不再依赖大厅先打开 |
| 2026-03-29 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Common/SettlementPanelComponentSystem.cs` | 修改 | 点击关闭结算后，若当前已回到 `Home`，则再打开大厅 |
| 2026-03-29 | `Book/08-版本计划/M0.2-W1周计划.md` | 修改 | 任务 11 挂接设计文档并在修复完成后推进到未回归 |
| 2026-03-29 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 补任务 11 二次修复记录 |

## 开发总结

> 开发结束后填写

- **实际完成**：已按“结算先展示、后台换场景、关闭后再回主界面”的新目标重排 UI 时序；`dotnet build ET.sln` 已通过。
- **未完成**：无。
- **与设计的偏差**：无。
- **后续待办**：如后续要统一撤离/超时路径体验，再看是否复用同一时序策略。
