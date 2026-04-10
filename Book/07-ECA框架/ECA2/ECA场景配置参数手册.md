# ECA 场景配置参数手册

> 适用对象：策划、场景配置、关卡联调
>
> 目标：明确说明 ECA 点位参数、FlowGraph 节点参数、默认值，以及每个参数实际在哪些点位/逻辑上生效。
>
> 本文档只写当前代码里已经确认接上的能力。
>
> 对“编辑器里能选到，但正式地图运行时没有统一触发链”的项，会明确标注不要依赖。

## 1. 配置入口

### 1.1 场景点位入口
- 在 Unity 场景中放置 `ECAPointMarker`
- 每个点位至少配置：
  - `ConfigId`
  - `Type`
  - `Params`
  - 可选 `FlowGraph`

补充说明：
- 旧版 `ECAConfigAsset` / `Config` 字段不再作为当前入口使用
- `ECAPointMarker` 会按 `Type` 自动补齐模板参数

### 1.2 导出结果
- 导出菜单：`ET/ECA/Export ECA Config`
- 导出路径：`Packages/cn.etetet.map/Bundles/ECA/{SceneName}.txt`
- 服务端加载入口：`ECALoader.LoadFromFile(scene, mapName)`

### 1.3 运行时生效规则
- 如果点位配置了 `FlowGraph`，优先走流程图逻辑。
- 如果点位没有 `FlowGraph`，则走点位类型 fallback。
- 当前真正仍在使用的 fallback，主要是“撤离点旧逻辑”。
- 撤离点还有一个兼容分支：
  - 如果点位有 `FlowGraph`
  - 但图里没有 `StartEvacCountdown`
  - 进入范围后仍然会 fallback 到旧撤离逻辑。

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
| `nav_block_enabled` | `bool/int` | `0` | 需要导航阻挡的点位 | 是否启用导航阻挡 |
| `nav_block_half_extents_x` | `float` | `1.25` | `nav_block_enabled=1` 的点位 | 阻挡体半尺寸 X |
| `nav_block_half_extents_y` | `float` | `2.5` | `nav_block_enabled=1` 的点位 | 阻挡体半尺寸 Y |
| `nav_block_half_extents_z` | `float` | `1.25` | `nav_block_enabled=1` 的点位 | 阻挡体半尺寸 Z |
| `nav_block_states` | `string` | 空 | `nav_block_enabled=1` 的点位 | 哪些状态下阻挡导航，支持逗号分隔状态值 |

#### `check_range_interval_ms` 特别说明
- 这是“场景级参数”，不是单点位独立频率。
- 当前实现会在导出后加载整张地图配置时，取“第一个大于 0 的 `check_range_interval_ms`”作为整张地图的范围检测间隔。
- 建议：
  - 同一张地图只在一个点位上填写这个值，作为全图统一配置。
  - 其它点位保持 `0` 或不填，避免多人误以为它是单点位参数。

#### `nav_block_*` 特别说明
- 门和钥匙门的模板默认会自动打开 `nav_block_enabled`
- 如果 `nav_block_states` 留空：
  - `Door` 默认在 `Closed` 状态阻挡
  - `KeyDoor` 默认在 `Locked` 和 `Closed` 状态阻挡
- 如果显式填写了 `nav_block_states`，运行时以你填写的状态集为准

### 3.2 撤离点参数

| Key | 类型 | 默认值 | 生效范围 | 说明 |
|---|---|---:|---|---|
| `evacuation_duration_ms` | `int` | `10000` | `EvacuationPoint` | 撤离时长，单位毫秒 |
| `lobby_map_name` | `string` | `Home` | `EvacuationPoint` | 撤离完成后传送的大厅地图名 |

#### 撤离点参数特别说明
- `evacuation_duration_ms`：
  - 无 `FlowGraph` fallback 撤离时生效
  - `StartEvacCountdown` 没填 `seconds` 时，也会回退读取它
- `lobby_map_name`：
  - fallback 撤离时生效
  - `StartEvacCountdown` 启动的撤离组件最终完成传送时，也会读取它
- 不要把 `TransferToLobby.map_name` 理解成撤离倒计时完成后的标准出口。
  - `TransferToLobby` 是立即执行动作
  - `StartEvacCountdown -> TransferToLobby` 串起来会立刻跳图，不会等倒计时结束

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

## 4.1 当前正式运行时会触发的 Event

| Event | 正式地图运行时 | 触发来源 |
|---|---|---|
| `OnPlayerEnterRange` | 是 | 玩家进入范围 |
| `OnPlayerLeaveRange` | 是 | 玩家离开范围 |
| `OnPlayerInteract` | 是 | 玩家交互消息 |
| `OnTimerElapsed` | 是 | `StartSearchTimer` 等定时器完成 |
| `OnMapLoaded` | 否 | 当前未找到地图加载后统一分发到每个点位的正式接线 |
| `OnCombatTimeReached` | 否 | 未接线 |
| `OnTaskComplete` | 否 | 未接线 |
| `OnSwitchPulled` | 否 | 未接线 |
| `OnTargetKilled` | 否 | 未接线 |
| `OnAreaHoldCompleted` | 否 | 未接线 |
| `OnEscortArrived` | 否 | 未接线 |

