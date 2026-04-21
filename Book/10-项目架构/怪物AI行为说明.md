# 怪物AI行为说明

**关联模块**：`cn.etetet.statesync`、`cn.etetet.btnode`、`cn.etetet.map`  
**关联文档**：[模块现状总览.md](模块现状总览.md)、[怪物与机器人AI行为说明.md](怪物与机器人AI行为说明.md)、[怪物技能与AI设计文档.md](../概念-需求-设计文档/怪物技能与AI设计文档.md)  
**目的**：补齐普通怪物 `300001.asset` 与 5 只正式配置化怪物 `1012~1016 / 330101~330105` 的真实入口、参数和行为边界。  
**说明**：本文只记录当前工程已接通的服务端链路，不展开未来方案和资源表现设计。

## 1. 当前范围

| 类型 | 入口 | AI 挂载来源 | 当前用途 |
|---|---|---|---|
| 普通怪物 | `UnitFactory.Create` / 地图预摆单位 | `NumericType.AI` / `UnitConfig.KV[1009]` | 现有 `1003 Boar`、`1004 Bear` 默认走 `300001` |
| 正式配置化怪物 | `SpawnMonstersHelper.SpawnAtPoint(scene, point, groupId, count)` / `DebugSpawnMonsterHelper.TrySpawn` | `UnitConfig.KV[1009]` 对应的静态 `BuffConfigCategory` AI Buff | `1012 影刃猫`、`1013 侦察猴`、`1014 幻影猫头鹰`、`1015 重装鳄鱼`、`1016 机械巨熊·巴鲁` |

当前结论：

1. 怪物 AI 全部运行在服务端，客户端只消费同步后的移动、施法和数值结果。
2. 普通怪物仍以 `300001` 作为默认通用模板。
3. 5 只差异化怪物已经有正式 `UnitConfig`，地图 ECA 后续应直接填写正式数字 `UnitConfigId`。
4. `blade_cat / baloo` 这类字符串入口仍保留兼容，但已是废弃态，不再是正式配置口径。

## 2. 挂载入口

### 2.1 地图单位进入时的公共挂载

入口：`MapUnitEnterHelper.EnsureMapRuntimeComponents(scene, unit)`

关键行为：

1. 给怪物补 `TurnComponent`、`MoveComponent`、`PathfindingComponent`、`MailBoxComponent`、`TargetComponent`。
2. 怪物默认补 `CampComponent(2)` 和 `ThreatComponent`。
3. 调用 `EnsureConfiguredAIBuff(unit)`，读取 `NumericType.AI` 对应的 `BuffConfigId`。
4. 如果目标 AI Buff 尚未挂上，则通过 `BuffHelper.CreateBuff(...)` 真正启动行为树。

这意味着怪物 AI 的最终生效来源不是某个独立“AI 管理器”，而是单位数值里的 `NumericType.AI`。

### 2.2 刷怪时的正式单位分发

入口：`SpawnMonstersHelper.SpawnAtPoint(scene, spawnPointUnit, groupId, count)`

实际流程：

1. 先把 `groupId` 当正式数字 `UnitConfigId` 解析，这也是后续地图配置的标准入口。
2. 如果不是数字，才走 `MonsterRuntimeProfileHelper.TryResolveUnitConfigId(groupId, out unitConfigId)` 把旧字符串别名解析到正式 `UnitConfigId`，同时打废弃警告日志。
3. `UnitFactory.Create(...)` 创建怪物后，统一做 NavMesh 投影、记录出生点，并调用 `MapUnitEnterHelper.EnsureConfiguredAIBuff(monster, true)` 按正式 `NumericType.AI` 重建静态 AI Buff。
4. `130101~130143`、`230101~230143`、`330101~330105` 已在 `Packages/cn.etetet.statesync/Assets/BT` 静态资产和 `Packages/cn.etetet.map/Bundles/Json/*.txt` 导出配置中落地，不再依赖运行时注册。

因此当前地图刷怪的正式口径已经变成“优先刷正式 `UnitConfigId`，兼容旧字符串别名”。

## 3. 标准怪物 AI `300001.asset`

### 3.1 行为树结构

```text
BTGetBuffOwner
└── Selector
    ├── ReturnCheck -> Return
    ├── HasThreat -> Combat
    └── NOT HasThreat -> Patrol
```

