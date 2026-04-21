# XiaoMao普通攻击冲刺与特效配置开发日志

**功能**：XiaoMao普通攻击冲刺与特效配置  
**关联设计文档**：[XiaoMao普通攻击冲刺与特效配置设计文档.md](./XiaoMao普通攻击冲刺与特效配置设计文档.md)  
**关联任务**：M0.2-W3 #36  
**开始时间**：2026-04-16  
**开发者**：AI

## 开发进度

- [x] 步骤1：补周计划、版本计划、设计文档和开发日志门禁
- [x] 步骤2：新增前摇锁点节点与冲刺动作节点
- [x] 步骤3：改造 XiaoMao 静态 `Spell/Buff` 资产并同步运行时导出文本
- [x] 步骤4：执行 `dotnet build ET.sln` 并回写文档状态

## 决策记录

### 2026-04-16 - 冲刺段不使用 ABTCoroutineHandler
- **背景**：XiaoMao 的冲刺逻辑需要在 `EffectServerBuffAdd` 阶段启动，但又要在冲刺结束后再结算一次范围伤害。
- **方案**：新增一个普通服务端 BT 动作，在 `BuffAdd` 时异步启动冲刺；Buff 自己用短持续时间表示“冲刺过程”，在 `BuffRemove` 时再结算一次范围伤害。
- **原因**：`ABTCoroutineHandler` 依赖 `BuffTickComponent`，不适合直接挂在 `EffectServerBuffAdd`；两段式更符合现有 Buff 框架。
- **替代方案**：直接新增一个大而全的“冲刺并伤害”协程节点。放弃原因是职责过重，不利于后续复用和调参。

### 2026-04-16 - 扩展 BTCreateBuffEffect 支持跟随单位
- **背景**：原有 `BTCreateBuffEffect` 会把特效实例化到绑定点位置，但不挂到绑定点下，冲刺时特效会留在起点。
- **方案**：在节点上增加 `FollowUnit` 布尔参数；仅在需要跟随时，以子节点方式挂到绑定点下。
- **原因**：这样能最小影响现有全部特效配置，只有显式开启 `FollowUnit` 的技能会改变表现。
- **替代方案**：新建一个额外的“Follow 版特效节点”。放弃原因是会增加重复节点和维护成本。

### 2026-04-21 - BladeCat 恢复期从施法结束后开始计算
- **背景**：用户要求小猫行为节奏改成“放一次技能，走路走 3 秒，再放技能”。如果简单在 `TryCast` 成功那一刻起算 `3000ms`，会被前摇与冲刺动画吃掉，实际可感知空窗明显不足。
- **方案**：在 `AI_BladeCatCombatHandler` 中检测“当前施法存活 -> 当前施法结束”的状态边沿，只在施法真正结束后写入 `nextCastReadyTime = now + PostCastRecoverMs`。
- **原因**：这样恢复时间与技能持续时间解耦，用户调 `230101/230102.Duration` 时，不会把 3 秒恢复期偷偷吃掉。
- **替代方案**：在施法开始时直接加固定 CD。放弃原因是表现会变成“技能总周期 3 秒”，而不是“技能后恢复 3 秒”。

### 2026-04-21 - 特效特殊修正收口到 BTCreateBuffEffect 通用参数
- **背景**：用户最新回归同时指出两类表现问题：`XiaoMaoYuJing` 的预警参数需要代码驱动，`XiaoMaoGongJi` 的冲刺特效轨迹和实际 dash 不重合。
- **方案**：继续扩展 `BTCreateBuffEffect`，增加局部位移/旋转修正、`LineRegion` 长度同步和按时长填充驱动四类通用参数；`230101/230102` 只通过 BT 资产配置启用，不在代码里写死 XiaoMao 特效名。
- **原因**：后续别的怪如果也要用线型预警或需要特效轨迹修正，可以直接复用这套节点字段，不会再散落成一堆单怪 if/else。
- **替代方案**：单独给 XiaoMao 写专用特效脚本或在 prefab 上直接手改固定参数。放弃原因是前者硬编码，后者无法根据目标点与前摇时长实时同步。

