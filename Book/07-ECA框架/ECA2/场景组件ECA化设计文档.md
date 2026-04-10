# 场景组件 ECA 化设计文档

**创建时间**：2026-03-28
**最后更新**：2026-03-28
**状态**：设计中
**关联任务**：M0.2-W1 #16
**涉及包**：cn.etetet.eca, cn.etetet.ecanode, cn.etetet.map

## 需求概述

基于 [场景概念设计.md](../../概念-需求-设计文档/场景概念设计.md) 中已经确认的场景组件，整理一套面向当前工程实际的 ECA 落地方案，明确以下内容：

1. 哪些组件应该进入 ECA，哪些只是纯场景静态资产，不应强行做成点位。
2. 哪些组件可以直接复用现有点位类型与节点能力，哪些必须补运行时代码。
3. 当前阶段场景配置应落在哪里，参数如何组织，哪些能力暂时不要依赖。
4. 后续正式实现时的分批顺序、代码改动范围和验收边界。

当前阶段只形成设计文档，不开始代码实现。

## 技术方案

### 整体思路

本轮场景组件 ECA 化遵循 4 条原则：

1. **只把“有交互、有状态、有流程”的组件纳入 ECA**。
   纯掩体、纯装饰、纯模型展示物不强行挂 `ECAPointMarker`，避免把场景结构件错误建模成逻辑点位。
2. **优先复用现有点位类型**。
   只有当组件的默认参数、状态机、提示文案和运行时语义明显不同于现有类型时，才考虑新增点位类型。
3. **当前配置入口以场景 `ECAPointMarker + FlowGraph` 为主**。
   现阶段不新增 Excel/Luban 配置表，不把功能设计建立在尚不存在的导出链路上。
4. **复杂交互优先走 FlowGraph，fallback 只保留给已成熟的基础能力**。
   当前已稳定可依赖的基础能力主要是门、钥匙门、容器、撤离等；`OnMapLoaded` 等未正式接线能力不纳入本期设计前提。

### 当前基线核对

结合现有代码和已清理过的 ECA 文档，当前可作为设计基线的能力如下：

- 现有点位类型：
  - `EvacuationPoint`
  - `SpawnPoint`
  - `Container`
  - `MonsterSpawnPoint`
  - `RangeTrigger`
  - `Door`
  - `KeyDoor`
- 当前正式可依赖的事件：
  - `OnPlayerEnterRange`
  - `OnPlayerLeaveRange`
  - `OnPlayerInteract`
  - `OnTimerElapsed`
- 当前正式可依赖的动作：
  - `ShowInteractButton`
  - `HideInteractButton`
  - `StartSearchTimer`
  - `ShowSearchUI`
  - `OpenContainerUI`
  - `GenerateContainerLoot`
  - `SpawnItemsToGround`
  - `SpawnMonsters`
  - `StartEvacCountdown`
  - `TransferToLobby`
  - `RefreshDoorInteractHint`
  - `ToggleDoor`
  - `ApplyStealth`
  - `RemoveStealth`
- 当前不要依赖的能力边界：
  - `OnMapLoaded` 虽然在枚举、编辑器模板、导出数据中存在，但当前未确认有正式运行时统一分发。
  - `ShowTaskAcceptUI`、`StartTask`、`GiveReward`、`TaskState` 等任务相关 key 目前属于“已预留、未正式接线”。
  - 现阶段没有场景组件对应的 Excel/Luban 表，场景配置不能假设存在一套外部配置源。

### 组件分类与 ECA 建模结论