### 3.2 参数来源

`300001.asset` 本身几乎没有覆盖节点字段，实际跑的是各节点类的默认值：

| 节点 | 字段 | 实际值 | 说明 |
|---|---|---|---|
| Buff 根节点 | `TickTime` | `500ms` | 每 500ms 触发一次 Buff Tick |
| `AI_MonsterReturnCheck` | `ReturnDistance` | `30m` | 离出生点超过 30m 才切回返程 |
| `AI_MonsterReturn` | `WaitIntervalMs` | `1000ms` | 回到出生点后每秒维持一次等待 |
| `AI_MonsterZhuiJi` | `ThinkIntervalMs` | `200ms` | 战斗思考频率 |
| `AI_MonsterZhuiJi` | `PreCastSpellId` | `100110` | 进入战斗协程时先施放一次 |
| `AI_MonsterZhuiJi` | `MainSpellId` | `100100` | 主技能 |
| `AI_MonsterXunLuo` | `PatrolMinRadius` | `0` | 允许从出生点原地开始随机 |
| `AI_MonsterXunLuo` | `PatrolMaxRadius` | `0` | 代码里会回退到当前 `AOI` |
| `AI_MonsterXunLuo` | `AggroRange` | `0` | 代码里会回退到当前 `AOI` |
| `AI_MonsterXunLuo` | `IdleMinMs / IdleMaxMs` | `1000 / 4000` | 巡逻点之间的停留时间 |
| `AI_MonsterXunLuo` | `ExitCombatBuffConfigId` | `200111` | 脱战巡逻前移除该 Buff |

### 3.3 实际运行细节

#### 巡逻

入口：`AI_MonsterXunLuoHandler`

1. 以 `UnitSpawnPointComponent.Position` 作为出生点；没有则退回当前坐标。
2. 如果 `ThreatComponent` 已有仇恨，立即结束巡逻，让行为树切到战斗分支。
3. 如果没有仇恨，会扫描 `AOIEntity.GetSeeUnits()` 中可见的玩家。
4. `AggroRange<=0` 时，实际索敌距离回退到怪物当前 `AOI`。
5. 选到目标后根据距离写入一份基础仇恨，再把目标写入 `TargetComponent`。
6. 若仍无目标，则在出生点附近随机取点巡逻。

#### 战斗

入口：`AI_MonsterZhuiJiHandler`

1. 每 `200ms` 从 `ThreatComponent` 里取最高仇恨目标。
2. 首次进入协程会先尝试施放一次 `PreCastSpellId=100110`。
3. 主循环按 `MainSpellId=100100` 的最小/最大施法距离决定行为：
   - 太远则寻路逼近
   - 太近则反向后撤
   - 距离合适则 `unit.Stop(0)` 并施法
4. 如果当前 `SpellComponent.Current` 正是同一技能对应 Buff，则不会重复起手。

#### 返程

入口：`AI_MonsterReturnCheckHandler`、`AI_MonsterReturnHandler`

1. 当怪物与出生点距离大于等于 `30m` 时，返程分支成立。
2. 返程开始先清空 `ThreatComponent`。
3. 然后寻路回出生点，回到后按 `1000ms` 间隔维持等待。

## 4. 正式配置化怪物（静态 BT 资产）

### 4.1 档案映射

| 正式 UnitConfigId | 废弃兼容别名 | AI Buff | 静态落表数值 | 当前定位 |
|---|---|---|---|---|
| `1012` | `blade_cat` / `影刃猫` | `330101` | `HP=260, SpeedBase=2350, Radius=430, AOI=10000` | 近战突刺小怪 |
| `1013` | `scout_monkey` / `侦察猴` | `330102` | `HP=220, SpeedBase=2100, Radius=420, AOI=11000` | 目标点延迟爆炸小怪 |
| `1014` | `phantom_owl` / `幻影猫头鹰` | `330103` | `HP=210, SpeedBase=2250, Radius=410, AOI=12000` | 双段回旋小怪 |
| `1015` | `heavy_gator` / `重装鳄鱼` | `330104` | `HP=520, SpeedBase=1750, Radius=520, AOI=11000` | 扇形压制精英 |
| `1016` | `baloo` / `巴鲁` / `机械巨熊·巴鲁` | `330105` | `HP=960, SpeedBase=1850, Radius=600, AOI=12000` | 双技能轮转 Boss |

