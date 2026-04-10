# ECA 框架使用指南 - 场景配置流程

> 本文档只描述当前工程里已经接上的配置流程。
>
> 具体参数、默认值、节点能力边界，请配合阅读：
> `Book/07-ECA框架/ECA2/ECA场景配置参数手册.md`

## 1. 当前有效入口

当前场景配置入口只有 `ECAPointMarker`，不再使用旧版 `ECAConfigAsset` / `Config` 字段手工关联方案。

每个点位在 Unity 场景里直接配置：
- `ConfigId`
- `Type`
- `Params`
- 可选 `FlowGraph`
- `ShowRange`
- `RangeColor`

`ECAPointMarker` 会按 `Type` 自动补齐一组模板参数：
- 撤离点：补 `evacuation_duration_ms`、`lobby_map_name`
- 出生点：补 `team_id`
- 门/钥匙门：补导航阻挡和门交互参数

## 2. 场景配置步骤

### 第一步：在 Unity 场景中创建点位

1. 创建一个空物体，例如 `EvacuationPoint_001`
2. 添加 `ECAPointMarker`
3. 配置 `ConfigId`
4. 选择 `Type`
5. 在 `Params` 里填写当前点位需要的参数
6. 如果这个点位要走 `FlowGraph`，再关联 `FlowGraph`

注意：
- `ConfigId` 必须在同一张图内唯一
- 不要再创建 `ScriptableObject.CreateInstance<ECAConfig>()`
- 不要再找旧文档里的 `Config` 字段，当前组件没有这个运行入口

### 第二步：导出场景配置

在 Unity 菜单执行：

```text
ET/ECA/Export ECA Config
```

导出结果：
- 路径：`Packages/cn.etetet.map/Bundles/ECA/{SceneName}.txt`
- 内容：当前场景所有 `ECAPointMarker` 收集后的 `ECAConfig` JSON

导出前校验：
- `ConfigId` 不能重复
- `FlowGraph` 里的节点必须通过编辑器校验

### 第三步：确认服务端加载链路

当前正式链路已经接好，不需要再手工补代码。

