# ECA 场景配置参数手册

> 适用对象：策划、场景配置、关卡联调
>
> 目标：明确说明 ECA 点位参数、FlowGraph 节点参数、默认值，以及每个参数实际在哪些点位/逻辑上生效。
>
> 本文档基于当前代码实现整理，不写规划态能力，只写当前已经接入的配置规则。

## 1. 配置入口

### 1.1 场景点位入口
- 在 Unity 场景中放置 `ECAPointMarker`
- 每个点位至少配置：
  - `ConfigId`
  - `Type`
  - `Params`
  - 可选 `FlowGraph`

### 1.2 导出结果
- 导出菜单：`ET/ECA/Export ECA Config`
- 导出路径：`Packages/cn.etetet.map/Bundles/ECA/{SceneName}.txt`
- 服务端加载入口：`ECALoader.LoadFromFile(scene, mapName)`

### 1.3 运行时生效规则
- 如果点位配置了 `FlowGraph`，优先走流程图逻辑。
- 如果点位没有 `FlowGraph`，则走点位类型的旧逻辑 fallback。
- 当前只有“撤离点无 FlowGraph 时”的 fallback 仍然在用，并且已经支持读取新的点位参数。

## 2. 点位类型

| Type | 名称 | 说明 |
|---|---|---|
| `1` | `EvacuationPoint` | 撤离点 |
| `2` | `SpawnPoint` | 出生点 |
| `3` | `Container` | 容器 |
| `4` | `MonsterSpawnPoint` | 刷怪点 |
| `5` | `RangeTrigger` | 通用范围触发点 |
| `6` | `Door` | 普通门 |
| `7` | `KeyDoor` | 钥匙门 |

## 3. 点位 Params 参数

这一节是直接挂在 `ECAPointMarker.Params` 上的参数。

### 3.1 通用参数

| Key | 类型 | 默认值 | 生效范围 | 说明 |
|---|---|---:|---|---|
| `interact_range` | `float` | `3` | 全部点位 | 点位交互/检测半径 |
| `check_range_interval_ms` | `int` | `200` | 场景级，全局生效 | 范围检测轮询间隔，单位毫秒 |

#### `check_range_interval_ms` 特别说明
- 这是“场景级参数”，不是单点位独立频率。
- 当前实现会在导出后加载整张地图配置时，取“第一个大于 0 的 `check_range_interval_ms`”作为整张地图的范围检测间隔。
- 建议：
  - 同一张地图只在一个点位上填写这个值，作为全图统一配置。
  - 其它点位保持 `0` 或不填，避免多人误以为它是单点位参数。

### 3.2 撤离点参数

| Key | 类型 | 默认值 | 生效范围 | 说明 |
|---|---|---:|---|---|
| `evacuation_duration_ms` | `int` | `10000` | `EvacuationPoint` 且无 `FlowGraph` fallback 逻辑 | 撤离时长，单位毫秒 |
| `lobby_map_name` | `string` | `Map1` | `EvacuationPoint` 且无 `FlowGraph` fallback 逻辑 | 撤离完成后传送的大厅地图名 |

#### 撤离点参数特别说明
- 这两个参数当前是给“无 FlowGraph 的撤离点旧逻辑”读取的。
- 如果撤离流程已经完全配置在 `FlowGraph` 里：
  - 倒计时长短通常由 `StartEvacCountdown.seconds` 控制。
  - 传送目标通常由 `TransferToLobby.map_name` 控制。
- 也就是说：
  - `evacuation_duration_ms` / `lobby_map_name` 不是通用的 FlowGraph 参数。
  - 它们只影响 fallback 撤离逻辑。

### 3.3 出生点参数

| Key | 类型 | 默认值 | 生效范围 | 说明 |
|---|---|---:|---|---|
| `team_id` | `int` | `0` | `SpawnPoint` | 队伍编号，当前主要用于出生点语义配置 |

### 3.4 普通门参数

| Key | 类型 | 默认值 | 生效范围 | 说明 |
|---|---|---:|---|---|
| `closed_button_text_id` | `int` | `0` | `Door` | 门关闭时的交互文本 ID |
| `opened_button_text_id` | `int` | `0` | `Door` | 门打开时的交互文本 ID |

### 3.5 钥匙门参数

| Key | 类型 | 默认值 | 生效范围 | 说明 |
|---|---|---:|---|---|
| `required_key_item_id` | `int` | `0` | `KeyDoor` | 所需钥匙物品配置 ID |
| `locked_button_text_id` | `int` | `0` | `KeyDoor` | 未持有钥匙时显示的交互文本 ID |
| `closed_button_text_id` | `int` | `0` | `KeyDoor` | 可打开/关闭状态的交互文本 ID |
| `opened_button_text_id` | `int` | `0` | `KeyDoor` | 已打开状态的交互文本 ID |
| `consume_key_count` | `int` | `1` | `KeyDoor` | 开门消耗钥匙数量 |

#### 钥匙门行为说明
- `KeyDoor` 初始状态为 `Locked`
- 玩家有钥匙时：
  - `RefreshDoorInteractHint`
  - `ToggleDoor`
  会允许交互
- 玩家没有钥匙时：
  - 仍然可以显示按钮
  - 但按钮会被标记为不可交互，并显示 `locked_button_text_id`

## 4. FlowGraph 节点参数

这一节是流程图节点自己的 `Params`，不是点位 `Params`。

## 4.1 已接入的 Condition 参数

### `PointStateEquals`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `state` | `int` | 是 | 判断点位当前状态是否等于该值 |

### `PointTypeEquals`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `point_type` | `int` | 是 | 判断点位类型是否匹配 |

