# 怪物技能与AI设计文档

**创建时间**：2026-03-27
**最后更新**：2026-03-28（补充技能动画与特效配置说明文档）
**状态**：已完成
**关联任务**：M0.2-W2 #10、M0.2-W3 #9
**涉及包**：cn.etetet.statesync、cn.etetet.btnode、cn.etetet.map、cn.etetet.spell

---

## 需求概述

本期先不等正式资源到位，直接推进 5 只怪物的技能与 AI 实装，目标是尽快把“可打、可躲、可调参”的战斗闭环跑起来。

本轮已确认的约束：

- 5 只怪物全部进入实现范围。
- 模型、特效、音效允许先使用占位资源。
- 侦察猴不做致盲，只保留伤害反馈。
- 每只怪物都创建新的独立怪物 AI，不继续共用单一通用怪物 AI 配置。

---

## 总体方案

### 设计原则

- **AI 独立**：每只怪物单独创建 `AIBuffConfig + BT 资产`，便于后续单怪调参和差异化迭代。
- **运行时先行**：第一版不等待 Luban 补齐正式 `UnitConfig`，先以运行时 `MonsterProfile` 覆写占位怪物的 AI 与核心数值，把技能和战斗闭环先跑通。
- **底层复用**：虽然 AI 配置独立，但巡逻 / 返回 / 目标锁定 / 施法链路尽量复用现有通用能力。
- **技能优先**：优先复用现有技能链路、目标选择和 `SpellHelper.Cast`，只有现有系统明显不够时再新增节点。
- **资源解耦**：战斗逻辑先用占位表现跑通，不把模型/特效/音效作为前置阻塞。

### 现有可复用链路

- 怪物创建时，`UnitFactory` 会读取 `NumericType.AI`，自动创建对应 AI Buff。
- 现有技能系统支持 `SpellConfig -> BuffConfig -> BTCreateSpell` 的组合触发。
- 现有行为树提供 `BTHasThreat`、`AI_MonsterReturn`、`AI_MonsterXunLuo`、`TargetSelectorSingle`、`TargetSelectorCircle`、`BTForeachUnit`、`BTDamage` 等通用节点。
- `SpellConfigCategory`、`BuffConfigCategory` 支持运行时 `Add`，可以在不改 Excel 生成代码的前提下注册首版怪物技能与 AI。

### 首版落地方案

#### 运行时 MonsterProfile

- 在 `cn.etetet.statesync` 新增运行时怪物配置注册器。
- `groupId` 优先解析为 `MonsterProfile`，未命中时仍兼容旧逻辑：字符串数字继续按 `UnitConfigId` 刷怪。
- 每个 `MonsterProfile` 包含：
  - 占位 `UnitConfigId`
  - 独立 `AIBuffConfigId`
  - 技能 `SpellConfigId`
  - 巡逻 / 感知 / 施法距离
  - 首版核心数值覆写（HP、移速、半径等）
- 创建怪物后立即覆写数值和 `NumericType.AI`，再调用 `MapUnitEnterHelper.EnsureConfiguredAIBuff(monster, true)` 重建 AI。

#### AI 结构

每只怪物都单独配置一套：

- 巡逻分支：进入感知范围前的随机游走 / 待机。
- 追击分支：锁定目标、走位到合适距离、释放主技能。
- 返回分支：脱战后回出生点，清仇恨，恢复巡逻。

差异主要体现在：

- `AggroRange`
- `PreferredRange`
- `ThinkIntervalMs`
- `MainSpellId`
- 巡逻半径
- 是否需要新增专用战斗节点

#### 预警和延迟生效实现

- 第一版不依赖 `AI_MonsterZhuiJi.PreCastSpellId` 做每轮前摇，因为该逻辑当前更接近协程启动时施放一次。
- 每个怪物的“预警 / 前摇 / 延迟爆发”统一收进技能 Buff 链里处理：
  - 主技能先挂一个短持续 Buff
  - Buff 移除时再触发伤害子技能或范围结算
- 这样不会影响现有机器人和旧怪物 AI。

---

## 单怪设计

### 1. 影刃猫

**定位**：近战突刺小怪，负责给玩家明确的“横向躲技能”压力。

**技能方案**

- 预警技能：红线预警，占位特效即可。
- 主技能：直线突刺，命中造成一次切割伤害。
- 未命中反馈：先做成短暂停顿或短后摇，不强依赖撞墙演出。

**AI 方案**

- 新建专属 AI。
- 巡逻 / 返回沿用基础节点。
- 追击阶段使用“接近目标 -> 播预警 -> 直线突刺 -> 进入短后摇”的模式。

**实现建议**

- 第一版做成“短前摇 + 单体爆发”的近战突刺近似，不强依赖真实位移。
- 后续如果要补真实突刺，再加位移节点，不阻塞当前闭环。

### 2. 捣蛋侦察猴

**定位**：近中距离骚扰投射物小怪。

**技能方案**

- 主技能为“发条遥控车”占位实现。
- 第一版不做致盲，只做小范围爆炸伤害。
- 投射物路径先按直线处理，命中或到时后爆炸。

**AI 方案**

- 新建专属 AI。
- 与影刃猫不同，保持略远于近战怪的攻击距离。
- 进入施法距离后直接释放主技能，避免贴脸站桩。

**实现建议**