| 场景组件 | 是否纳入本期 ECA | 推荐建模方式 | 结论说明 |
|---|---|---|---|
| 星愿方块 | 是 | 复用 `RangeTrigger` + 补消失流程节点运行时 | 玩家靠近后点击交互键，方块逐渐消失；具备明确的交互入口、状态切换和范围检测，符合 ECA 纳入原则 |
| 常规门 `Push-Gate` | 是 | 复用 `Door` | 现有普通门语义和该组件一致，可直接使用门交互和导航阻挡能力 |
| 钥匙门 `Vault-Gate` | 是 | 复用 `KeyDoor` | 现有钥匙门语义和该组件一致，可直接使用钥匙校验、提示刷新和开关门能力 |
| 任务点 `Mission Terminal` | 是 | 复用 `RangeTrigger` + 补任务相关节点运行时 | 当前不急着新增点位类型，先基于可交互触发点补齐任务终端需要的条件与动作 |
| 充气防爆墙 `Air Barrier` | 条件纳入 | 第一阶段按 `Door` 语义设计，若后续有独立状态机再升级 | 如果只是“可阻挡/可放行”的通路控制，按门处理即可；若后续要做充放气、爆破、修复等独立状态流，再单独拆类型 |
| 低级容器 `Common` | 是 | 复用 `Container` 参数模板 | 本质是不同掉落档位的容器，不建议拆出新点位类型 |
| 中级容器 `Rare` | 是 | 复用 `Container` 参数模板 | 同上 |
| 高级容器 `Epic` | 是 | 复用 `Container` 参数模板 | 同上 |
| 道具容器 `Utility Canister` | 是 | 复用 `Container` 参数模板 | 与普通容器的区别主要在掉落池、UI 文案和搜索时长，不需要新点位类型 |
| 蓝白扭蛋机 `Basic` | 是 | 第一阶段复用 `Container`，第二阶段补抽取/消耗节点 | 现有容器能力已覆盖“交互 -> 出奖励”的主流程，但尚未覆盖消耗、保底和专属 UI |
| 金色扭蛋机 `Elite` | 是 | 第一阶段复用 `Container`，第二阶段补抽取/消耗节点 | 与蓝白扭蛋机同类，先做参数模板差异，不急着拆新类型 |

### 为什么不在第一阶段大量新增点位类型

本设计不建议一开始把每个美术组件都拆成单独 `ECAPointType`，原因如下：

1. 当前 `ECAPointType` 主要承载的是运行时语义和模板补全，不是美术分类系统。
2. 低中高容器、道具容器、蓝白/金色扭蛋机，本质上都属于“交互后产出内容”的同类点位，差异更适合落在参数模板和节点参数，而不是类型枚举。
3. `RangeTrigger` 本身就是给“有交互流程但暂无专用 fallback”的点位做承载，任务点优先用它承接更稳妥。
4. 真正需要新增类型的前提，应当是该组件存在独立状态机、默认参数模板、编辑器校验规则，且复用现有类型会明显造成配置歧义。

## 涉及的包和文件