### `PlayerInRange`
- 无参数

## 4.2 已接入的 Action 参数

### `SetPointActive`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `active` | `bool/int` | 是 | `true/false` 或 `1/0` |

### `SetPointState`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `state` | `int` | 是 | 设置点位状态 |

### `ShowInteractButton`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `button_text_id` | `int` | 否 | 推荐使用，交互按钮文本 ID |
| `button_id` | `int` | 否 | 兼容旧字段，`button_text_id` 没填时回退读取它 |
| `can_interact` | `bool/int` | 否 | 默认 `true` |

### `HideInteractButton`
- 无参数

### `StartSearchTimer`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `seconds` | `float` | 是 | 搜索时长，单位秒 |
| `timer_id` | `string` | 是 | 定时器标识，后续 `OnTimerElapsed` 依赖它匹配 |

### `ShowSearchUI`
- 无参数

### `OpenContainerUI`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `ui_key` | `string` | 否 | 容器 UI 标识 |

### `GenerateContainerLoot`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `loot_table` | `string` | 是 | 掉落表 ID |
| `count` | `int` | 是 | 掉落次数/数量 |
| `radius` | `float` | 是 | 生成半径 |
| `output_mode` | `string` | 否 | 输出模式 |

### `SpawnItemsToGround`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `loot_table` | `string` | 是 | 掉落表 ID |
| `count` | `int` | 是 | 掉落次数/数量 |
| `radius` | `float` | 是 | 掉落半径 |

说明：
- 这是兼容旧动作。
- 当前内部会默认按地面掉落模式处理。

### `SpawnMonsters`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `group_id` | `string` | 是 | 刷怪组 ID |
| `count` | `int` | 是 | 刷怪数量 |

### `StartEvacCountdown`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `seconds` | `float` | 是 | 倒计时秒数 |
| `timer_id` | `string` | 是 | 定时器 ID |

### `AllowEvacPlayers`
- 无参数

### `TransferToLobby`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `map_name` | `string` | 是 | 传送目标地图名 |

### `RefreshDoorInteractHint`
- 无参数

### `ToggleDoor`
- 无参数

### `ApplyStealth`
- 无参数

### `RemoveStealth`
- 无参数

## 5. 当前已接入、但不要误配的能力边界

以下 key 已经存在于枚举或编辑器候选里，但当前服务端逻辑还没有真正接上，不建议策划当成功能可用：
- `PlayOpenAnim`
- `ShowEvacUI`
- `BroadcastMap`
- `ShowTaskAcceptUI`
- `StartTask`
- `GiveReward`
- `StartAreaHold`
- `StartEscort`
- `StartZipline`
- `BackpackWeightLE`
- `KillCountGE`
- `TaskState`

结论：
- 这些节点 key 可以先保留在设计稿里。
- 但在正式配置中不要假设它们已经有完整运行时效果。

## 6. 默认值汇总

| 项目 | 默认值 |
|---|---:|
| `interact_range` | `3` |
| `check_range_interval_ms` | `200` |
| `evacuation_duration_ms` | `10000` |
| `lobby_map_name` | `Map1` |
| `consume_key_count` | `1` |
| `ShowInteractButton.can_interact` | `true` |

## 7. 推荐配置示例

### 7.1 钥匙门

点位 `Params`：

```text
interact_range=3
required_key_item_id=10001
locked_button_text_id=1001
closed_button_text_id=1002
opened_button_text_id=1003
consume_key_count=1
```

FlowGraph 推荐：
- `OnPlayerEnterRange -> RefreshDoorInteractHint`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> ToggleDoor`

### 7.2 纯 fallback 撤离点

点位 `Params`：

```text
interact_range=5
evacuation_duration_ms=15000
lobby_map_name=LobbyMap
```

说明：
- 不配置 `FlowGraph`
- 玩家进入范围后会直接走旧撤离逻辑

### 7.3 FlowGraph 撤离点

点位 `Params`：

```text
interact_range=5
```

FlowGraph Action 参数：

```text
StartEvacCountdown:
seconds=15
timer_id=evac_main

TransferToLobby:
map_name=LobbyMap
```

说明：
- 这种写法下，真正生效的是 Action 参数
- `evacuation_duration_ms` / `lobby_map_name` 不参与这条流程

### 7.4 地图全局范围检测频率

任选一个点位填写：

```text
check_range_interval_ms=100
```

说明：
- 当前整张地图统一使用这个值
- 不要在多个点位上填不同值

## 8. 配置检查清单

- `ConfigId` 必须唯一
- `Type` 必须和实际业务一致
- `interact_range` 未填时默认是 `3`
- `KeyDoor` 必须补齐钥匙相关参数，否则会表现成不可用门
- `TransferToLobby` 必须填写 `map_name`
- `StartSearchTimer` / `StartEvacCountdown` 必须填写 `timer_id`
- 同一条流程里如果依赖 `OnTimerElapsed`，要确保 `timer_id` 前后一致
- `check_range_interval_ms` 一张图只配置一次

## 9. 相关代码入口

- 点位参数模板：`Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs`
- 点位参数定义：`Packages/cn.etetet.eca/Scripts/Model/Share/ECAPointParamKey.cs`
- 门参数定义：`Packages/cn.etetet.eca/Scripts/Model/Share/ECADoorParamKey.cs`
- Flow 参数读取：`Packages/cn.etetet.eca/Scripts/Model/Share/FlowParamHelper.cs`
- Flow 执行器：`Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAFlowGraphRunner.cs`
- Action 实现：`Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ECAFlowActionInvokeHandler.cs`

---

最后更新：2026-03-13