说明：

1. 这些数值已经静态写入 `Unit.xlsx`，不会再依赖占位 `1003/1004` 覆写。
2. `blade_cat / baloo` 这类 groupId 只保留到正式 `UnitConfigId` 的废弃兼容映射，不再承担技能/AI 配置职责。
3. 正式怪物的 AI 标识仍然是 `NumericType.AI = 330101~330105`。

### 4.2 静态资产入口

当前实现已经改为读取静态 BT 资产和导出文本：

1. `SpellScriptableObject`：`Packages/cn.etetet.statesync/Assets/BT/Spell/*/*.asset`
2. `BuffScriptableObject`：`Packages/cn.etetet.statesync/Assets/BT/Spell/*/*.asset`
3. `AI Buff`：`Packages/cn.etetet.statesync/Assets/BT/AI/330101.asset` ~ `330105.asset`
4. 导出文本：`Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt`、`Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt`

这意味着 5 只怪虽然共享同一套大框架，但 `ThinkInterval`、主技能、巡逻半径、警戒范围和返程距离都已经固化在静态 BT 资产中。

### 4.3 共用行为框架

这些正式怪物虽然配置来源已经静态化，但战斗逻辑仍然通过专门 Handler + `MonsterCombatCommonHelper` 实现：

| 能力 | 入口 | 作用 |
|---|---|---|
| 初始上下文提取 | `TryGetInitialContext` | 统一取 `unit/root/threat/unitRadius` |
| 目标刷新 | `TryRefreshTarget` | 从最高仇恨目标同步到 `TargetComponent` |
| 施法距离维护 | `TryMaintainCastRange` | 太远前进、太近后撤、距离合适停步 |
| 施法前转向 | `FaceTarget` | 正式静态怪在尝试施法前统一朝向当前目标，避免 `TargetSelectorSingle` 因目标不在正前方而空转 |
| 施法前检查 | `HasCastingSpell` | 已在施法时避免重入 |
| 真正施法 | `TryCast` | `unit.Stop(0)` 后执行 `SpellHelper.Cast`；失败时写 `MonsterAI` 调试日志 |

### 4.4 单怪差异

#### 影刃猫

- `ThinkIntervalMs=160`
- 主技能：短前摇后单体命中
- 巡逻半径：`1.2m ~ 4.5m`
- 警戒范围：`8.5m`
- 返程距离：`16m`

#### 侦察猴

- `ThinkIntervalMs=180`
- 主技能：目标点延迟爆炸
- 巡逻半径：`1.5m ~ 5.5m`
- 警戒范围：`10m`
- 返程距离：`18m`

#### 幻影猫头鹰

- `ThinkIntervalMs=140`
- 主技能：去程 + 回程双段命中
- 巡逻半径：`1.5m ~ 6m`
- 警戒范围：`11m`
- 返程距离：`20m`

#### 重装鳄鱼

- `ThinkIntervalMs=260`
- 主技能：前方扇形喷射
- 巡逻半径：`0.6m ~ 3.5m`
- 警戒范围：`9m`
- 返程距离：`14m`

#### 巴鲁

- `ThinkIntervalMs=260`
- 主技能：弹簧拳前摇 + 命中
- 副技能：积木雨轰炸
- 狂暴阈值：`45% HP`（`EnrageHpPermille=450`）
- 狂暴后思考间隔：`160ms`
- 狂暴后移速：基础移速的 `135%`
- 技能成功施放后会在主技能 / 副技能之间轮换

## 5. 当前边界

1. 普通怪物 `300001` 和 5 只正式配置化怪物都属于服务端 BT 行为树，客户端没有单独怪物 AI。
2. 地图刷怪的正式入口已经是纯数字 `UnitConfigId`；旧字符串 `groupId` 仅保留兼容，不建议继续使用。
3. 5 只怪的静态身份、基础数值、尸体掉落、`Spell/Buff/AI` 资产和导出文本都已落地；后续技能动画、预警、特效也应直接在静态 BT 资产上维护。
4. 机器人 AI、匹配机器人枪械控距和默认回落逻辑见 [怪物与机器人AI行为说明.md](怪物与机器人AI行为说明.md)。
