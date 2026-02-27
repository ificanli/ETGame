# ECA 联调待办（上下文摘要）

> 目的：下次继续时快速恢复上下文与当前进度。

## 1. 关键已完成
- GraphView 参数模板已覆盖 Event/Condition/Action，保存时增加基础校验（空Key/无出口/Condition分支缺失）。
- FlowGraph 事件支持参数匹配（例如 `OnTimerElapsed` 按 `timer_id` 精确命中）。
- 新增流程图计时器：`ECAFlowTimerComponent` + `ECAFlowTimerHelper` + `TimerInvokeType.ECAFlowTimer`。
- 服务端动作骨架已落地：
  - `SetPointState` / `SetPointActive`
  - `StartSearchTimer` / `StartEvacCountdown`（触发 `OnTimerElapsed`）
  - `TransferToLobby` / `AllowEvacPlayers`
  - `SpawnItemsToGround` / `SpawnMonsters`（仅记录请求，未接入真实系统）
- ECANode 侧容器/刷怪点已加请求记录字段与对接入口。

## 2. 新增测试
- `Ecanode_ContainerFlow_Timer_Test`：验证计时触发 `OnTimerElapsed`，并设置点状态。
- `Ecanode_SpawnPoint_Request_Test`：验证刷怪请求记录写入组件。

## 3. 测试执行情况
- 尝试执行：`dotnet .\Bin\ET.App.dll --SceneName=Test` + `Test --Name=Ecanode_(ContainerFlow_Timer|SpawnPoint_Request)_Test`
- 进程两次超时未退出（含一次带 `Exit` 输入）。
- 清理 `Logs` 目录的命令被策略拦截（无法删除）。
- 本次未重新编译；此前编译失败原因仍是 Unity Test Framework 的过时 API（`Library/PackageCache/com.unity.test-framework.../SwitchPlatformSetup.cs`）。

## 4. 明确未做（按你要求）
- `BackpackWeightLE / KillCountGE / TaskState` 条件（数据源未指定）。
- “客户端表现事件接口”（UI/动画通知）。

## 5. 待办清单（联调前）
- 任务相关动作落地：`StartTask / GiveReward / StartAreaHold / StartEscort / BroadcastMap`。
- 接入真实掉落/刷怪系统（当前只是记录请求）。
- GraphView 节点类型继续细化。
- 关卡点位标注 + FlowGraph 资产绑定（你计划后续完成）。

## 6. 联调注意点
- `timer_id` 必须一致，否则 `OnTimerElapsed` 事件不会命中。
- FlowGraph 优先：点位绑定流程图后将优先走流程图逻辑。
- 撤离点无 FlowGraph 时仍走旧逻辑兜底。

---
**最后更新**：2026-02-27
