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
- [ ] 步骤4：执行 `dotnet build ET.sln` 并回写文档状态

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

## 问题日志

### 2026-04-16 - 预警方向与冲刺方向可能漂移
- **现象**：如果只在前摇结束时重新取当前目标位置，玩家在前摇期间移动会导致 XiaoMao 临时改向，和预警不一致。
- **原因**：现有 `TargetSelectorSingle` 只缓存目标单位，不缓存前摇开始时的目标点。
- **解决**：在前摇 Buff 中增加“锁定目标点”动作，把目标当前位置写入 `SpellTargetComponent.Position`，冲刺段从父 Buff 读取这个锁定位置。

### 2026-04-16 - 整仓编译被现有消息类型缺失阻塞
- **现象**：`dotnet build ET.sln` 失败，错误集中在 `M2C_WeaponDiscarded`、`C2M_PickupGroundItem`、`M2C_PickupGroundItem`、`C2M_DiscardWeapon`、`M2C_DiscardWeapon` 等消息类型不存在。
- **原因**：这些 `statesync` 热更脚本是工作区里已有的未收口改动，不是 XiaoMao 普攻冲刺本轮新增的节点或配置引入。
- **解决**：本轮记录为外部阻塞，保留 XiaoMao 改动结果；待这批协议/代码缺口补齐后，再重新执行整仓编译和局内回归。

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

## 开发总结

> 开发结束后填写

- **实际完成**：已完成 XiaoMao 前摇锁点、直线冲刺、自体攻击特效跟随、冲刺结束一次范围伤害，以及静态 asset / 运行时文本 / 单位挂点同步。
- **未完成**：未完成整仓通过后的局内回归；当前被工作区既有消息类型缺失阻塞。
- **与设计的偏差**：为满足“冲刺时特效挂自身”，实际额外扩展了 `BTCreateBuffEffect.FollowUnit`，比初始设计多一处通用能力改动。
- **后续待办**：补齐当前工作区缺失的协议消息类型后，重新执行 `dotnet build ET.sln`，再进 Unity/游戏内调前摇时长、冲刺距离和伤害半径。