## 问题日志

### 2026-04-16 - 预警方向与冲刺方向可能漂移
- **现象**：如果只在前摇结束时重新取当前目标位置，玩家在前摇期间移动会导致 XiaoMao 临时改向，和预警不一致。
- **原因**：现有 `TargetSelectorSingle` 只缓存目标单位，不缓存前摇开始时的目标点。
- **解决**：在前摇 Buff 中增加“锁定目标点”动作，把目标当前位置写入 `SpellTargetComponent.Position`，冲刺段从父 Buff 读取这个锁定位置。

### 2026-04-16 - 整仓编译被现有消息类型缺失阻塞
- **现象**：`dotnet build ET.sln` 失败，错误集中在 `M2C_WeaponDiscarded`、`C2M_PickupGroundItem`、`M2C_PickupGroundItem`、`C2M_DiscardWeapon`、`M2C_DiscardWeapon` 等消息类型不存在。
- **原因**：这些 `statesync` 热更脚本是工作区里已有的未收口改动，不是 XiaoMao 普攻冲刺本轮新增的节点或配置引入。
- **解决**：本轮记录为外部阻塞，保留 XiaoMao 改动结果；待这批协议/代码缺口补齐后，再重新执行整仓编译和局内回归。

### 2026-04-21 - 地图摆点刷怪进入战斗但永远不出手
- **现象**：地图摆点刷出的 XiaoMao 现场表现为“不会打人”，并且完全没有前摇、冲刺和特效。
- **原因**：静态怪共用的 `MonsterCombatCommonHelper.HasCastingSpell()` 写成了 `unit?.GetComponent<SpellComponent>()?.Current != null`。由于 `Current` 是值类型 `EntityRef<Buff>`，空条件运算符会把结果提升成可空结构体，只要 `SpellComponent` 存在就恒为“有值”，导致 AI 每个 think tick 都误判“正在施法”，永远跳过 `TryCast`。
- **解决**：改为显式取 `SpellComponent.Current` 并还原成真实 `Buff` 判空；同时补齐怪物施法成功/失败日志，输出 `ret` 含义、目标、距离、边缘距离和仇恨数量，便于你下一轮局内回归直接看日志。

### 2026-04-21 - 仇恨组件存在 childId 与最大仇恨返回错误
- **现象**：`ThreatComponentSystem.AddThreat()` 首次建条目时没有用 `unit.Id` 作为 childId，而 `GetMaxThreat()` 也可能返回“最后遍历到的条目”而不是“最大仇恨条目”。
- **原因**：仇恨条目创建和读取口径不一致，且最大/最小值选择逻辑遗漏了独立的 best/min 引用。
- **解决**：改为 `AddChildWithId<ThreatInfo>(unit.Id)`，并修正 `GetMaxThreat()/GetMinThreat()` 的返回逻辑，避免地图摆点刷怪在后续多目标或重复加仇恨场景里继续出现不稳定目标选择。

### 2026-04-21 - 当前工作区缺少局内实战日志，追加小猫战斗循环前置日志
- **现象**：用户反馈“怪物会动了，但还是不会放技能”，但当前工作区 `Logs/All.log` 与小时日志里只有测试和 ExcelExporter 输出，没有任何这轮地图内 XiaoMao 战斗日志，也没有 `[MonsterAI] cast failed/success`。
- **原因**：真实服务端可能不是从当前仓库根目录启动，或者这轮复现后日志尚未回到当前 `Logs` 目录，导致现有日志无法证明问题卡在 `TryCast` 内还是卡在它之前。
- **解决**：在 `AI_BladeCatCombatHandler` 里补三段前置日志，分别覆盖“刷新目标失败但已有仇恨”“仍在维持施法距离”“仍被判定正在施法”，这样你下一轮局内回归即使还没有 `cast failed/success`，也能直接看出协程卡在 `TryCast` 之前的哪一步。

