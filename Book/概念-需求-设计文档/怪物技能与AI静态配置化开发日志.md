# 怪物技能与AI静态配置化开发日志

**功能**：怪物技能/Buff/AI 静态配置化  
**关联设计文档**：[怪物技能与AI静态配置化设计文档.md](./怪物技能与AI静态配置化设计文档.md)  
**关联任务**：M0.2-W3 #16  
**开始时间**：2026-04-10  
**开发者**：AI

## 开发进度

- [x] 步骤1：补计划与设计/开发文档
- [x] 步骤2：生成 5 只怪的静态 Spell/Buff/AI BT 资产
- [x] 步骤3：导出静态 BT 配置文本
- [x] 步骤4：收口刷怪/GM/测试辅助到正式静态链路
- [x] 步骤5：编译验证并回写文档

## 决策记录

### 2026-04-10 - 怪物技能与AI改为统一静态 BT 资产链路
- **背景**：5 只怪已经有正式 `UnitConfig`，但技能/Buff/AI 仍来自 `MonsterRuntimeProfileHelper` 运行时注册，形成静态/动态双轨。
- **方案**：把 5 只怪的 `SpellConfig / BuffConfig / AI Buff` 收口到 `Packages/cn.etetet.statesync/Assets/BT` 静态资产，并导出到 `Packages/cn.etetet.map/Bundles/Json`。
- **原因**：这样才能与旧怪物保持同一条正式配置链路，后续技能动画、预警和特效也能回到老静态 BT 资产口径维护。
- **替代方案**：继续保留运行时注册，仅在正式入口上改用 `UnitConfigId`；放弃原因是用户明确要求不要长期保留两套怪物配置来源。

### 2026-04-10 - 使用已打开 Unity 做一次性静态资产迁移
- **背景**：批处理 Unity 被同项目已打开实例阻塞，无法直接 `-batchmode` 打开工程执行导出。
- **方案**：临时加入一次性编辑器迁移脚本，借助当前已打开的 Unity 生成 25 个静态 BT 资产并完成导出，成功后立即删除该临时脚本。
- **原因**：这样可以绕开项目锁，同时不在仓库里长期保留第二套怪物技能/AI 配置来源。
- **替代方案**：等待用户关闭 Unity 再走批处理；放弃原因是当前打开编辑器可以直接复用，推进更快。

### 2026-04-16 - 静态怪战斗协程统一在施法前转向目标
- **背景**：场景回归中，`XiaoHou / XiaoYing` 出现“站着不动也不攻击”的表现，排查后发现静态怪战斗协程直接 `SpellHelper.Cast`，但 `TargetSelectorSingle` 会检查“目标是否在施法者前方”。
- **方案**：在 `MonsterCombatCommonHelper` 增加共用 `FaceTarget`，并在 5 只静态怪的战斗协程正式施法前统一转向当前目标。
- **原因**：这能一次性修正影刃猫、幻影猫头鹰、重装鳄鱼、巴鲁这类单体/定向技能的站桩空转问题；对侦察猴的目标点技能也能统一视觉朝向。
- **替代方案**：分别回头去每个 `BuffScriptableObject` 里补 `BTTurnToUnit/BTTurnToPos`；放弃原因是改动分散、维护成本高，而且不能覆盖战斗协程层的共性问题。

### 2026-04-17 - PVEMap 临时切成单怪正式回归入口
- **背景**：用户希望单独在 `PVEMap` 里验证“这只怪”的索敌、走位和出手；排查发现 `PVEMap.txt` 仍保留两个 `SpawnMonsters(group_id=1003)` 点位，实际刷的是旧 `Boar`，并不是这轮正式静态怪。
- **方案**：把 `PVEMap` 收口成单刷怪点，并直接改成正式 `UnitConfigId=1013`，让地图进入后只生成一只 `XiaoHou`。
- **原因**：这样最小改动、最快可回归，而且明确走正式 `UnitConfig -> 静态 AI Buff` 主链路，不再被旧怪和多点刷怪干扰。
- **替代方案**：新建独立测试地图或继续沿用 `1003`；放弃原因是新建地图成本更高，而沿用 `1003` 无法验证这轮修的正式静态怪。

