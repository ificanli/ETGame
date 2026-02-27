# ECA 节点文档

> 说明：此文档用于统一记录 ECA 节点类型、触发方式、参数约定与状态含义，后续会持续维护。
> 节点具体执行逻辑由 **ECANode 包** 实现，通过 Invoke 机制接入。

## 1. 节点总览
- **Event**：事件入口（触发流程）
- **Condition**：条件判断（分支 True/False）
- **Action**：动作执行（驱动状态变化/业务逻辑）
- **State**：状态节点（当前仅作流程占位）

## 2. 客户端/服务端执行端约定
> 说明：表现可以客户端本地预测，但**状态与结果以服务端为准**。

### 2.1 服务端权威执行（逻辑）
- `SetPointState` / `SetPointActive`
- `StartSearchTimer` / `StartEvacCountdown`
- `SpawnItemsToGround` / `SpawnMonsters`
- `AllowEvacPlayers` / `TransferToLobby`
- `StartTask` / `GiveReward`
- `StartAreaHold` / `StartEscort`
- `BroadcastMap`

### 2.2 客户端表现执行（UI/动画）
- `ShowInteractButton` / `HideInteractButton`
- `ShowSearchUI` / `OpenContainerUI`
- `ShowEvacUI` / `ShowTaskAcceptUI`
- `PlayOpenAnim`
- `StartZipline`

### 2.3 双端配合
- `ApplyStealth` / `RemoveStealth`：服务端控制状态，客户端做表现

## 3. 事件节点（Event）
### 已实现/可选
- `OnPlayerEnterRange`：玩家进入范围
- `OnPlayerLeaveRange`：玩家离开范围 （先不做）
- `OnPlayerInteract`：玩家交互
- `OnTimerElapsed`：计时结束
- `OnCombatTimeReached`：战斗时间达到
- `OnTaskComplete`：任务完成
- `OnSwitchPulled`：拉闸触发
- `OnTargetKilled`：目标击杀完成
- `OnAreaHoldCompleted`：占领完成
- `OnEscortArrived`：护送到达

## 4. 条件节点（Condition）
### 已实现（最小可执行）
- `PlayerInRange`：玩家是否在范围内
- `PointStateEquals`：点位状态是否等于指定值（参数：`state`）
- `PointTypeEquals`：点位类型匹配（参数：`point_type`）

### 规划/待扩展
- `BackpackWeightLE`：背包重量 ≤ `max_weight`
- `KillCountGE`：击杀数 ≥ `min_kill`
- `TaskState`：任务状态匹配（参数：`task_id`, `state`）

## 5. 动作节点（Action）
### 已实现（最小可执行）
- `SetPointState`：设置点位状态（参数：`state`）
- `SetPointActive`：点位启用/禁用（参数：`active` 0/1）
- `StartSearchTimer`：启动搜索计时（`seconds`, `timer_id`，计时结束触发 `OnTimerElapsed`）
- `StartEvacCountdown`：撤离倒计时（`seconds`, `timer_id`，计时结束触发 `OnTimerElapsed`）
- `TransferToLobby`：传送回大厅（`map_name`）

### 已实现（骨架，待接入真实系统）
- `SpawnItemsToGround`：记录掉落请求（`loot_table`, `count`, `radius`）
- `SpawnMonsters`：记录刷怪请求（`group_id`, `count`）

### 规划/待扩展
- `ShowInteractButton` / `HideInteractButton`：显示/隐藏交互按钮（`button_id`）
- `ShowSearchUI`：显示搜索 UI（`ui_id`）
- `OpenContainerUI`：打开容器 UI（`ui_id`）
- `PlayOpenAnim`：播放打开动画（`anim_id`）
- `ShowEvacUI`：显示撤离 UI（`ui_id`）
- `AllowEvacPlayers`：允许撤离（无参）
- `BroadcastMap`：地图广播（`message_id`）
- `ShowTaskAcceptUI`：显示接任务 UI（`ui_id`）
- `StartTask`：开始任务（`task_id`）
- `GiveReward`：发放奖励（`reward_id`）
- `StartAreaHold`：开始占领（`area_id`, `seconds`）
- `StartEscort`：开始护送（`route_id`, `target_id`）
- `StartZipline`：滑索（`zipline_id`）
- `ApplyStealth`：进入隐身（`seconds`）
- `RemoveStealth`：取消隐身

## 6. 点位类型（PointType）
- `1`：EvacuationPoint（撤离点）
- `2`：SpawnPoint（刷怪点）
- `3`：Container（容器）