### 2026-04-21 - Unity 日志确认技能已施放，真正缺口是预警特效资源未进包
- **现象**：改看 Unity `Editor.log` 后，能看到 XiaoMao 在 `SDCMap` 中多次 `[MonsterAI] cast success`、`Buff Create: 230101`、`Buff Create: 230102`，说明服务端施法链路已通；但客户端同时报 `Failed to mapping location to asset path : XiaoMaoYuJing` 与 `Failed to load asset ! The location is invalid : XiaoMaoYuJing`。
- **原因**：`Packages/cn.etetet.statesync/Settings/AssetBundleCollectorSetting.asset` 的 `Effects` 组只收 `Packages/cn.etetet.statesync/Bundles/Effect`，而 XiaoMao 的 `XiaoMaoYuJing.prefab / XiaoMaoGongJi.prefab` 实际放在 `Packages/cn.etetet.statesync/Assets/GameRes/Effect`，运行时包清单里自然没有这两个地址。
- **解决**：在 `Effects` collector 中补收 `Packages/cn.etetet.statesync/Assets/GameRes/Effect`，继续沿用 `AddressByFileName`，让 `XiaoMaoYuJing / XiaoMaoGongJi` 能按现有 BT 配置名直接映射。

### 2026-04-21 - XiaoMaoYuJing prefab 还存在失效材质引用
- **现象**：在补完 Effects collector 后继续追查粉色预警，发现 `XiaoMaoYuJing.prefab` 的两个 `MeshRenderer` 仍分别引用 `1168ea4234d80da4aa604b1b67a4f61d` 与 `387fb2d607dbace47897b634f1608ea6`，但这两个 GUID 在整个仓库中都不存在。
- **原因**：预警 prefab 自身挂了失效材质引用，哪怕 prefab 地址映射恢复，运行时也会因为材质丢失继续表现为粉色。
- **解决**：把两个 renderer 分别改挂仓库内已存在且已用于指示器系统的 `indicate_angle90.mat` 与 `indicate_curve001.mat`，让预警 prefab 不再依赖不存在的材质资源。

### 2026-04-21 - 预警参数确实需要运行时代码驱动
- **现象**：继续检查 `XiaoMaoYuJing.prefab` 后确认它挂的是 DTT `LineRegion`，但业务层此前只会实例化 prefab，没有任何代码去同步 `Length` 或推进 `FillProgress`，所以预警只能吃 prefab 固定值。
- **原因**：当前 `BTCreateBuffEffect` 只负责加载、挂点和销毁，没有携带任何“按目标点/时长修正特效”的能力。
- **解决**：在 `BTCreateBuffEffect` 增加 `LineRegion` 通用配置；客户端先反射找到 `LineRegion` 组件，再按 `SpellTargetComponent.Position` 运行时同步长度，并通过 `LineRegionFillProgressDriver` 按前摇时长从 `0 -> 1` 推进填充。

### 2026-04-21 - 冲刺特效 prefab 自身朝向导致轨迹错位
- **现象**：`XiaoMaoGongJi.prefab` 的主要粒子和发光条带大量分布在负 `Z` 方向，直接挂到小猫 `Base` 挂点后会表现成轨迹朝后拖，与真实冲刺方向不重合。
- **原因**：当前特效节点没有任何局部旋转修正能力，只能原样实例化 prefab。
- **解决**：给 `BTCreateBuffEffect` 增加局部旋转字段，并在 `230102` 上配置 `LocalEulerAnglesOffsetY=180`，先把 prefab 朝向翻正；如果你下一轮回归还有轻微偏差，再继续微调局部偏移字段即可。

