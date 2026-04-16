# 怪物技能与AI静态配置化设计文档

**创建时间**：2026-04-10  
**最后更新**：2026-04-10  
**状态**：已完成  
**关联任务**：M0.2-W3 #16  
**涉及包**：cn.etetet.statesync, cn.etetet.map, cn.etetet.btnode, cn.etetet.spell

## 需求概述

当前影刃猫、侦察猴、幻影猫头鹰、重装鳄鱼、巴鲁这 5 只怪，虽然已经有正式 `UnitConfig(1012~1016)` 与独立掉落配置，但技能、Buff、AI Buff 仍依赖 `MonsterRuntimeProfileHelper` 在运行时动态注册。

这会造成：

- 正式怪物静态身份已落表，但技能/AI 仍保留第二套运行时来源；
- 刷怪、GM、测试需要额外调用 `EnsureRegistered` / `ApplyProfile`；
- 技能动画、预警、特效的正式配置口径仍停留在代码里，不利于后续按老静态 BT 资产方式维护。

本次目标是把这 5 只怪的 `SpellConfig / BuffConfig / AI Buff` 收口到老链路使用的静态 BT 资产，并导出到正式配置文件中，让怪物统一走：

`UnitConfig -> NumericType.AI -> BuffConfigCategory(静态) -> BT`

## 技术方案

### 整体思路

本次不改 5 只怪的技能语义和数值边界，只改“配置来源”：

1. 保持现有正式 `UnitConfig(1012~1016)` 和 `NumericType.AI=330101~330105` 不变。
2. 新增 5 只怪对应的静态 `SpellScriptableObject / BuffScriptableObject` 资产，落到 `Packages/cn.etetet.statesync/Assets/BT/`。
3. 通过现有 `ExportScriptableObject` 导出链路，把静态 BT 资产导出到 `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt` 与 `BuffConfigCategory.txt`。
4. 刷怪、GM、测试统一依赖静态 `BuffConfigCategory`，去掉对 `MonsterRuntimeProfileHelper` 运行时注册和覆写数值的主依赖。
5. `MonsterRuntimeProfileHelper` 保留怪物分组识别、旧字符串到正式 `UnitConfigId` 的废弃兼容映射，以及 `IsBoss / IsElite / MatchesCombatFilter` 这类业务辅助能力；不再作为正式技能/AI 的主配置来源。

### 老静态链路确认

当前老怪物链路已经是静态 BT 资产：

- 标准怪物 AI：`Packages/cn.etetet.statesync/Assets/BT/AI/300001.asset`
- 标准怪物技能：`Packages/cn.etetet.statesync/Assets/BT/Spell/100100-MonsterAutoAttack/*`
- 标准怪物战斗 Buff：`Packages/cn.etetet.statesync/Assets/BT/Spell/100110-MonsterBattle/*`
- 导出结果：`Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt`、`Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt`

本次 5 只怪将统一接入这条链路，不再长期保留“老怪静态、特殊怪运行时”的双轨形态。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增 W3 任务16 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录 |
| `Book/概念-需求-设计文档/怪物技能与AI静态配置化设计文档.md` | 新增 | 本设计文档 |
| `Book/概念-需求-设计文档/怪物技能与AI静态配置化开发日志.md` | 新增 | 开发过程留档 |
| `Packages/cn.etetet.statesync/Assets/BT/AI/*.asset` | 新增 | 5 只怪的静态 AI Buff 资产 |
| `Packages/cn.etetet.statesync/Assets/BT/Spell/*/*.asset` | 新增 | 5 只怪的静态 Spell/Buff 资产 |
| `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt` | 修改 | 导出静态技能配置 |
| `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 导出静态 Buff/AI 配置 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/MonsterRuntimeProfileHelper.cs` | 修改 | 去掉运行时注册主职责，仅保留分组/兼容辅助 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/SpawnMonstersHelper.cs` | 修改 | 不再依赖运行时注册 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/DebugSpawnMonsterHelper.cs` | 修改 | 不再依赖运行时注册 |
| `Packages/cn.etetet.test/Scripts/Hotfix/Test/TestHelper.cs` | 修改 | 提供按正式 `UnitConfigId` 建怪辅助 |
| `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Rogue_Fixes_P0_Test.cs` | 修改 | 测试收口到正式 `UnitConfigId` |
| `Book/10-项目架构/怪物AI行为说明.md` | 修改 | 回写静态配置化后的真实入口 |
| `Book/概念-需求-设计文档/怪物技能动画与特效配置说明.md` | 修改 | 回写技能动画正式配置入口 |

### 数据结构

#### 1. 正式怪物静态 AI Buff

保留现有 AI BuffId，不改 `UnitConfig` 绑定关系：

| 怪物 | UnitConfigId | AI BuffId |
|------|------|------|
| 影刃猫 | 1012 | 330101 |
| 侦察猴 | 1013 | 330102 |
| 幻影猫头鹰 | 1014 | 330103 |
| 重装鳄鱼 | 1015 | 330104 |
| 巴鲁 | 1016 | 330105 |

#### 2. 技能与 Buff 静态化 ID

沿用当前运行时注册时已经使用的 ID，不做二次改号：

