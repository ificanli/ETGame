# ECA 流程图编辑器开发计划（方案A）

> 目标：在现有“点位导出”链路基础上，新增 **流程图编辑器（类似 BT）**，并将流程图数据 **嵌入点位配置导出**，用于运行时驱动交互逻辑。

## 1. 概念区别（你确认的定义）
- **点位编辑器**：从 Scene 导出 **位置/范围** 数据（现有 `ECAPointMarker + Export`）。
- **流程图编辑器**：类似 BT 的逻辑图（事件/条件/动作/状态），描述交互流程。

## 2. 现状总结（基线）
- 点位导出链路已存在，只包含 `ConfigId / PointType / InteractRange / Pos`。
- `PointType=2/3` 在运行时已有分支，但逻辑是 TODO。
- ECANode 仅实现撤离点，作为新增节点模板。

## 3. 总体方案（方案A）
1. **保持点位导出不变**（位置/范围仍来自 Scene）。
2. **新增流程图资产**（Graph 数据结构）。
3. **点位绑定流程图**：点位引用流程图资产。
4. **导出时将流程图数据嵌入点位配置**（单文件输出）。

## 4. 客户端/服务端边界与同步策略（已确认）
- **客户端**：范围判断与按钮/UI/动画表现（本地预测，仅表现）。
- **服务端**：权威状态、条件判断、计时、奖励、撤离等核心逻辑。
- **同步方式**：服务端下发状态变更与倒计时开始/取消（携带开始时间 + 时长），客户端以服务端为准校正 UI。
- **规则**：表现可本地预估，但结果与状态必须以服务端为准。
- **撤离倒计时**：必须由服务端通知开始/取消/完成。

## 5. 最小节点集合（来自需求抽取）
> 具体参数清单在“架子搭好后”再细化；先保证结构可扩展。

### 事件 Event
- `OnPlayerEnterRange` / `OnPlayerLeaveRange`
- `OnPlayerInteract`
- `OnTimerElapsed`（搜索/撤离倒计时）
- `OnCombatTimeReached`（战斗进行 x 秒后）
- `OnTaskComplete`（任务完成）
- `OnSwitchPulled`（拉闸触发）
- `OnTargetKilled`（击杀小怪/首领）
- `OnAreaHoldCompleted`（占领完成）
- `OnEscortArrived`（护送到达）

### 条件 Condition
- `PlayerInRange`
- `PointStateEquals`（state）
- `BackpackWeightLE`（max_weight）
- `KillCountGE`（min_kill）
- `TaskState`（task_id, state）
- `PointTypeEquals`（point_type）

### 动作 Action
- `SetPointState`
- `SetPointActive`
- `ShowInteractButton` / `HideInteractButton`
- `StartSearchTimer` / `ShowSearchUI`
- `OpenContainerUI`
- `SpawnItemsToGround`
- `PlayOpenAnim`
- `SpawnMonsters`
- `StartEvacCountdown` + `ShowEvacUI`
- `AllowEvacPlayers` / `TransferToLobby`
- `BroadcastMap`
- `ShowTaskAcceptUI` / `StartTask` / `GiveReward`
- `StartAreaHold` / `StartEscort`
- `StartZipline`
- `ApplyStealth` / `RemoveStealth`

## 6. 参数方案草案（待确认）
> 说明：用于 Key 下拉与 Params 模板自动填充；你确认后落地到编辑器与运行时。

### Event（事件）
- `OnPlayerEnterRange`（无参）
- `OnPlayerLeaveRange`（无参）
- `OnPlayerInteract`（无参）
- `OnTimerElapsed`：`timer_id`
- `OnCombatTimeReached`：`seconds`
- `OnTaskComplete`：`task_id`
- `OnSwitchPulled`：`switch_id`
- `OnTargetKilled`：`target_id` / `group_id`
- `OnAreaHoldCompleted`：`area_id`
- `OnEscortArrived`：`escort_id`

### Condition（条件）
- `PlayerInRange`（无参）
- `PointStateEquals`：`state`
- `BackpackWeightLE`：`max_weight`
- `KillCountGE`：`min_kill`
- `TaskState`：`task_id`, `state`
- `PointTypeEquals`：`point_type`

### Action（动作）
- `SetPointState`：`state`
- `SetPointActive`：`active`
- `ShowInteractButton` / `HideInteractButton`：`button_id`
- `StartSearchTimer`：`seconds`, `timer_id`
- `ShowSearchUI`：`ui_id`
- `OpenContainerUI`：`ui_id`
- `SpawnItemsToGround`：`loot_table`, `count`, `radius`
- `PlayOpenAnim`：`anim_id`
- `StartEvacCountdown`：`seconds`, `reason`, `timer_id`
- `ShowEvacUI`：`ui_id`
- `AllowEvacPlayers`（无参）
- `TransferToLobby`：`map_name`
- `SpawnMonsters`：`group_id`, `count`
- `BroadcastMap`：`message_id`
- `ShowTaskAcceptUI`：`ui_id`
- `StartTask`：`task_id`
- `GiveReward`：`reward_id`
- `StartAreaHold`：`area_id`, `seconds`
- `StartEscort`：`route_id`, `target_id`
- `StartZipline`：`zipline_id`
- `ApplyStealth`：`seconds`
- `RemoveStealth`（无参）

## 6. 数据结构设计（草案）
### 6.1 FlowGraphAsset（ScriptableObject）
- `Nodes[]`：节点列表（含类型与参数）
- `Connections[]`：连线列表
- `Version / Meta`：版本信息

### 6.2 ECAConfig 扩展（方案A嵌入）
- 点位信息保持不变
- 增加 `FlowGraph`（序列化结构）

### 6.3 点位绑定
- `ECAPointMarker` 增加 `FlowGraphAsset` 引用字段
- 导出时将图数据写入 `ECAConfig`

## 7. GraphView 编辑器实现
- 新建 GraphView 窗口（创建节点 / 连线 / 拖拽 / 保存 / 加载）
- 节点视图与 `FlowGraphAsset` 双向同步
- 支持最小节点集合

## 8. 运行时流程执行器
- 新增流程运行组件与系统（Hotfix/Server）
- `PointType=2/3` 触发流程图事件
- **撤离点优先走流程图**（无流程图时走旧逻辑兜底）
- 复杂业务逻辑下沉到 Helper，System 只负责调度

## 9. ECANode 新节点实现
- 新增 `Container` 与 `SpawnPoint` 组件/系统
- 由流程图动作驱动
- **不触碰**：`equipment/item`、`spell` 核心逻辑、匹配与战局系统、摇杆/输入系统

## 10. 测试计划
- 新增 `Ecanode_ContainerInteract_Test`
- 新增 `Ecanode_SpawnPoint_Test`
- 编译仅使用 `dotnet build ET.sln`

## 11. 待确认清单（后续沟通）
1. 最小节点集合是否需要增删？
2. 动作参数清单（容器表/刷怪组/计时/奖励等）待架子完成后细化。
3. 容器复开细节（容器1再次交互、容器2再次进入范围）待细化。

---
**最后更新**：2026-02-27
