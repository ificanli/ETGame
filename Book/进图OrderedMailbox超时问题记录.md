# 进图 Ordered Mailbox 超时问题记录

## 基本信息

- 记录时间：2026-03-15
- 当前状态：未修复，已完成代码链路分析
- 问题类型：进图阶段 `OrderedMessage` 邮箱持锁超时

## 现象

进图时出现如下报错：

```text
.\Packages\cn.etetet.actorlocation\Scripts\Hotfix\Server\MailBoxType_OrderedMessageHandler.cs:24
Coroutine lock timeout after 5000ms, type: 1001, key: 321188793417758, level: 1
```

用户侧感知通常可能表现为：

- 进图卡住或明显变慢
- 转图后状态异常
- 后续伴随一串随机错误

## 当前结论

这条日志不是“锁没拿到”，而是“已经拿到锁，但 5 秒内没有执行完”。

关键点如下：

1. `type: 1001` 对应 `CoroutineLockType.Mailbox`
2. `key: 321188793417758` 对应具体的 `Unit/Actor Id`
3. `level: 1` 表示当前是第一层运行中的持锁协程
4. 超时计时是在锁授予之后才开始，不是排队等待时开始
5. 超时后框架会主动释放这把锁，不只是打印日志

因此，这个问题的本质是：

> 某个 Unit 的 ordered mailbox 消息，在进图链路里执行时间超过了 5000ms。

## 风险说明

这个问题可以暂时不修，但风险比较高。

因为超时后锁会被强制释放，后果不是单纯“慢一点”，而是：

- 同一个 `Unit` 的消息串行保证失效
- 前一条消息还没处理完，后一条消息就可能并发进入
- 可能出现状态打架、转图半成功、旧场景和新场景同时操作同一实体
- 后续容易引发 `NotFoundActor`、`location unlock not found`、空引用、Buff/初始化重复执行等次生问题

## 关键代码位置

### 1. Ordered mailbox 持锁位置

文件：`Packages/cn.etetet.actorlocation/Scripts/Hotfix/Server/MailBoxType_OrderedMessageHandler.cs`

关键逻辑：

- 第 24 行对 `mailBoxComponent.Parent.Id` 加 `Mailbox` 协程锁
- 第 36 行在持锁期间执行整个 `MessageDispatcher.Instance.HandleAsync(...)`

结论：

- 只要某条 ordered mailbox 消息整体执行超过 5 秒，就会触发该报错

### 2. 超时计时是在拿锁后开始

文件：`Packages/cn.etetet.core/Scripts/Core/Share/CoroutineLock/CoroutineLockQueueSystem.cs`

关键逻辑：

- 锁对象创建或唤醒后，立即调用 `coroutineLock.SetTimeout(...)`

结论：

- 这不是“排队等锁超时”
- 这是“持锁执行超时”

### 3. 超时后会强制释放锁

文件：`Packages/cn.etetet.core/Scripts/Core/Share/CoroutineLock/CoroutineLockSystem.cs`

关键逻辑：

- `SetTimeout(...)` 到时后，如果锁还存在，就打印错误并 `Dispose`

结论：

- 这类问题会破坏原本的串行执行语义

## 当前最高概率链路

结合“进图时报错”，当前最可疑的是下面这条链：

1. `MessageLocationHandler<Unit, ...>` 在目标 `Unit` 的 ordered mailbox 下执行消息
2. `MapManager2Map_NotifyPlayerTransferRequestHandler.Run(...)`
3. `TransferHelper.TransferLock(unit, request.MapName, request.MapId, request.TeamId, false)`
4. `TransferHelper.Transfer(...)`
5. `A2MapManager_GetMapRequest -> GetMapAsync(...)`
6. 若地图副本不存在，则创建新的 Map Fiber
7. `FiberInit_Map` 初始化地图
8. `M2M_UnitTransferRequestHandler`
9. `MapUnitEnterHelper` 做运行时组件、出生点、武器、被动、肉鸽初始化

对应文件：

- `Packages/cn.etetet.actorlocation/Scripts/Hotfix/Server/MessageLocationHandler.cs`
- `Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/MapManager2Map_NotifyPlayerTransferRequestHandler.cs`
- `Packages/cn.etetet.map/Scripts/Hotfix/Server/TransferHelper.cs`
- `Packages/cn.etetet.mapmanager/Scripts/Hotfix/Server/A2MapManager_GetMapRequestHandler.cs`
- `Packages/cn.etetet.mapmanager/Scripts/Hotfix/Server/MapManagerComponentSystem.cs`
- `Packages/cn.etetet.map/Scripts/Hotfix/Server/FiberInit_Map.cs`
- `Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/M2M_UnitTransferRequestHandler.cs`
- `Packages/cn.etetet.map/Scripts/Hotfix/Server/MapUnitEnterHelper.cs`