## 问题日志

### 2026-04-10 - Unity 批处理被项目锁阻塞
- **现象**：`Unity.exe -batchmode -projectPath D:\\05ET\\MatchTest\\ETGame -executeMethod ...` 失败，日志提示同项目已有 Unity 实例正在运行。
- **原因**：项目被现有 Unity 进程占用，批处理模式无法同时打开同一工程。
- **解决**：改为向已打开的 Unity 发出一次性迁移请求，在现有编辑器进程内完成静态 BT 资产生成与导出。

### 2026-04-16 - `XiaoHou / XiaoYing` 场景回归出现站桩空转
- **现象**：用户场景回归反馈“小怪不会动也不会打我”；静态排查后发现 `1013/1014` 的正式 `UnitConfig`、`AI Buff`、`SpellConfig` 都已落地，但战斗协程没有在施法前主动对准目标。
- **原因**：`MonsterCombatCommonHelper.TryCast` 之前缺少统一转向逻辑，而 `TargetSelectorSingle` 在服务端会拦截“目标不在施法者前方”的施法。
- **解决**：补 `MonsterCombatCommonHelper.FaceTarget(unit, target)`，并在 5 只静态怪战斗协程施法前统一调用；同时把施法失败返回码写入日志，便于后续继续排查。

### 2026-04-17 - PVEMap 仍在刷旧 `Boar(1003)`，不利于单怪回归
- **现象**：用户要求“单独在 pve 地图加载一个这个怪”；复核 `Packages/cn.etetet.map/Bundles/ECA/PVEMap.txt` 后发现地图存在两个 `SpawnMonsters` 点，且 `group_id=1003` 实际解析为旧 `UnitConfigId=1003`，也就是 `Boar`。
- **原因**：`SpawnMonstersHelper` 会优先把纯数字 `group_id` 当正式 `UnitConfigId` 解析，`PVEMap` 没有跟着新怪正式化一起切到 `1012~1016`。
- **解决**：删除多余刷怪点，并把保留点直接切到 `1013`，让 `PVEMap` 进入后只刷一只 `XiaoHou` 供局内回归。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-10 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增并推进 W3 任务16“怪物技能/Buff/AI 静态配置化” |
| 2026-04-10 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录与任务16状态推进 |
| 2026-04-10 | `Book/概念-需求-设计文档/怪物技能与AI静态配置化设计文档.md` | 新增/修改 | 建立并回写设计文档 |
| 2026-04-10 | `Book/概念-需求-设计文档/怪物技能与AI静态配置化开发日志.md` | 新增/修改 | 建立并回写开发日志 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/AI/330101.asset` ~ `330105.asset` | 新增 | 5 只怪静态 AI Buff 资产 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/*.asset` | 新增 | 影刃猫静态 Spell/Buff 资产 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130111-ScoutMonkey/*.asset` | 新增 | 侦察猴静态 Spell/Buff 资产 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130121-PhantomOwl/*.asset` | 新增 | 幻影猫头鹰静态 Spell/Buff 资产 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130131-HeavyGator/*.asset` | 新增 | 重装鳄鱼静态 Spell/Buff 资产 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130141-Baloo/*.asset` | 新增 | 巴鲁静态 Spell/Buff 资产 |
| 2026-04-10 | `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt` | 修改 | 导出新增 `130101~130143` |
| 2026-04-10 | `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 导出新增 `230101~230143`、`330101~330105` |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/MonsterRuntimeProfileHelper.cs` | 修改 | 移除运行时注册与数值覆写，只保留废弃 groupId 映射和战斗筛选辅助 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/SpawnMonstersHelper.cs` | 修改 | 正式刷怪不再调用运行时注册 |
| 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/DebugSpawnMonsterHelper.cs` | 修改 | GM 召怪不再调用运行时注册 |
| 2026-04-10 | `Packages/cn.etetet.test/Scripts/Hotfix/Test/TestHelper.cs` | 修改 | 新增按正式 `UnitConfigId` 建单位辅助 |
| 2026-04-10 | `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Rogue_Fixes_P0_Test.cs` | 修改 | 测试改为 `groupId -> UnitConfigId -> 正式配置建怪` |
| 2026-04-10 | `Book/10-项目架构/怪物AI行为说明.md` | 修改 | 回写正式怪物已改为静态 BT 资产入口 |
| 2026-04-10 | `Book/概念-需求-设计文档/怪物技能动画与特效配置说明.md` | 修改 | 回写技能动画/特效的正式静态资产配置入口 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/MonsterCombatCommonHelper.cs` | 修改 | 增加施法前统一转向与施法失败日志 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BladeCatCombatHandler.cs` | 修改 | 施法前统一朝向目标 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_ScoutMonkeyCombatHandler.cs` | 修改 | 施法前统一朝向目标 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_PhantomOwlCombatHandler.cs` | 修改 | 施法前统一朝向目标 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_HeavyGatorCombatHandler.cs` | 修改 | 施法前统一朝向目标 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BalooCombatHandler.cs` | 修改 | 施法前统一朝向目标 |
| 2026-04-16 | `Book/概念-需求-设计文档/怪物技能与AI静态配置化设计文档.md` | 修改 | 回写站桩空转修复追踪 |
| 2026-04-16 | `Book/概念-需求-设计文档/怪物技能与AI静态配置化开发日志.md` | 修改 | 记录本轮场景回归问题与修复 |
| 2026-04-16 | `Book/10-项目架构/怪物AI行为说明.md` | 修改 | 补充静态怪战斗协程施法前统一转向说明 |
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 更新 W3#16 备注，记录本轮站桩空转修复与重新待回归 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加 W3#16 本轮修复记录 |
| 2026-04-17 | `Packages/cn.etetet.map/Bundles/ECA/PVEMap.txt` | 修改 | 将 PVEMap 从双 `Boar(1003)` 刷怪点切为单只 `XiaoHou(1013)` 回归入口 |
| 2026-04-17 | `Book/概念-需求-设计文档/怪物技能与AI静态配置化设计文档.md` | 修改 | 补记 PVEMap 单怪回归入口调整 |
| 2026-04-17 | `Book/概念-需求-设计文档/怪物技能与AI静态配置化开发日志.md` | 修改 | 记录 PVEMap 单怪回归入口调整 |
| 2026-04-17 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 更新 W3#16 备注，记录 PVEMap 单怪回归入口 |
| 2026-04-17 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加 W3#16 PVEMap 单怪回归入口记录 |

## 开发总结

- **实际完成**：5 只怪的静态 `Spell/Buff/AI` BT 资产已生成并导出；正式刷怪、GM、测试入口已收口到 `UnitConfigId + 静态 BuffConfig` 主链路；本轮又补上了静态怪战斗协程施法前统一朝向目标，并把 `PVEMap` 临时切成单只 `XiaoHou(1013)` 的正式回归入口；`dotnet build ET.sln` 已通过。
- **未完成**：本轮修复后仍待你重新进场验证 `XiaoHou / XiaoYing` 等静态怪的真实索敌、走位和出手表现；技能动画和特效资源仍是占位，等待后续资源导入后在静态 BT 资产上继续补节点。
- **与设计的偏差**：迁移阶段临时使用过一次性编辑器脚本在已打开 Unity 中导出静态资产，但导出完成后已删除，不保留为正式配置来源。
- **后续待办**：先在 `PVEMap` 单怪入口回归 `XiaoHou(1013)` 的索敌与施法；如果需要切 `XiaoYing(1014)`，直接替换 `PVEMap.txt` 的 `group_id` 即可；若 `Logs/All.log` 仍出现 `[MonsterAI] cast failed`，再根据错误码继续补静态技能前置条件或目标选择。