> 说明：PointType 用于语义/分组；**若点位绑定了 FlowGraph，则优先走流程图逻辑**。撤离点后续也将迁移为流程图实现。

## 7. 状态约定（PointState / ContainerState）
- `0`：Closed/初始
- `1`：Opened/已打开

## 8. 参数填写规范
- 统一使用 `key=value`，一行一个
- 示例：
  - `state=1`
  - `seconds=10`
  - `task_id=Task_001`

## 9. 业务模板（建议组合）
> 以下为“节点组合模板”，用于快速搭建流程图。参数与UI/资源由后续配置补齐。

### 9.1 搜索类容器1（手动点击，首次搜索）
**需求**：玩家靠近出现按钮，点击后搜索 x 秒，显示容器与背包 UI；首次打开播放动画并变为已打开；再次点击不再搜索。

**模板**：
- Event: `OnPlayerEnterRange` → Action: `ShowInteractButton`
- Event: `OnPlayerInteract` → Condition: `PointStateEquals (state=0)`
  - True → Action: `StartSearchTimer (seconds=?, timer_id=?)` → Action: `OpenContainerUI (ui_id=?)` → Action: `SetPointState (state=1)`
  - False → Action: `OpenContainerUI (ui_id=?)`

### 9.2 搜索类容器2（自动计时，爆出物品）
**需求**：玩家进入范围自动计时，时间到后爆出物品；已打开不再触发。

**模板**：
- Event: `OnPlayerEnterRange` → Condition: `PointStateEquals (state=0)`
  - True → Action: `StartSearchTimer (seconds=?, timer_id=?)` → Action: `SpawnItemsToGround (loot_table=?, count=?, radius=?)` → Action: `SetPointState (state=1)`
  - False →（无）

### 9.3 撤离点（倒计时撤离）
**需求**：战斗时间达到/任务完成/条件满足后生效，玩家在范围内倒计时撤离。

**模板**：
- Event: `OnCombatTimeReached (seconds=?)` 或 `OnTaskComplete (task_id=?)`
  → Action: `StartEvacCountdown (seconds=?, reason=?, timer_id=?)` → Action: `ShowEvacUI (ui_id=?)` → Action: `TransferToLobby (map_name=?)`

### 9.4 条件撤离（背包重量/击杀数）
**模板**：
- Event: `OnPlayerEnterRange` → Condition: `BackpackWeightLE (max_weight=?)` 或 `KillCountGE (min_kill=?)`
  - True → Action: `StartEvacCountdown (seconds=?, reason=?, timer_id=?)` → Action: `ShowEvacUI (ui_id=?)` → Action: `TransferToLobby (map_name=?)`

### 9.5 拉闸撤离（全图倒计时）
**模板**：
- Event: `OnSwitchPulled (switch_id=?)` → Action: `StartEvacCountdown (seconds=?, reason=?, timer_id=?)` → Action: `BroadcastMap (message_id=?)`
- Event: `OnTimerElapsed (timer_id=与上面一致)` → Action: `AllowEvacPlayers`

### 9.6 地图任务点（接任务）
**模板**：
- Event: `OnPlayerEnterRange` → Action: `ShowTaskAcceptUI (ui_id=?)`
- Event: `OnPlayerInteract` → Action: `StartTask (task_id=?)`

### 9.7 任务类型示例
**刷怪任务**：
- Action: `SpawnMonsters (group_id=?, count=?)`
- Event: `OnTargetKilled (group_id=?)` → Action: `GiveReward (reward_id=?)`

**占领任务**：
- Action: `StartAreaHold (area_id=?, seconds=?)`
- Event: `OnAreaHoldCompleted (area_id=?)` → Action: `GiveReward (reward_id=?)`

**护送任务**：
- Action: `StartEscort (route_id=?, target_id=?)`
- Event: `OnEscortArrived (escort_id=?)` → Action: `GiveReward (reward_id=?)`

**击杀Boss任务**：
- Event: `OnTargetKilled (target_id=?)` → Action: `GiveReward (reward_id=?)`

### 9.8 其他交互物
**滑索**：
- Event: `OnPlayerInteract` → Action: `StartZipline (zipline_id=?)`

**草丛**：
- Event: `OnPlayerEnterRange` → Action: `ApplyStealth (seconds=?)`
- Event: `OnPlayerLeaveRange` → Action: `RemoveStealth`

---
**最后更新**：2026-02-27