## 为什么进图链容易超时

### 原因 1：可能在邮箱持锁期间同步创建新地图副本

`GetMapAsync(...)` 在找不到现成副本时，会创建新的 Map Fiber。

该步骤本身可能包含：

- 创建新 Fiber
- 初始化 Map Scene
- 加载 Navmesh
- 加载 ECA
- 重建 ECA NavBlock
- 注册/订阅 ServiceDiscovery

如果这是第一次进入某张较重的地图，例如 `SDCMap`，这一步就可能很慢。

### 原因 2：目标地图接收 Unit 后还有一整套初始化

`M2M_UnitTransferRequestHandler` 接住 Unit 后，还会继续执行：

- `EnsureMapRuntimeComponents`
- `ApplyAssignedTeamIfNeeded`
- `ApplySpawnPointIfNeeded`
- `InitializePlayerGameplay`
- `SetupMatchRobotIfNeeded`
- `MatchCopyContextHelper.OnHumanPlayerEntered`

其中 `InitializePlayerGameplay` 当前仍然包含：

- 武器初始化
- 英雄被动 Buff 初始化
- 肉鸽进度初始化

这部分也会放大进图耗时。

## 与“节点配错”问题的关系

从这条超时日志本身看，不能直接判断是“哪个 BT/ECA 节点配错了”。

这类报错和之前的 “json 反序列化失败 / 节点字段为空 / BT 空引用” 不是同一类问题。

当前这条更偏向：

- 进图链整体过重
- 或某一步卡住
- 或首次建图初始化过慢

但是如果某张地图的 ECA 特别重，或者在 `MapLoadFinish` 时触发了大量刷怪/初始化，也会间接放大这个问题。

## 当前已有旁证

历史日志里可以看到多次正常转图链路：

- `start transfer1`
- `location proxy lock`
- `start transfer2`
- `M2M_UnitTransferRequest`
- `start transfer3`
- `location proxy unlock`
- `start transfer4`

也可以看到：

- `Home` 进图时确实会进入 `MapUnitEnterHelper.InitializePlayerGameplay`
- 当前代码里肉鸽初始化仍然在进图路径上
- `SDCMap` 在地图初始化后会触发大量刷怪相关日志

说明这个问题和“进图链过长”是对得上的。

## 当前未确认点

目前还没有直接证据证明超时一定卡在以下哪一步：

1. `GetMapAsync -> CreateFiber -> FiberInit_Map`
2. `M2M_UnitTransferRequestHandler -> MapUnitEnterHelper.InitializePlayerGameplay`
3. 某个更深层的跨 Fiber / 跨 Scene 调用

另外，这次报错里的精确 `key=321188793417758` 没有在当前落盘日志里搜到，说明：

- 可能只在控制台里出现，尚未刷新到 `Logs/All.log`
- 或者发生时日志被截断/覆盖

所以当前结论是基于代码链路和已有日志旁证做出的高概率判断。

## 后续排查建议

正确顺序应该是先定位“哪条消息、哪一步最慢”，再决定怎么优化。

建议按下面顺序排查：

1. 在 `MailBoxType_OrderedMessageHandler` 增加低频耗时日志
2. 记录消息类型、实体 ID、开始时间、结束时间、总耗时
3. 在 `TransferHelper.Transfer(...)` 内部分段打点
4. 在 `A2MapManager_GetMapRequestHandler / GetMapAsync / FiberInit_Map` 打点
5. 在 `M2M_UnitTransferRequestHandler` 和 `MapUnitEnterHelper.InitializePlayerGameplay` 打点

优先不要做的事：

- 不要先把 5000ms 简单改大

原因：

- 改大只能掩盖问题
- 不能解决“锁超时后串行语义被破坏”的根因

## 暂定修复方向

如果后续确认是首次建图过慢，优先考虑：

- 避免在 Unit mailbox 持锁期间承担完整建图成本
- 将建图和玩家进图解耦
- 预热高频地图副本

如果后续确认是单位初始化过重，优先考虑：

- 拆分 `InitializePlayerGameplay`
- 将非关键初始化延后
- 把不必须阻塞进图的逻辑转为异步后置执行

## 备注

这份文档只记录当前阶段的结论，不代表最终根因已经实锤。
后续如果补充了耗时打点，应在本文件追加“实测耗时结果”和“最终修复结论”。
