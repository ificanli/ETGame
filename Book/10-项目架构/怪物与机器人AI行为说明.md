# 怪物与机器人AI行为说明

**关联模块**：`cn.etetet.statesync`、`cn.etetet.btnode`  
**关联文档**：[模块现状总览.md](模块现状总览.md)、[怪物AI行为说明.md](怪物AI行为说明.md)  
**目的**：补齐 `300010.asset`、`300011.asset` 的真实行为说明，明确通用机器人 AI、匹配机器人枪械 AI 的运行边界；普通怪物与运行时 MonsterProfile 怪物见独立文档。  
**说明**：本文只记录当前工程中已经接通的服务端行为树与运行时挂载方式，不展开未来方案。

## 1. 当前 AI 资产定位

| BuffConfigId | 资产 | 当前用途 | 备注 |
|---|---|---|---|
| `300001` | 标准怪物AI | 普通怪物巡逻/追击/脱战返回 | 详细入口与参数见 [怪物AI行为说明.md](怪物AI行为说明.md) |
| `300010` | 机器人AI | 施法型/近战型机器人模板 | 战斗段通过 `SpellHelper.Cast` 主动施法 |
| `300011` | 匹配机器人枪械AI | 匹配机器人默认 AI | `MatchRobotRuntimeHelper` 默认回落到它 |

## 2. `300010.asset` 的实际行为

### 2.1 行为树结构

```text
BTGetBuffOwner
└── Selector
    ├── ReturnCheck -> Return
    ├── CombatCheck -> Combat
    └── NOT CombatCheck -> Patrol
```

### 2.2 关键参数

| 节点 | 参数 | 当前值 | 说明 |
|---|---|---|---|
| Buff 根节点 | `TickTime` | `500ms` | 以 Buff Tick 驱动整棵树 |
| `AI_RobotReturnCheck` | `MaxDistance` | `35m` | 超出出生点 35 米时进入返程 |
| `AI_RobotReturn` | `WaitIntervalMs` | `1000ms` | 回到出生点后维持等待 |
| `AI_RobotReturn` | `ExitCombatBuffConfigId` | `200111` | 返程前移除脱战 Buff |
| `AI_RobotCombatCheck` | `MaxRange` | `18m` | 搜索/验证目标的最大距离 |
| `AI_RobotCombatCheck` | `SelectIntervalMs` | `300ms` | 目标重选节流 |
| `AI_RobotCombat` | `ThinkIntervalMs` | `200ms` | 战斗思考频率 |
| `AI_RobotCombat` | `PreCastSpellId` | `100110` | 进入战斗协程时先施放一次 |
| `AI_RobotCombat` | `MainSpellId` | `100100` | 主施法技能 |
| `AI_RobotPatrol` | `MinRadius` / `MaxRadius` | `2m` / `8m` | 围绕出生点随机巡逻 |
| `AI_RobotPatrol` | `IdleMinMs` / `IdleMaxMs` | `1000ms` / `3000ms` | 巡逻点间的停留时间 |
| `AI_RobotPatrol` | `ExitCombatBuffConfigId` | `200111` | 巡逻前移除脱战 Buff |

### 2.3 运行时细节

1. `AI_RobotCombatCheckHandler` 会优先复用当前目标；目标失效后，使用 `TargetSelectorComponent` 在 `18m` 内重选，仍失败时再退回 `ThreatComponent` 的最高仇恨目标。
2. `AI_RobotCombatHandler` 进入时如果配置了 `PreCastSpellId`，会立即执行一次 `SpellHelper.Cast(unit, 100110)`。
3. 主循环中会根据 `MainSpellId=100100` 对应技能配置的最小/最大施法距离决定行为：
   - 距离过远：向目标寻路逼近。
   - 距离过近：按目标反方向后撤。
   - 距离合适：`unit.Stop(0)`，然后施放主技能。
4. `AI_RobotReturnHandler` 在返程时会清理 `ThreatComponent`、`TargetComponent` 和 `TargetSelectorComponent` 的目标状态，再移动回出生点。
5. `AI_RobotPatrolHandler` 每轮都会清空当前目标，并在出生点附近 `2~8m` 随机取点巡逻。