本设计文档对应的后续实现，预计会涉及以下文件：

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.eca/Scripts/Model/Share/ECAPointType.cs` | 评估是否修改 | 仅当 `AirBarrier` 后续确认需要独立点位类型时才新增 |
| `Packages/cn.etetet.eca/Scripts/Model/Share/ECAFlowActionKey.cs` | 修改 | 补星愿方块 `StartDissolve`、任务点/扭蛋机需要的动作 key，或正式启用已预留 key |
| `Packages/cn.etetet.eca/Scripts/Model/Share/ECAFlowConditionKey.cs` | 修改 | 补任务状态、消耗条件等判断 key，或正式启用已预留 key |
| `Packages/cn.etetet.eca/Scripts/Model/Share/ECAFlowEventType.cs` | 评估是否修改 | 当前不以新增事件为优先，优先复用现有 Enter/Leave/Interact/Timer |
| `Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ECAFlowActionInvokeHandler.cs` | 修改 | 落星愿方块消失、任务点、扭蛋机、可选防爆墙动作的运行时逻辑 |
| `Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAPointComponentSystem.cs` | 修改 | 补点位交互流程、状态流转和必要 fallback |
| `Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs` | 修改 | 统一新的参数模板和编辑器默认值 |
| `Book/07-ECA框架/ECA2/ECA框架使用指南-场景配置.md` | 修改 | 正式实现后同步配置流程 |
| `Book/07-ECA框架/ECA2/ECA场景配置参数手册.md` | 修改 | 正式实现后同步参数手册 |

当前这一轮只新增设计文档，不改上述代码文件。

## Entity/Component 设计

本期设计优先复用现有 `ECAPointComponent` 数据模型，不新增新的独立 Entity。

第一阶段的核心目标是：

1. 用现有点位类型承接组件语义。
2. 补齐 Flow 动作/条件运行时。
3. 通过 `ECAPointMarker` 参数模板减少场景侧误配。

只有在以下条件同时成立时，才考虑新增组件或点位类型：

- 复用现有 `Door` / `Container` / `RangeTrigger` 会造成明显语义歧义；
- 该组件存在稳定、长期复用的独立状态机；
- 该组件需要专门的默认参数模板和编辑器校验逻辑；
- 该组件的 fallback 流程无法仅靠 FlowGraph 承担。

## 接口设计

当前设计阶段不计划新增网络消息，优先复用现有交互消息链路：

- 客户端交互入口仍然使用 `C2M_ECAInteract`
- 服务端继续由点位执行对应 FlowGraph 或 fallback

如后续 Mission Terminal 或 Gashapon Machine 需要专属 UI 回包，再在实现阶段评估是否新增消息，而不是在本轮设计里预先扩接口。

## 数据结构

### 当前阶段的配置落点

当前阶段的场景组件配置统一落在以下两个位置：

1. `ECAPointMarker.Params`
2. `FlowGraph` 中各节点的 `Params`

不新增 Excel/Luban 表，理由如下：

- 当前项目里还没有与这些场景组件匹配的现成表结构；
- 先补运行时能力和场景配置模板，能更快验证真实交互是否合理；
- 等参数趋于稳定后，再考虑把高复用的模板抽到 Excel 做统一配置。

### 参数组织原则

1. **点位 Params** 放“点位稳定属性”：
   - 交互半径
   - 文案 ID
   - 钥匙/消耗物品 ID
   - 是否阻挡导航
   - 组件档位或功能 profile
2. **节点 Params** 放“流程节点局部参数”：
   - 定时时长
   - 掉落表
   - 奖励数量
   - UI key
   - 条件分支参数
3. 同一类组件优先共用同一套 key，避免因为美术名不同而分裂成多套配置字段。

### 推荐参数模板

#### 1. `Push-Gate` / `Air Barrier`（按 `Door` 语义）

点位参数建议：

- `interact_range`
- `closed_button_text_id`
- `opened_button_text_id`
- `nav_block_enabled`
- `nav_block_states`
- `component_profile`

说明：

- `component_profile` 用于区分 `push_gate`、`air_barrier` 等美术/表现差异。
- 如果 `Air Barrier` 后续不允许玩家主动交互，而是由任务或机关切状态，则保留 `Door` 状态机，但交互入口改由其他点位或系统驱动。

#### 2. `Vault-Gate`（按 `KeyDoor` 语义）

点位参数建议：

- `interact_range`
- `required_key_item_id`
- `consume_key_count`
- `locked_button_text_id`
- `closed_button_text_id`
- `opened_button_text_id`
- `nav_block_enabled`
- `nav_block_states`
- `component_profile`

#### 3. 容器族（`Common` / `Rare` / `Epic` / `Utility Canister`）

点位参数建议：

- `interact_range`
- `button_text_id`
- `container_profile`
- `search_seconds`
- `loot_table`
- `loot_count`
- `drop_radius`
- `output_mode`

FlowGraph 建议流程：

- `OnPlayerEnterRange -> ShowInteractButton`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> ShowSearchUI -> StartSearchTimer`
- `OnTimerElapsed -> GenerateContainerLoot`
- 分支 1：`OpenContainerUI`
- 分支 2：`SpawnItemsToGround`

说明：

- 低中高容器和道具容器只做 profile 区分，不拆新点位类型。
- `output_mode` 可以决定是开容器面板还是直接掉地。

#### 3. 星愿方块

点位类型建议：

- `RangeTrigger`

点位参数建议：

- `interact_range`
- `button_text_id`
- `dissolve_duration_ms`
- `component_profile`

后续需要补齐的动作：

- `StartDissolve`（启动逐渐消失流程，含渐隐/缩小动画，服务端标记已消失并移除碰撞/掩体）

建议流程：