结论：
- 关卡正式配置目前只应依赖前四个 Event
- `OnMapLoaded` 虽然在编辑器和导出数据里存在，但当前不要当作正式可用能力

## 4.2 已接入的 Condition 参数

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

以下 Condition key 虽然仍在枚举里，但当前没有正式运行时实现：
- `BackpackWeightLE`
- `KillCountGE`
- `TaskState`

## 4.3 已接入的 Action 参数

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
| `count` | `int/string` | 是 | 掉落次数/数量，支持固定值或范围（如 `2`、`1-3`、`1~3`） |
| `radius` | `float` | 是 | 生成半径 |
| `output_mode` | `string` | 否 | 输出模式 |
| `allow_repeat` | `bool/int` | 否 | 是否允许重复抽取，默认 `true` |

`output_mode` 当前可用值：
- `ContainerPanel` / `1`
- `GroundDrop` / `2`

### `SpawnItemsToGround`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `loot_table` | `string` | 是 | 掉落表 ID |
| `count` | `int/string` | 是 | 掉落次数/数量，支持固定值或范围 |
| `radius` | `float` | 是 | 掉落半径 |
| `allow_repeat` | `bool/int` | 否 | 是否允许重复抽取，默认 `true` |

说明：
- 这是兼容旧动作。
- 当前内部会默认按地面掉落模式处理。

### `SpawnMonsters`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `group_id` | `string` | 是 | 刷怪组 ID |
| `count` | `int` | 是 | 刷怪数量 |
| `chance_permille` | `int` | 否 | 千分比概率，默认 `1000` |

### `StartEvacCountdown`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `seconds` | `float` | 否 | 倒计时秒数；未填时回退到点位 `evacuation_duration_ms` 或默认值 |

特别说明：
- 当前实现不读取 `timer_id`
- 这个动作只负责启动 `PlayerEvacuationComponent`
- 撤离完成后的真正传送目标取自点位 `lobby_map_name`

### `AllowEvacPlayers`
- 无参数

### `TransferToLobby`

| Key | 类型 | 必填 | 说明 |
|---|---|---|---|
| `map_name` | `string` | 是 | 立即传送目标地图名 |

特别说明：
- 这是“立即执行”的传送动作
- 不要把它串在 `StartEvacCountdown` 后面模拟倒计时传送

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
| `nav_block_enabled` | `0` |
| `nav_block_half_extents_x` | `1.25` |
| `nav_block_half_extents_y` | `2.5` |
| `nav_block_half_extents_z` | `1.25` |
| `evacuation_duration_ms` | `10000` |
| `lobby_map_name` | `Home` |
| `consume_key_count` | `1` |
| `ShowInteractButton.can_interact` | `true` |
| `GenerateContainerLoot.allow_repeat` | `true` |
| `SpawnItemsToGround.allow_repeat` | `true` |
| `SpawnMonsters.chance_permille` | `1000` |

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
lobby_map_name=Home
```

说明：
- 不配置 `FlowGraph`
- 玩家进入范围后会直接走旧撤离逻辑

### 7.3 FlowGraph 撤离点

点位 `Params`：

```text
interact_range=5
lobby_map_name=Home
```

FlowGraph Action 参数：

```text
StartEvacCountdown:
seconds=15
```

说明：
- 这种写法下，倒计时主要由 `StartEvacCountdown.seconds` 控制
- 撤离完成后的回城目标仍然取点位 `lobby_map_name`
- 不要把 `TransferToLobby` 串在后面

### 7.4 地图全局范围检测频率

任选一个点位填写：

```text
check_range_interval_ms=100
```

说明：
- 当前整张地图统一使用这个值
- 不要在多个点位上填不同值

### 7.5 容器掉落到地面

```text
GenerateContainerLoot:
output_mode=GroundDrop
loot_table=10001*1|10002*2
count=1-2
radius=2
allow_repeat=0
```

## 8. 配置检查清单

- `ConfigId` 必须唯一
- `Type` 必须和实际业务一致
- `interact_range` 未填时默认是 `3`
- `KeyDoor` 必须补齐钥匙相关参数，否则会表现成不可用门
- `TransferToLobby` 必须填写 `map_name`
- `StartSearchTimer` 必须填写 `timer_id`
- `StartEvacCountdown` 当前不需要 `timer_id`
- 同一条流程里如果依赖 `OnTimerElapsed`，要确保 `timer_id` 前后一致
- `check_range_interval_ms` 一张图只配置一次
- 正式地图不要依赖 `OnMapLoaded`
- 撤离点不要配置 `StartEvacCountdown -> TransferToLobby`

## 9. 相关代码入口

- 点位参数模板：`Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs`
- 点位参数定义：`Packages/cn.etetet.eca/Scripts/Model/Share/ECAPointParamKey.cs`
- 门参数定义：`Packages/cn.etetet.eca/Scripts/Model/Share/ECADoorParamKey.cs`
- Flow 参数读取：`Packages/cn.etetet.eca/Scripts/Model/Share/FlowParamHelper.cs`
- Flow 执行器：`Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAFlowGraphRunner.cs`
- Action 实现：`Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ECAFlowActionInvokeHandler.cs`
- 容器运行时：`Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ContainerRuntimeHelper.cs`
- 导航阻挡：`Packages/cn.etetet.map/Scripts/Hotfix/Server/ECAPointNavBlockHelper.cs`

---

最后更新：2026-03-27