## 3. `300011.asset` 的实际行为

### 3.1 与 `300010` 的共同部分

- Buff Tick、返程、巡逻三段结构与 `300010` 相同。
- `ReturnCheck=35m`、`CombatCheck=18m/300ms`、`Patrol=2~8m + 1000~3000ms` 这些参数保持一致。
- 同样围绕 `UnitSpawnPointComponent.Position` 作为出生点运转。

### 3.2 战斗段的关键差异

| 项目 | `300010` | `300011` |
|---|---|---|
| 检查节点 | `AI_RobotCombatCheck` | `AI_RobotWeaponCombatCheck` |
| 战斗节点 | `AI_RobotCombat` | `AI_RobotWeaponCombat` |
| `PreCastSpellId` | `100110` | `0` |
| `MainSpellId` | `100100` | `0` |
| 战斗方式 | 主动施法 | 维持枪械射击距离，不主动施法 |

### 3.3 `AI_RobotWeaponCombat` 的基础参数

`300011.asset` 没有覆盖下面这些字段，因此基础值仍然来自 `AI_RobotWeaponCombat` 类：

| 字段 | 基础值 | 作用 |
|---|---|---|
| `PreferredMinDistance` | `5m` | 理想距离区间下限 |
| `PreferredMaxDistance` | `8m` | 理想距离区间上限 |
| `StopMoveTolerance` | `1m` | 停止移动的容忍带 |
| `RetreatDistance` | `3m` | 触发后撤的阈值 |
| `RetreatStepDistance` | `4m` | 单次后撤步长 |

> 说明：这些值现在只作为“中远程武器默认带宽”。运行时会再按当前武器实际射程做一次收敛，避免短射程机器人长期停在射程外。

### 3.4 运行时细节

1. `AI_RobotWeaponCombatCheckHandler` 同样先尝试复用当前目标，失效后在 `18m` 内重选目标，并把结果写入 `TargetComponent`。
2. `AI_RobotWeaponCombatHandler` 每 `200ms` 评估一次和目标的水平距离。
3. 理想距离不再死板固定在 `5m~8m`：
   - 中远程武器仍以 `5m~8m` 作为默认控距带。
   - 短射程武器会根据 `TargetSelectorComponent.MaxRange`（即当前武器有效射程）自动压缩理想距离、停步容忍带和后撤阈值，主动贴近到可开火范围。
4. 自动开火链路中的 `BTWeaponHasTarget` 会优先复用 `TargetComponent` 中已经由 AI 选出的目标，避免“AI 已经锁敌，但武器树重新索敌失败”。
5. 自动开火链路中的 `BTWeaponInRange` 会优先读取运行时武器射程，而不是只信 BT 资产里的固定值。
6. 该节点本身不施放技能，默认依赖单位已有的武器/自动攻击链路输出。

## 4. 匹配机器人运行时挂载

### 4.1 入口

- `MatchRobotRuntimeHelper.TryBuildSpawnProfile`
- `MatchRobotRuntimeHelper.ResolveMatchRobotAIBuffConfigId`
- `MapUnitEnterHelper.SetupMatchRobotIfNeeded`

### 4.2 当前行为

1. 匹配机器人生成时，会按地图名、模式和机器人 `PlayerId` 选择英雄、主武器、AI Buff 和自动选牌延迟。
2. 如果配置中的 `AIBuffConfigId` 无效，`MatchRobotRuntimeHelper` 会回落到默认值 `300011`。
3. 也就是说，当前匹配机器人默认使用的是“枪械距离控制 AI”，而不是 `300010` 的施法型机器人 AI。
4. 该 AI 现在已经按当前武器射程动态调节接敌距离，短射程武器不会再长期停在武器射程外。

## 5. 当前边界结论

1. `300001` 仍是普通怪物主用 AI。
2. `300010` 适合带明确技能施放的机器人单位。
3. `300011` 是当前匹配机器人默认模板，核心职责不是施法，而是把机器人维持在适合当前武器的射击距离。
4. 这两份 BT 都是服务端行为树；客户端没有单独的机器人 AI 逻辑，只消费同步后的移动、战斗和视图结果。
