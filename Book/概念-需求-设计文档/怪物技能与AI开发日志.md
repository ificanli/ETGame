# 怪物技能与AI开发日志

**功能**：怪物技能与AI
**关联设计文档**：[怪物技能与AI设计文档.md](./怪物技能与AI设计文档.md)
**关联任务**：M0.2-W2 #10、M0.2-W3 #9
**开始时间**：2026-03-27
**开发者**：AI

## 开发进度

- [x] 步骤1：更新设计文档并确定首版运行时 MonsterProfile 方案
- [x] 步骤2：实现运行时怪物 Profile、技能/Buff 注册与刷怪映射
- [x] 步骤3：实现 5 只怪物独立 AI 与首版战斗行为
- [x] 步骤4：编译验证并回写文档

## 决策记录

### 2026-03-27 - 首版不改 Luban UnitConfig
- **背景**：当前正式怪物资源和 `UnitConfig` 不完整，但 5 只怪的技能和 AI 需要先落地。
- **方案**：使用运行时 `MonsterProfile` 映射 `groupId`，基于现有占位 `UnitConfig` 创建怪物，再覆写数值、AI 和技能配置。
- **原因**：不阻塞战斗逻辑验证，也避免手改 Luban 生成代码和 Excel 链路。
- **替代方案**：直接补 Excel / Luban 正式怪物配置；放弃原因是当前资源和表结构仍在变化，投入产出比低。

### 2026-03-27 - 预警收进技能 Buff 链
- **背景**：现有 `AI_MonsterZhuiJi.PreCastSpellId` 更适合协程启动阶段，不适合作为每次出手前摇。
- **方案**：每次攻击统一走“主技能 Buff 前摇 -> Buff 结束时结算伤害 / 子技能”。
- **原因**：不影响现有机器人和旧怪物 AI，行为更稳定，也方便后续替换表现资源。
- **替代方案**：修改 `AI_MonsterZhuiJi` 的公共逻辑；放弃原因是会影响已有链路。

### 2026-03-27 - MonsterProfile helper 改为纯静态分发
- **背景**：首版 `MonsterRuntimeProfileHelper` 使用普通 profile 类、`init` 属性和静态字典缓存，在 `ET.Hotfix` 中触发 `ET0004 / ET0005 / ET0015 / CS0518`。
- **方案**：删除普通 `MonsterRuntimeProfile` 类和静态状态，改为 `TryResolveUnitConfigId` / `ApplyProfile` / `EnsureGroupRegistered` 三个纯静态入口，按 `groupId` 分发并幂等注册 `SpellConfig` / `BuffConfig`。
- **原因**：符合 ET 分析器约束，也保留了运行时注册正式怪物技能与 AI 的能力。
- **替代方案**：继续在 Hotfix 中保留 profile 数据类或静态缓存；放弃原因是分析器不接受，继续堆补丁收益很低。

## 问题日志

### 2026-03-27 - 刷怪 groupId 当前直接当 UnitConfigId 使用
- **现象**：现有 `SpawnMonstersHelper` 只支持数字 `groupId`，无法直接挂新怪物档案名。
- **原因**：历史实现为临时方案，尚未引入真正的怪物组配置。
- **解决**：本轮改成“优先 MonsterProfile，回退数字 UnitConfigId”的兼容模式。

### 2026-03-27 - ET 分析器阻塞 MonsterProfile helper 编译
- **现象**：`MonsterRuntimeProfileHelper.cs` 报 `ET0004 / ET0005 / ET0015`，并伴随 `CS0518 IsExternalInit`。
- **原因**：Hotfix 程序集中不允许当前这种普通类 + `init` + 静态字段缓存的组合写法。
- **解决**：整体重构为纯静态 helper，不再保存 profile 对象状态。

