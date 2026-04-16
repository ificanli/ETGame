# XiaoMao普通攻击冲刺与特效配置设计文档

**创建时间**：2026-04-16  
**最后更新**：2026-04-16  
**状态**：开发中  
**关联任务**：M0.2-W3 #36  
**涉及包**：cn.etetet.statesync、cn.etetet.btnode、cn.etetet.map、cn.etetet.spell

## 需求概述

当前影刃猫（XiaoMao）的静态技能链路虽然已经接进正式 `Spell/Buff/AI` 配置，但实际效果仍接近“短前摇后单体瞬时伤害”，没有真实冲刺，也没有接入你已经挑好的 `XiaoMaoYuJing / XiaoMaoGongJi` 特效。用户当前要求把它收口为唯一一套普通攻击：

- 前摇阶段显示预警，默认 `1.5s`，并且后续可直接调参。
- 前摇阶段播放 `XiaoMaoYuJing`。
- 前摇结束后按预警方向执行一次短冲刺。
- 不做“冲刺结束点再补一段扇形攻击”。
- 改为在这次冲刺动作里，只对自身附近目标结算一次范围伤害。
- `XiaoMaoGongJi` 在冲刺阶段挂在 XiaoMao 自身身上播放。
- 范围、冲刺距离、冲刺持续时间本轮先给可调的大概值，后续继续调。

## 技术方案

### 整体思路

本轮按“最小新增节点 + 静态 BT 资产改造 + 导出文本同步”收口，不去重做整套怪物 AI：

1. 保留 `330101` 的 AI 入口与 `130101 -> 230101 -> 130102 -> 230102` 这条技能链，不改怪物主行为决策。
2. 在前摇 Buff `230101` 中锁定本次冲刺目标点：
   - 读取当前技能目标单位。
   - 立刻把 XiaoMao 朝向目标。
   - 把目标当前位置缓存进 `SpellTargetComponent.Position`，作为“预警锁定位置”。
3. 在冲刺 Buff `230102` 中新增一个最小服务端动作节点，只负责：
   - 从当前 Buff 或父 Buff 读取锁定目标点。
   - 按可配置持续时间启动一次直线移动。
   - 过程中不额外做多段命中。
4. 冲刺 Buff 自身设置一个短持续时间；在 Buff 移除时复用现有 `TargetSelectorCasterCircle + BTForeachUnit + BTDamage`，对 XiaoMao 当前附近目标结算一次范围伤害。
5. 客户端表现仍走现有 Buff 客户端节点：
   - `230101.EffectClientBuffAdd`：播前摇动作和 `XiaoMaoYuJing`。
   - `230102.EffectClientBuffAdd`：播冲刺阶段特效 `XiaoMaoGongJi`。

这样可以满足“按预警方向冲刺”的要求，同时避免把协程节点强塞进 `EffectServerBuffAdd` 破坏现有 Buff 执行框架。

### 为什么不用更重的方案

- 不新增完整“怪物冲锋技能系统”：
  当前只需要 XiaoMao 一只怪先跑通，直接加一套大而全的位移技能框架会扩大改动面。
- 不做终点扇形：
  用户已明确本轮先不做，避免多一段结算和更多 BT 节点。
- 不依赖 DTT 插件代码：
  这两个特效 prefab 已经选定，本轮直接走现有 `BTCreateBuffEffect` 挂载即可，不把插件接入当作前置门槛。

## 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTDashToSpellTargetPosition.cs` | 新增 | 服务端冲刺动作节点定义 |
| `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTDashToSpellTargetPositionHandler.cs` | 新增 | 读取锁定目标点并发起短冲刺 |
| `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTSetSpellTargetPositionFromUnit.cs` | 新增 | 前摇阶段把目标当前位置缓存到 Buff 的 `SpellTarget.Position` |
| `Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTSetSpellTargetPositionFromUnitHandler.cs` | 新增 | 目标点缓存逻辑 |
| `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTCreateBuffEffect.cs` | 修改 | 为特效节点增加 `FollowUnit`，支持冲刺特效跟随 XiaoMao |
| `Packages/cn.etetet.btnode/Scripts/HotfixView/Client/Action/BTCreateBuffEffectHandler.cs` | 修改 | 按 `FollowUnit` 决定是否挂到绑定点下 |
| `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/130101.asset` | 修改 | XiaoMao 主技能入口配置 |
| `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/130102.asset` | 修改 | XiaoMao 冲刺段子技能配置 |
| `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230101.asset` | 修改 | 前摇 Buff：锁点、播预警、前摇时长可调 |
| `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230102.asset` | 修改 | 冲刺 Buff：播攻击特效、冲刺、范围结算 |
| `Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt` | 修改 | 运行时技能导出同步 |
| `Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt` | 修改 | 运行时 Buff 导出同步 |
| `Packages/cn.etetet.map/Bundles/Units/XiaoMao.prefab` | 修改 | 为 XiaoMao 增加 `Base` 挂点，保证特效能挂在角色根部 |