### 2026-04-21 - XiaoMao 预警特效被地面盖住
- **现象**：用户最新局内回归反馈 `XiaoMaoYuJing` 预警太贴地，局部会被地表遮住，影响可读性。
- **原因**：`230101` 当前只驱动了线段长度和填充，没有额外离地偏移，特效实例仍贴着 `Base` 挂点原点。
- **解决**：直接在 `230101` 的 `BTCreateBuffEffect` 上增加 `LocalPositionOffsetY=0.08`，只抬高 XiaoMao 技能里的这份预警实例，不改 prefab 本体和其他复用场景。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增 W3#36 XiaoMao 普攻冲刺任务 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 记录任务36变更 |
| 2026-04-16 | `Book/概念-需求-设计文档/XiaoMao普通攻击冲刺与特效配置设计文档.md` | 新增 | 建立本次实现设计方案 |
| 2026-04-16 | `Book/概念-需求-设计文档/XiaoMao普通攻击冲刺与特效配置开发日志.md` | 新增 | 建立本次开发日志 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTCreateBuffEffect.cs` | 修改 | 为 Buff 特效节点增加 `FollowUnit` 参数 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/HotfixView/Client/Action/BTCreateBuffEffectHandler.cs` | 修改 | 按 `FollowUnit` 决定是否跟随绑定点 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTSetSpellTargetPositionFromUnit.cs` | 新增 | 前摇阶段锁定目标点 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTSetSpellTargetPositionFromUnitHandler.cs` | 新增 | 目标点缓存处理器 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTDashToSpellTargetPosition.cs` | 新增 | 冲刺动作节点定义 |
| 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTDashToSpellTargetPositionHandler.cs` | 新增 | 服务器冲刺处理器 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/130102.asset` | 修改 | 改为冲刺执行段入口，目标选择改为施法者自身 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230101.asset` | 修改 | 前摇 Buff 增加锁点、预警特效和客户端动作 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230102.asset` | 修改 | 冲刺 Buff 增加 dash、自体攻击特效和结束范围结算 |
| 2026-04-16 | `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt` | 修改 | 同步 XiaoMao 运行时 Spell 配置 |
| 2026-04-16 | `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 同步 XiaoMao 运行时 Buff 配置 |
| 2026-04-16 | `Packages/cn.etetet.map/Bundles/Units/XiaoMao.prefab` | 修改 | 为 XiaoMao 补 `Base` 挂点，供两个特效挂载 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/MonsterCombatCommonHelper.cs` | 修改 | 修复静态怪 `HasCastingSpell` 误判，补充怪物施法成功/失败上下文日志 |
| 2026-04-21 | `Packages/cn.etetet.map/Scripts/Hotfix/Server/Unit/ThreatComponentSystem.cs` | 修改 | 修复仇恨条目 childId 与最大/最小仇恨返回错误 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BladeCatCombatHandler.cs` | 修改 | 补 XiaoMao 战斗循环前置日志，覆盖刷新目标失败/控距/误判当前施法三段卡点 |
| 2026-04-21 | `Packages/cn.etetet.statesync/Settings/AssetBundleCollectorSetting.asset` | 修改 | 补收 `Assets/GameRes/Effect`，修复 XiaoMao 预警/攻击特效运行时地址映射缺失 |
| 2026-04-21 | `Packages/cn.etetet.statesync/Assets/GameRes/Effect/XiaoMaoYuJing.prefab` | 修改 | 把两个失效材质 GUID 改挂到仓库内真实存在的指示器材质，修复预警粉色根因 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/Model/Share/AI/AI_BladeCatCombat.cs` | 修改 | 新增 `PostCastRecoverMs` 配置字段 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/AI/AI_BladeCatCombatHandler.cs` | 修改 | 新增“施法结束后恢复期”门控与恢复日志，避免小猫连续施法 |
| 2026-04-21 | `Packages/cn.etetet.statesync/Assets/BT/AI/330101.asset` | 修改 | 将小猫 AI 的 `PostCastRecoverMs` 配为 `3000` |
| 2026-04-21 | `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 同步运行时 `PostCastRecoverMs=3000`，保持导出文本与静态资产一致 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTCreateBuffEffect.cs` | 修改 | 为 Buff 特效节点增加局部偏移/旋转和 `LineRegion` 运行时驱动配置 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/HotfixView/Client/Action/BTCreateBuffEffectHandler.cs` | 修改 | 改为在创建特效后统一应用局部修正，并通过反射驱动 `LineRegion` 长度同步 |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/ModelView/Client/Action/LineRegionFillProgressDriver.cs` | 新增 | 新增 `LineRegion` 填充进度驱动组件，并按 ET 分层要求放到 `ModelView` |
| 2026-04-21 | `Packages/cn.etetet.btnode/Scripts/HotfixView/Client/Action/LineRegionFillProgressDriver.cs` | 新增 | 补空壳占位文件，兼容当前显式 `ET.HotfixView.csproj` 的旧路径引用 |
| 2026-04-21 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230101.asset` | 修改 | 给预警特效打开 `LineRegion` 长度同步和按前摇时长填充 |
| 2026-04-21 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230102.asset` | 修改 | 给冲刺特效增加 `LocalEulerAnglesOffsetY=180` 修正 |
| 2026-04-21 | `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 同步 XiaoMao 两个 Buff 的新增通用特效字段，保持运行时导出一致 |
| 2026-04-21 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230101.asset` | 修改 | 给 XiaoMao 预警特效增加 `LocalPositionOffsetY=0.08`，抬高离地高度避免被地面盖住 |
| 2026-04-21 | `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 同步 XiaoMao 预警离地偏移到运行时导出文本 |
| 2026-04-21 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 将 W3#36 推进到未回归，并记录地图摆点刷怪不攻击的根因与修复 |
| 2026-04-21 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加 W3#36 回归修复记录 |
| 2026-04-21 | `Book/概念-需求-设计文档/XiaoMao普通攻击冲刺与特效配置设计文档.md` | 修改 | 回写地图摆点刷怪不攻击的回归修复追踪 |
| 2026-04-21 | `Book/概念-需求-设计文档/XiaoMao普通攻击冲刺与特效配置开发日志.md` | 修改 | 记录主因、附带修复点与编译结果 |

## 开发总结

> 开发结束后填写

- **实际完成**：已完成 XiaoMao 前摇锁点、直线冲刺、自体攻击特效跟随、冲刺结束一次范围伤害，以及本轮地图摆点刷怪不攻击回归修复：修正静态怪 `HasCastingSpell` 误判、补足施法日志、修复仇恨条目 childId/最大仇恨返回错误；在当前工作区缺少局内实战日志的前提下，又额外补了小猫战斗循环前置日志；随后进一步从 Unity `Editor.log` 确认技能实际已施放，并补收 `Assets/GameRes/Effect` 进 `Effects` collector；继续下钻后又修掉 `XiaoMaoYuJing.prefab` 两个失效材质引用，并新增 `PostCastRecoverMs=3000`，把小猫改成“技能完整结束后恢复 3 秒再允许下一次施法”。这一轮又继续把 `BTCreateBuffEffect` 扩成通用表现修正层：`230101` 现在会在客户端运行时按目标点同步 `LineRegion` 长度、按前摇时长推进填充，并额外上抬 `0.08m` 避免被地面盖住；`230102` 则通过局部 `Y=180` 旋转修正，把冲刺特效轨迹翻到正确方向；`dotnet build ET.sln` 已重新通过。
- **未完成**：尚未完成你这一轮局内人工回归。
- **与设计的偏差**：为满足“冲刺时特效挂自身”，实际额外扩展了 `BTCreateBuffEffect.FollowUnit`；本轮回归修复里又顺带收口了通用静态怪施法状态判断、仇恨稳定性、小猫战斗循环前置日志，以及 `Effects` 资源收集口径，这些都属于运行时保护性修正，不改变设计目标。
- **后续待办**：重新构建客户端资源后，你进游戏验证地图摆点刷出的 XiaoMao 是否已能正常前摇、冲刺、播特效，并确认相邻两次 `cast success` 之间是否稳定多出约 `3000ms` 的恢复空窗；若仍异常，先看 Unity `Editor.log` 是否还报 `Failed to mapping location to asset path : XiaoMaoYuJing`，再结合 `[MonsterAI] bladecat enter recover / refresh target blocked / maintain range / skip cast because current spell alive / cast failed / cast success` 判断卡点。