### 2026-03-27 - Baloo AI 命中 await 后 Entity 安全检查
- **现象**：`AI_BalooCombatHandler.cs` 在 `await` 后继续使用 await 前缓存的 `NumericComponent`，触发 `ETAE001`。
- **原因**：`NumericComponent` 也是 Entity，不能跨 `await` 继续持有。
- **解决**：仅在 `await` 前缓存纯值 `baseSpeed`，循环里每次通过 `unitRef` 重新取 `unit` 后再获取 `NumericComponent`。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-03-27 | `Book/概念-需求-设计文档/怪物技能与AI设计文档.md` | 修改 | 更新首版技术方案，切换到运行时 MonsterProfile |
| 2026-03-27 | `Book/概念-需求-设计文档/怪物技能与AI开发日志.md` | 新增 | 创建怪物功能开发日志 |
| 2026-03-27 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/MonsterRuntimeProfileHelper.cs` | 新增/重构 | 改为纯静态 `groupId` 分发，运行时注册 5 只怪的技能、Buff 与 AI |
| 2026-03-27 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/SpawnMonstersHelper.cs` | 修改 | 刷怪时优先解析运行时怪物档案，失败后回退旧数字 `groupId` 逻辑 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/AI_BladeCatCombat.cs` | 新增 | 影刃猫独立 AI 节点定义 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/AI_ScoutMonkeyCombat.cs` | 新增 | 侦察猴独立 AI 节点定义 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/AI_PhantomOwlCombat.cs` | 新增 | 幻影猫头鹰独立 AI 节点定义 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/AI_HeavyGatorCombat.cs` | 新增 | 重装鳄鱼独立 AI 节点定义 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/AI_BalooCombat.cs` | 新增 | 巴鲁独立 AI 节点定义 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/MonsterCombatCommonHelper.cs` | 新增 | 抽取通用战斗目标刷新、施法距离维持、施法入口逻辑 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BladeCatCombatHandler.cs` | 新增 | 影刃猫战斗协程实现 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_ScoutMonkeyCombatHandler.cs` | 新增 | 侦察猴战斗协程实现 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_PhantomOwlCombatHandler.cs` | 新增 | 幻影猫头鹰战斗协程实现 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_HeavyGatorCombatHandler.cs` | 新增 | 重装鳄鱼战斗协程实现 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BalooCombatHandler.cs` | 新增/修改 | 巴鲁战斗协程与狂暴逻辑，补 `await` 后 Entity 安全访问 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTDamageSpellTargetCircle.cs` | 新增 | 延迟目标点圆形伤害节点定义 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTDamageSpellTargetCircleHandler.cs` | 新增 | 延迟目标点圆形伤害逻辑 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Root/TargetSelectorSector.cs` | 修改 | 新增扇形目标选择配置节点 |
| 2026-03-27 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Root/BTTargetSelectorSectorHandler.cs` | 新增 | 服务端扇形选目标逻辑 |
| 2026-03-27 | `ET.Model.csproj` | 修改 | 纳入新增模型文件编译 |
| 2026-03-27 | `ET.Hotfix.csproj` | 修改 | 纳入新增 Hotfix 文件编译 |
| 2026-03-28 | `Book/概念-需求-设计文档/怪物技能动画与特效配置说明.md` | 新增 | 补充怪物技能表现配置说明，统一动画、指示器、角色特效、地面特效、飞行弹道的配置入口 |

## 开发总结

- **实际完成**：5 只怪的首版技能与独立 AI 已落地；刷怪支持 `groupId -> 运行时 MonsterProfile -> 占位 UnitConfig + 数值覆写 + AI/Buff/Spell 注册`；新增扇形选目标与延迟圆形伤害两个通用节点；`dotnet build ET.sln` 已通过。
- **未完成**：未做场景内手工回归，怪物表现资源仍是占位；鳄鱼持续减速区、背后弱点等增强机制未进入本轮实装。
- **与设计的偏差**：MonsterProfile 最终没有保留为普通数据对象，而是改成纯静态分发 helper；这是为了满足 ET Hotfix 分析器限制。
- **后续待办**：进场景验证 5 只怪的出招频率、距离感、命中判定和占位表现，再决定是否补减速区、真实弹道/位移和 Boss 演出。