- `OnPlayerEnterRange -> ShowInteractButton`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> StartDissolve`

说明：

- 星愿方块同时作为掩体使用，消失后需要同步移除导航阻挡和碰撞体。
- `dissolve_duration_ms` 控制消失动画时长，动画期间不可重复交互。
- 消失后状态不可逆（单局内不会重置）。

#### 4. `Mission Terminal`

点位类型建议：

- 第一阶段：`RangeTrigger`

点位参数建议：

- `interact_range`
- `mission_id`
- `mission_terminal_profile`
- `accept_button_text_id`
- `progress_button_text_id`
- `complete_button_text_id`
- `reward_id`

后续需要补齐的条件/动作：

- `TaskState`
- `ShowTaskAcceptUI`
- `StartTask`
- `GiveReward`

建议流程：

- `OnPlayerEnterRange -> 根据 TaskState 刷新按钮文案`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> 根据 TaskState 分支`
- 可接：
  - `ShowTaskAcceptUI`
  - `StartTask`
  - `GiveReward`
  - `SetPointState`

#### 5. 扭蛋机族（`Basic` / `Elite`）

点位类型建议：

- 第一阶段：`Container`

点位参数建议：

- `interact_range`
- `button_text_id`
- `gashapon_profile`
- `cost_item_id`
- `cost_item_count`
- `loot_table`
- `loot_count`
- `drop_radius`

第一阶段建议流程：

- `OnPlayerEnterRange -> ShowInteractButton`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> GenerateContainerLoot -> SpawnItemsToGround`

第二阶段计划补齐：

- 交互前消耗校验
- 扭蛋专属 UI / 动画
- 精英扭蛋机的差异化规则（例如更高品质池、特殊保底、特效表现）

### 当前明确不纳入 ECA 的组件

当前所有场景概念设计中的组件均已纳入 ECA 或复用现有点位类型，暂无需要排除的组件。

## 实现步骤

后续进入实现阶段时，建议按以下顺序推进：

1. **第一批：复用现有类型的配置模板标准化**
   - 常规门
   - 钥匙门
   - 低中高容器
   - 道具容器
   - 蓝白/金色扭蛋机的容器化第一阶段配置
2. **第二批：补星愿方块和 Mission Terminal 运行时**
   - 星愿方块：正式接入 `StartDissolve`，补消失动画、碰撞移除、导航阻挡移除
   - Mission Terminal：正式接入 `TaskState`、`ShowTaskAcceptUI`、`StartTask`、`GiveReward`
3. **第三批：补扭蛋机差异化能力**
   - 消耗校验
   - 专属 UI / 动画
   - 高级池规则
4. **第四批：评估 Air Barrier 是否需要独立点位类型**
   - 如果只是门语义，不新增类型
   - 如果需要充放气/爆破/修复等独立状态流，再补类型和状态机
5. **最后同步文档**
   - 更新使用指南
   - 更新参数手册
   - 在场景概念文档中回填“哪些组件已进入 ECA”

## 验收标准

- [ ] 场景概念设计中的组件都已完成“纳入 ECA / 不纳入 ECA”的明确判定
- [ ] 每个纳入 ECA 的组件都有推荐点位类型和参数模板
- [ ] 已明确当前代码基线下哪些能力可直接依赖，哪些属于待补运行时
- [ ] 已明确本期场景配置以 `ECAPointMarker + FlowGraph` 为主，不依赖 Excel/Luban
- [ ] 已给出后续分批实现顺序，不把所有组件一次性硬塞进一个版本

## 关联文档

- [场景概念设计.md](../../概念-需求-设计文档/场景概念设计.md)
- [ECA框架使用指南-场景配置.md](./ECA框架使用指南-场景配置.md)
- [ECA场景配置参数手册.md](./ECA场景配置参数手册.md)

## 实现追踪

> 开发开始后补充

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 第一步：复用现有类型的配置模板标准化 | 待开发 | - | 当前仅完成设计文档 |
| 第二步：星愿方块和 Mission Terminal 运行时补齐 | 待开发 | - | 当前仅完成设计文档 |
| 第三步：扭蛋机差异化能力补齐 | 待开发 | - | 当前仅完成设计文档 |