| 怪物 | SpellId | BuffId |
|------|------|------|
| 影刃猫 | 130101 / 130102 | 230101 / 230102 |
| 侦察猴 | 130111 | 230111 |
| 幻影猫头鹰 | 130121 / 130122 / 130123 | 230121 / 230122 / 230123 |
| 重装鳄鱼 | 130131 | 230131 |
| 巴鲁 | 130141 / 130142 / 130143 | 230141 / 230142 / 230143 |

#### 3. 技能逻辑接入方式

这 5 只怪的技能仍然接 BT，只是从“运行时代码构造”改成“静态 BT 资产”：

- `SpellConfig`：技能入口、CD、目标选择、预警指示器
- `BuffConfig`：服务端时序、伤害、子技能、客户端动画/特效节点
- `AI Buff`：巡逻 / 战斗 / 返程行为树

### 迁移策略

#### 1. 先按旧运行时定义做一次性迁移

为避免迁移时改出技能语义偏差，本次静态资产以旧运行时定义为基准做一次性迁移，确保：

- 技能 ID 不变
- 目标选择范围不变
- AI 思考间隔、巡逻半径、警戒范围、返程距离不变
- 巴鲁双技能轮转和狂暴阈值不变

#### 2. 再收口运行时链路

静态资产与导出配置落地后，再移除以下主依赖：

- `SpawnMonstersHelper` 中的 `MonsterRuntimeProfileHelper.EnsureRegistered`
- `DebugSpawnMonsterHelper` 中的 `MonsterRuntimeProfileHelper.EnsureRegistered`
- 测试中对 `ApplyProfile(groupId)` 的主依赖

实际执行结果：

- 已生成 25 个静态 BT 资产并完成导出。
- 已删除临时迁移编辑器脚本，不在仓库里保留第二套怪物技能/AI 正式来源。

### 风险与处理

#### 风险1：Unity BT 资产手写 YAML 容易出错

- 处理：已通过一次性编辑器迁移脚本生成 `SpellScriptableObject / BuffScriptableObject`，导出完成后删除临时脚本，不手工硬写复杂 YAML。

#### 风险2：静态资产已创建但导出文本未更新

- 处理：本次开发必须显式执行 `ExportScriptableObject` 对应导出，并核对 `SpellConfigCategory.txt` / `BuffConfigCategory.txt` 内容。

#### 风险3：旧测试仍创建“空怪 + ApplyProfile”

- 处理：新增测试辅助入口，统一按正式 `UnitConfigId` 建怪；保留旧辅助兼容期尽量短。

## 实现步骤

1. 补计划与设计/开发文档，登记“怪物技能/Buff/AI 静态配置化”任务。
2. 生成并落盘 5 只怪的静态 `Spell/Buff/AI Buff` BT 资产。
3. 导出静态 BT 资产，更新 `SpellConfigCategory.txt` 与 `BuffConfigCategory.txt`。
4. 修改刷怪、GM、测试辅助和 `MonsterRuntimeProfileHelper`，切换为正式静态链路主口径。
5. 编译验证并回写架构/设计/开发文档。

## 验收标准

- [x] 5 只怪的 `SpellConfig / BuffConfig / AI Buff` 均有静态 BT 资产
- [x] `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt` 包含 `130101~130143`
- [x] `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` 包含 `230101~230143` 与 `330101~330105`
- [x] 地图刷怪与 GM 召怪不再依赖 `MonsterRuntimeProfileHelper.EnsureRegistered`
- [x] 测试辅助不再依赖 `ApplyProfile(groupId)` 作为正式入口
- [x] `dotnet build ET.sln` 通过

## 关联文档

- [怪物正式配置化设计文档.md](./怪物正式配置化设计文档.md)
- [怪物技能与AI设计文档.md](./怪物技能与AI设计文档.md)
- [怪物技能动画与特效配置说明.md](./怪物技能动画与特效配置说明.md)
- [怪物AI行为说明.md](../10-项目架构/怪物AI行为说明.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-10 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md`、`Book/概念-需求-设计文档/怪物技能与AI静态配置化设计文档.md`、`Book/概念-需求-设计文档/怪物技能与AI静态配置化开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/AI/*.asset`、`Packages/cn.etetet.statesync/Assets/BT/Spell/*/*.asset` | 使用一次性编辑器脚本在已打开 Unity 中生成，导出完成后已删除脚本 |
| 步骤3 | 2026-04-10 | `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt`、`Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 无偏差 |
| 步骤4 | 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/MonsterRuntimeProfileHelper.cs`、`Packages/cn.etetet.statesync/Scripts/Hotfix/Server/SpawnMonstersHelper.cs`、`Packages/cn.etetet.statesync/Scripts/Hotfix/Server/DebugSpawnMonsterHelper.cs`、`Packages/cn.etetet.test/Scripts/Hotfix/Test/TestHelper.cs`、`Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Rogue_Fixes_P0_Test.cs` | 无偏差 |
| 步骤5 | 2026-04-10 | `Book/10-项目架构/怪物AI行为说明.md`、`Book/概念-需求-设计文档/怪物技能动画与特效配置说明.md` | 额外补跑 `Test_Rogue_Fixes_P0_Test`，比设计更完整 |