- 第一版做成“延迟后目标点小范围爆炸”的占位版本。
- 不做致盲，仅保留伤害与范围压力。

### 3. 幻影猫头鹰

**定位**：高机动回旋飞行道具小怪。

**技能方案**

- 主技能是双飞镖回旋。
- 第一版核心是“去程 + 回程双判定”，不强求完整的华丽轨迹。
- 可先实现为两段式飞行：射出命中判定一次，回收再判定一次。

**AI 方案**

- 新建专属 AI。
- 保持中距离输出，避免长时间贴身。
- 可在战斗分支中预留“短闪烁换位”接口，但第一版不必强行做复杂位移演出。

**实现建议**

- 第一版不强求真实回旋轨迹，先做“双段命中”的战斗语义。
- 可用一次施法中的双段子技能实现去程 / 回程伤害。

### 4. 重装鳄鱼

**定位**：范围压制型精英怪。

**技能方案**

- 主技能为前方扇形泡泡喷射。
- 泡泡落地形成减速区，第一版可先做扇形命中 + 持续减速伤害区。
- 背后弱点机制先做数据接口和判定预留，不强行在第一版做完整弱点演出。

**AI 方案**

- 新建专属 AI。
- 巡逻比小怪更短，感知更强。
- 进入技能距离后优先站定施法，强调压制区域而非频繁追击。

**实现建议**

- 第一版补齐服务端扇形目标选择能力。
- 地面持续区与背后弱点本轮只做接口预留，不阻塞首版 AI。

### 5. 机械巨熊·巴鲁

**定位**：Boss，多技能轮转与阶段变化。

**技能方案**

- 技能1：弹簧拳冲击，直线重击，可预警。
- 技能2：积木雨轰炸，随机区域红圈轰炸。
- 狂暴形态：低血量后提升移动 / 攻击频率，强化近战压迫。

**AI 方案**

- 新建专属 Boss AI。
- 需要比普通怪更多的技能选择与阶段状态切换。
- 第一版先做“正常阶段技能轮转 + 低血量狂暴切换”，不依赖完整演出资源。

**实现建议**

- 这是 5 只怪里新增逻辑最多的一只。
- 第一版优先保证两段技能循环和狂暴切换，随机红圈轰炸先近似为目标区域轰炸。
- 拳头反弹等高复杂细节排到后续补强。

---

## 实现拆分建议

### W2

- 影刃猫：完成预警 + 突刺 + 独立 AI。
- 侦察猴：完成投射物爆炸伤害 + 独立 AI。
- 幻影猫头鹰：完成双段回旋伤害 + 独立 AI。

### W3

- 重装鳄鱼：完成扇形压制 + 减速区 + 独立 AI。
- 机械巨熊·巴鲁：完成双技能轮转 + 狂暴形态 + 独立 Boss AI。

---

## 涉及的实现对象

| 类型 | 预计动作 |
|------|------|
| MonsterProfile | 新增运行时怪物档案，负责 `groupId -> 占位配置 + 数值 + AI/技能` |
| BuffConfig | 为 5 只怪新增独立 AI BuffConfig |
| SpellConfig | 为 5 只怪新增运行时技能配置 |
| BT 运行时树 | 为每只怪创建独立 AI 树和技能效果树 |
| Server 代码 | 对缺失的扇形范围、Boss 双技能轮转、低血量狂暴补节点 / 补处理器 |
| ECA / group_id | 首版兼容旧数字 `groupId`，后续可切字符串档案名做精确刷怪 |

---

## 验收标准

- 5 只怪物均可在无正式资源条件下跑通技能与 AI。
- 每只怪物均有独立 AI 配置，不共用单一怪物 AI。
- 侦察猴不包含致盲逻辑，只保留伤害表现。
- 玩家可以明确感知每只怪物的差异化攻击方式和躲避方式。

---

## 实现追踪

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 文档方案更新 | 2026-03-27 | `Book/概念-需求-设计文档/怪物技能与AI设计文档.md` | 首版改为运行时 MonsterProfile，不直接改 Luban `UnitConfig` |
| 运行时 MonsterProfile 注册与刷怪映射 | 2026-03-27 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/MonsterRuntimeProfileHelper.cs`、`Packages/cn.etetet.statesync/Scripts/Hotfix/Server/SpawnMonstersHelper.cs` | 为兼容 ET Hotfix 分析器，最终改为“纯静态分发 + 幂等注册”，不保留普通 profile 类与静态字典 |
| 5 只怪独立 AI 与技能链路 | 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/*`、`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/*`、`Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTDamageSpellTargetCircle.cs`、`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTDamageSpellTargetCircleHandler.cs`、`Packages/cn.etetet.btnode/Scripts/Model/Share/Root/TargetSelectorSector.cs`、`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Root/BTTargetSelectorSectorHandler.cs` | 首版按占位技能语义落地；鳄鱼本轮先做扇形伤害压制，持续减速区与背后弱点留待后续补强 |
| 编译收口 | 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BalooCombatHandler.cs`、`ET.Hotfix.csproj`、`ET.Model.csproj` | 修复 `await` 后 Entity 访问问题后，`dotnet build ET.sln` 已通过；尚未做场景内手工回归 |

---

## 关联文档

- [怪物概念设计.md](./怪物概念设计.md)
- [怪物技能动画与特效配置说明.md](./怪物技能动画与特效配置说明.md)