地图初始化时：
- [`FiberInit_Map.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.map/Scripts/Hotfix/Server/FiberInit_Map.cs#L45) 会自动调用 `ECALoader.LoadFromFile(root, mapName)`
- [`ECALoader.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECALoader.cs#L13) 会从 `Packages/cn.etetet.map/Bundles/ECA/{mapName}.txt` 加载

这里最容易出错的是文件名：
- Unity 导出名来自当前场景名
- 服务端加载名来自 `root.Name.GetSceneConfigName()`
- 两边必须一致

### 第四步：确认运行时触发链路

当前正式运行时链路如下：

范围检测：
- 不是挂在 `MoveTimer`
- 由 [`ECACheckRangeTimer.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ECACheckRangeTimer.cs#L3) 按全图统一间隔轮询

进入/离开范围：
- 由 [`ECAHelper.CheckPlayerInRange`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAHelper.cs#L17) 触发
- 最终走 [`ECAPointComponentSystem`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAPointComponentSystem.cs#L41)

玩家交互：
- 客户端发 `C2M_ECAInteract`
- 服务端由 [`C2M_ECAInteractHandler.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/C2M_ECAInteractHandler.cs#L3) 校验距离后触发 `OnPlayerInteractAsync`

### 第五步：理解 FlowGraph 与 fallback 的关系

规则是：
- 点位有 `FlowGraph` 时，优先走 FlowGraph
- 点位没有 `FlowGraph` 时，走点位类型 fallback
- 当前真正仍在用的 fallback，主要是“撤离点旧逻辑”

撤离点有一个特殊兼容分支：
- 如果撤离点挂了 `FlowGraph`
- 但图里没有 `StartEvacCountdown`
- 那么进入范围后仍然会 fallback 到旧撤离逻辑

对应代码：
- [`ECAPointComponentSystem.ShouldFallbackToLegacyEvacuation`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAPointComponentSystem.cs#L302)

## 3. 推荐配置模式

### 3.1 纯 fallback 撤离点

适合：
- 只要“进圈开始撤离，离圈取消，完成后回大厅”的简单撤离点

点位参数示例：

```text
interact_range=5
evacuation_duration_ms=15000
lobby_map_name=Home
```

说明：
- 不配置 `FlowGraph`
- 玩家进入范围后直接启动撤离

### 3.2 FlowGraph 容器

适合：
- 搜索、掉落、开容器 UI 这类有明确流程的点位

常见流程：
- `OnPlayerEnterRange -> ShowInteractButton`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> ShowSearchUI -> StartSearchTimer`
- `OnTimerElapsed(timer_id=...) -> GenerateContainerLoot -> OpenContainerUI`

### 3.3 门/钥匙门

推荐流程：
- `OnPlayerEnterRange -> RefreshDoorInteractHint`
- `OnPlayerLeaveRange -> HideInteractButton`
- `OnPlayerInteract -> ToggleDoor`

说明：
- 钥匙门是否可交互、显示什么按钮文字，运行时会结合点位参数自动判断
- 导航阻挡会根据点位状态自动刷新，不要再手工写额外门阻挡逻辑

## 4. 不要再做的旧操作

以下做法都是旧文档遗留，当前不要再照着做：

- 不要创建 `ECAConfigAsset`
- 不要手写 `ScriptableObject.CreateInstance<ECAConfig>()`
- 不要手工在地图初始化里补 `ECALoader.LoadECAPoints(mapScene, mapName)`
- 不要把 `ECAHelper.CheckPlayerInRange(unit)` 塞回 `MoveTimer`
- 不要把 `StartEvacCountdown -> TransferToLobby` 当成“延迟传送”流程

最后一点尤其重要：
- `StartEvacCountdown` 只是启动撤离组件
- `TransferToLobby` 是立即执行的动作
- 两者直接串起来会立刻跳图，不会等倒计时结束

## 5. 常见问题

### Q1：为什么导出了配置，但进图后没有生效？

优先检查：
- 导出文件名是否和服务端 `mapName` 一致
- 当前地图是否真的会走 `FiberInit_Map`
- 导出的 `ConfigId` 是否重复

### Q2：为什么门状态变了，但路网没变化？

优先检查：
- 点位是否配置了 `nav_block_enabled`
- `nav_block_states` 是否覆盖了当前状态
- 当前地图是否在初始化后执行了 `ECAPointNavBlockHelper.Rebuild`

### Q3：为什么 FlowGraph 里写了 `OnMapLoaded`，进图没触发？

截至当前代码：
- `OnMapLoaded` 已经出现在枚举、编辑器模板和导出数据里
- 但没有看到地图加载完成后统一转发到每个点位 `FlowGraph` 的正式运行时接线

结论：
- 当前正式关卡配置不要依赖 `OnMapLoaded`
- 如果后面补了接线，再单独更新文档

## 6. 相关代码入口

- 点位组件：[`ECAPointMarker.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs)
- 导出入口：[`ExportECAConfigEditor.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Editor/ECAEditor/ExportECAConfigEditor.cs)
- 场景收集：[`ECASceneHelper.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/ModelView/Client/ECASceneHelper.cs)
- 服务端加载：[`ECALoader.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECALoader.cs)
- 范围检测：[`ECAHelper.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAHelper.cs)
- 范围轮询定时器：[`ECACheckRangeTimer.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ECACheckRangeTimer.cs)
- FlowGraph 执行：[`ECAFlowGraphRunner.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.eca/Scripts/Hotfix/Server/ECAFlowGraphRunner.cs)
- Action 实现：[`ECAFlowActionInvokeHandler.cs`](D:/05ET/MatchTest/ETGame/Packages/cn.etetet.ecanode/Scripts/Hotfix/Server/ECAFlowActionInvokeHandler.cs)

---

最后更新：2026-03-27
状态：已按当前实现重写