## Entity/Component 设计

本轮不新增新的运行时 Entity / Component，只复用：

- `SpellTargetComponent.Position`
  - 在前摇 Buff 中缓存“本次冲刺锁定位置”。
- `Buff.GetParentBuff()`
  - 冲刺 Buff 启动时，从父 Buff 读取预警阶段缓存的位置。
- `MoveComponent`
  - 复用现有服务器权威移动同步链路，避免自造位置广播协议。

## 接口设计

本轮不新增协议。

客户端表现依然通过既有 `M2C_BuffAdd / BuffUpdate / BuffRemove` 同步 Buff 生命周期，特效和动画由客户端 BT 节点在收到 Buff 后自行播放。

## 数据结构

本轮不改 Excel / Proto，只修改：

- 静态 BT 资产 `.asset`
- 运行时导出文本 `SpellConfigCategory.txt / BuffConfigCategory.txt`

核心可调参数统一留在 Buff/Spell 配置中：

- `230101.Duration`：前摇时长，默认 `1500ms`
- `230102.Duration`：冲刺持续时间，首版默认短时可调
- 冲刺节点 `Distance` / `MinStopDistance`
- 范围选择 `Radius`
- 伤害值 `Value`

## 实现步骤

1. 补计划与文档门禁，登记 XiaoMao 普攻冲刺任务。
2. 新增前摇锁点节点与冲刺动作节点。
3. 改造 `130101 / 130102 / 230101 / 230102`，接入 `XiaoMaoYuJing / XiaoMaoGongJi`。
4. 同步运行时导出文本。
5. 执行 `dotnet build ET.sln` 验证。
6. 回写开发日志、设计文档实现追踪和周计划状态。

## 验收标准

- [ ] XiaoMao 只有一套普通攻击链路，不再是旧的单体瞬伤表现。
- [ ] 前摇默认 `1.5s`，并且可直接通过配置调整。
- [ ] 前摇期间能看到 `XiaoMaoYuJing`。
- [ ] 冲刺阶段 XiaoMao 会按预警方向直线冲刺。
- [ ] 冲刺阶段 XiaoMao 自身会播放 `XiaoMaoGongJi`。
- [ ] 冲刺结束附近目标只结算一次范围伤害，不额外补终点扇形。
- [ ] `SpellConfigCategory.txt / BuffConfigCategory.txt` 与静态资产保持一致。
- [ ] `dotnet build ET.sln` 通过。

## 关联文档

- [怪物技能与AI设计文档.md](./怪物技能与AI设计文档.md)
- [怪物技能动画与特效配置说明.md](./怪物技能动画与特效配置说明.md)

## 实现追踪

> 开发完成后回填

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 方案建档 | 2026-04-16 | `Book/概念-需求-设计文档/XiaoMao普通攻击冲刺与特效配置设计文档.md` | 按用户最新口径取消终点扇形，改为冲刺结束自身范围一次结算 |
| 节点与特效跟随能力落地 | 2026-04-16 | `Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTDashToSpellTargetPosition.cs`、`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTDashToSpellTargetPositionHandler.cs`、`Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTSetSpellTargetPositionFromUnit.cs`、`Packages/cn.etetet.btnode/Scripts/Hotfix/Server/Action/BTSetSpellTargetPositionFromUnitHandler.cs`、`Packages/cn.etetet.btnode/Scripts/Model/Share/Action/BTCreateBuffEffect.cs`、`Packages/cn.etetet.btnode/Scripts/HotfixView/Client/Action/BTCreateBuffEffectHandler.cs` | 为满足“按预警方向冲刺”和“攻击特效跟随自身”，实际比原计划多补了 `FollowUnit` 能力 |
| XiaoMao 静态配置与运行时文本同步 | 2026-04-16 | `Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/130102.asset`、`Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230101.asset`、`Packages/cn.etetet.statesync/Assets/BT/Spell/130101-BladeCat/230102.asset`、`Packages/cn.etetet.map/Bundles/Json/SpellConfigCategory.txt`、`Packages/cn.etetet.map/Bundles/Json/BuffConfigCategory.txt`、`Packages/cn.etetet.map/Bundles/Units/XiaoMao.prefab` | 运行时文本与资产已按同一方案收口；`Base` 挂点额外补到 XiaoMao prefab |
| 整仓编译验证 | 2026-04-16 | `ET.sln` | 本轮 `dotnet build ET.sln` 未通过，但阻塞点是工作区现有消息类型缺失：`M2C_WeaponDiscarded`、`C2M_PickupGroundItem`、`M2C_PickupGroundItem`、`C2M_DiscardWeapon`、`M2C_DiscardWeapon`，不是 XiaoMao 改动本身引入 |
