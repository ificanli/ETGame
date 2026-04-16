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

## 问题日志

### 2026-04-10 - Unity 批处理被项目锁阻塞
- **现象**：`Unity.exe -batchmode -projectPath D:\\05ET\\MatchTest\\ETGame -executeMethod ...` 失败，日志提示同项目已有 Unity 实例正在运行。
- **原因**：项目被现有 Unity 进程占用，批处理模式无法同时打开同一工程。
- **解决**：改为向已打开的 Unity 发出一次性迁移请求，在现有编辑器进程内完成静态 BT 资产生成与导出。

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

## 开发总结

- **实际完成**：5 只怪的静态 `Spell/Buff/AI` BT 资产已生成并导出；正式刷怪、GM、测试入口已收口到 `UnitConfigId + 静态 BuffConfig` 主链路；`dotnet build ET.sln` 与 `Test_Rogue_Fixes_P0_Test` 已通过。
- **未完成**：未做地图摆点后的场景内人工回归；技能动画和特效资源仍是占位，等待后续资源导入后在静态 BT 资产上继续补节点。
- **与设计的偏差**：迁移阶段临时使用过一次性编辑器脚本在已打开 Unity 中导出静态资产，但导出完成后已删除，不保留为正式配置来源。
- **后续待办**：后续地图 ECA 直接填正式 `UnitConfigId(1012~1016)`；如要补不同技能动画/特效，直接改对应 `BuffScriptableObject` 的客户端节点。
