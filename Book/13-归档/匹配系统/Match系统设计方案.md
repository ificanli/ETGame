# Match 匹配系统设计方案

## 1. 概述

### 1.1 定位
Match 系统是搜打撤项目的入局入口，负责将玩家按游戏模式凑齐人数，产出匹配结果交给 Battle 系统创建战局。

### 1.2 职责边界
| 系统 | 职责 | 不负责 |
|------|------|--------|
| Match | 排队、匹配算法、产出匹配结果 | 战局创建、地图加载、战斗逻辑 |
| Battle | 接收匹配结果、创建战局、管理生命周期 | 排队、凑人 |

### 1.3 设计原则
- FIFO 先到先得（M0 阶段不做评分匹配）
- 人数可配置（通过配置表，不 hard code）
- 支持取消匹配
- 30 秒超时自动取消

---

## 2. 包信息

| 属性 | 值 |
|------|-----|
| 包名 | cn.etetet.match |
| PackageType ID | 54 |
| 层级 | 第 3 层 |
| 依赖 | cn.etetet.core, cn.etetet.proto, cn.etetet.startconfig, cn.etetet.netinner |
| 被依赖 | cn.etetet.battle (第4层) |

依赖说明：
- core, proto（第1层）：基础框架和协议定义
- startconfig（第2层）：SceneType 定义、服务器配置
- netinner（第3层）：跨 Scene 内网通信（ProcessInnerSender、MessageSender）

---

## 3. 游戏模式定义

| GameMode | 名称 | 默认人数 | 说明 |
|----------|------|----------|------|
| 1 | PVE | 1 | 单人打怪 |
| 2 | 1v1 | 2 | 双人对战 |
| 3 | 3v3 | 6 | 三对三团战 |
| 4 | 搜打撤 | 可配置(默认6) | 多人PVP+PVE，人数由地图配置决定 |

> 搜打撤模式人数不固定，通过配置表按地图设定。M0 默认 6 人，后续可调。

---

## 4. 目录结构

```
Packages/cn.etetet.match/
├── Scripts/
│   ├── Model/
│   │   └── Share/
│   │       ├── PackageType.cs          # 包类型 ID=54
│   │       ├── SceneType.cs            # SceneType.Match 定义
│   │       ├── TimerInvokeType.cs      # 定时器类型
│   │       ├── GameModeType.cs         # 游戏模式常量
│   │       ├── MatchState.cs           # 匹配状态常量
│   │       ├── MatchResult.cs          # 匹配结果结构体
│   │       ├── MatchSuccessEvent.cs    # 匹配成功事件
│   │       ├── MatchQueueComponent.cs  # 匹配队列（挂在 Match Scene 上）
│   │       └── MatchRequest.cs         # 匹配请求（ChildOf MatchQueueComponent）
│   └── Hotfix/
│       ├── Server/
│       │   ├── FiberInit_Match.cs              # Match Scene 初始化
│       │   ├── MatchQueueComponentSystem.cs    # 队列管理逻辑 + 定时器
│       │   ├── MatchHelper.cs                  # 辅助方法
│       │   ├── G2Match_MatchRequestHandler.cs  # 处理 Gate 转发的匹配请求
│       │   ├── G2Match_MatchCancelHandler.cs   # 处理 Gate 转发的取消请求
│       │   └── Match2G_MatchSuccessHandler.cs  # Gate 侧：收到匹配成功后推送客户端
│       └── Test/
│           ├── Match_BasicFlow_Test.cs         # 基础匹配流程
│           ├── Match_Cancel_Test.cs            # 取消匹配
│           ├── Match_Timeout_Test.cs           # 超时处理
│           └── Match_DuplicateRequest_Test.cs  # 重复请求防护
├── Proto/
│   ├── Match_C_10400.proto             # 客户端 ↔ Gate 消息
│   └── Match_S_10401.proto             # Gate ↔ Match Scene 内网消息
├── packagegit.json
└── AGENTS.md
```

---

## 4.1 Scene 设计

### SceneType.cs

```csharp
namespace ET
{
    public static partial class SceneType
    {
        public const int Match = PackageType.Match * 1000 + 1;
    }
}
```

Match 是全局单例 Scene（类似 Location、MapManager），整个服务器只有一个 Match Scene 实例，集中管理所有匹配队列。

### FiberInit_Match.cs

```csharp
namespace ET.Server
{
    [Invoke(SceneType.Match)]
    public class FiberInit_Match : AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();

            // 匹配队列
            root.AddComponent<MatchQueueComponent>();

            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy =
                root.AddComponent<ServiceDiscoveryProxy>();
            await serviceDiscoveryProxy.RegisterToServiceDiscovery();

            await ETTask.CompletedTask;
        }
    }
}
```

### StartSceneConfig 配置（需新增）

```json
{
    "Id": 12,
    "Process": 1,
    "Zone": 2,
    "SceneType": "Match",
    "Name": "Match",
    "Port": 0
}
```

### 消息路由架构

```
客户端 ←→ Gate ←→ Match Scene
                ↘→ Map Scene
```

- 客户端只和 Gate 通信（C2G_ / G2C_）
- Gate 收到匹配请求后，通过 MessageSender 转发给 Match Scene（G2Match_ / Match2G_）
- Match Scene 匹配成功后，通知 Gate 推送结果给客户端

---

## 5. Entity 设计

### 5.1 GameModeType.cs

```csharp
namespace ET
{
    /// <summary>
    /// 游戏模式类型
    /// </summary>
    public static class GameModeType
    {
        public const int PVE = 1;       // 单人PVE
        public const int OneVsOne = 2;  // 1v1
        public const int ThreeVsThree = 3; // 3v3
        public const int Extraction = 4;   // 搜打撤
    }
}
```

### 5.2 MatchState.cs

```csharp
namespace ET
{
    /// <summary>
    /// 匹配状态
    /// </summary>
    public static class MatchState
    {
        public const int Waiting = 0;    // 等待中
        public const int Matched = 1;    // 匹配成功
        public const int Timeout = 2;    // 超时
        public const int Cancelled = 3;  // 已取消
    }
}
```

### 5.3 MatchQueueComponent.cs

```csharp
namespace ET
{
    /// <summary>
    /// 匹配队列组件，挂在 Scene 上
    /// 按游戏模式管理多个匹配队列
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MatchQueueComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 游戏模式 -> 该模式需要的玩家人数
        /// 从配置表加载，不 hard code
        /// </summary>
        public Dictionary<int, int> ModePlayerCountDict;

        /// <summary>
        /// 玩家去重索引：PlayerId -> MatchRequest 的 EntityId
        /// 防止同一玩家重复排队
        /// </summary>
        public Dictionary<long, long> PlayerRequestDict;

        /// <summary>
        /// 匹配超时时间（毫秒），默认 30000
        /// </summary>
        public long MatchTimeoutMs;

        /// <summary>
        /// 定时器 Id（用于取消定时器）
        /// </summary>
        public long TimerId;
    }
}
```

> MatchRequest 作为 MatchQueueComponent 的子 Entity（ChildOf），
> 通过 `self.Children` 遍历所有请求，按 GameMode 筛选。

### 5.4 MatchRequest.cs

```csharp
namespace ET
{
    /// <summary>
    /// 匹配请求，MatchQueueComponent 的子 Entity
    /// 每个请求代表一个正在排队的玩家
    /// </summary>
    [ChildOf(typeof(MatchQueueComponent))]
    public class MatchRequest : Entity, IAwake<long, int>, IDestroy
    {
        /// <summary>
        /// 玩家 ID
        /// </summary>
        public long PlayerId;

        /// <summary>
        /// 游戏模式
        /// </summary>
        public int GameMode;

        /// <summary>
        /// 入队时间戳（毫秒）
        /// </summary>
        public long EnqueueTime;

        /// <summary>
        /// 匹配状态，见 MatchState
        /// </summary>
        public int State;

        /// <summary>
        /// 玩家所在 Gate 的 ActorId，匹配成功后用于回调通知
        /// </summary>
        public long GateActorId;
    }
}
```

### 5.5 MatchResult（匹配结果结构体）

```csharp
namespace ET
{
    /// <summary>
    /// 匹配结果，TryMatch 成功时产出
    /// </summary>
    public struct MatchResult
    {
        public int GameMode;
        public string MapName;
        public List<long> PlayerIds;
        public List<long> GateActorIds; // 各玩家对应的 Gate ActorId
    }
}
```

### 5.6 MatchSuccessEvent（匹配成功事件）

```csharp
namespace ET
{
    /// <summary>
    /// 匹配成功事件，供高层包（如 battle）订阅
    /// </summary>
    public struct MatchSuccessEvent
    {
        public int GameMode;
        public string MapName;
        public List<long> PlayerIds;
    }
}
```

---

## 6. System 设计

### 6.1 TimerInvokeType.cs

```csharp
namespace ET
{
    public static partial class TimerInvokeType
    {
        public const int MatchTick = PackageType.Match * 1000 + 1;
    }
}
```

### 6.2 MatchQueueComponentSystem.cs

```csharp
namespace ET.Server
{
    [EntitySystemOf(typeof(MatchQueueComponent))]
    public static partial class MatchQueueComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MatchQueueComponent self)
        {
            self.ModePlayerCountDict = new Dictionary<int, int>();
            self.PlayerRequestDict = new Dictionary<long, long>();
            self.MatchTimeoutMs = 30000;
            // TODO: 从配置表加载各模式人数

            // 启动定时器，每秒执行匹配和超时清理
            self.TimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(1000, TimerInvokeType.MatchTick, self);
        }

        [EntitySystem]
        private static void Destroy(this MatchQueueComponent self)
        {
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.TimerId);
            self.ModePlayerCountDict.Clear();
            self.PlayerRequestDict.Clear();
        }

        /// <summary>
        /// 加入匹配队列
        /// </summary>
        /// <returns>MatchRequest 的 Entity Id，用于取消。-1 表示已在队列中</returns>
        public static long Enqueue(this MatchQueueComponent self,
            long playerId, int gameMode, long gateActorId)
        {
            // 去重检查
            if (self.PlayerRequestDict.ContainsKey(playerId))
            {
                return -1;
            }
            // 创建 MatchRequest 子 Entity，加入队列
            // ...
        }

        /// <summary>
        /// 取消匹配
        /// </summary>
        public static bool Cancel(this MatchQueueComponent self,
            long requestId) { ... }

        /// <summary>
        /// 尝试匹配（由定时器驱动）
        /// </summary>
        /// <returns>匹配结果，null 表示未凑齐</returns>
        public static MatchResult? TryMatch(this MatchQueueComponent self,
            int gameMode) { ... }

        /// <summary>
        /// 清理超时请求
        /// </summary>
        public static void CleanTimeoutRequests(
            this MatchQueueComponent self) { ... }

        /// <summary>
        /// 获取指定模式的排队人数
        /// </summary>
        public static int GetQueueCount(this MatchQueueComponent self,
            int gameMode) { ... }
    }

    /// <summary>
    /// 匹配定时器回调
    /// </summary>
    [Invoke(TimerInvokeType.MatchTick)]
    public class MatchTickTimer : ATimer<MatchQueueComponent>
    {
        protected override void Run(MatchQueueComponent self)
        {
            // 遍历所有游戏模式，尝试匹配
            foreach (int gameMode in self.ModePlayerCountDict.Keys)
            {
                MatchResult? result = self.TryMatch(gameMode);
                if (result != null)
                {
                    // 1. Publish 事件（供 battle 等高层包订阅）
                    // EventSystem.Instance.Publish(self.Root(),
                    //     new MatchSuccessEvent { ... });

                    // 2. 通知各 Gate 推送给客户端
                    // ...
                }
            }

            // 清理超时请求
            self.CleanTimeoutRequests();
        }
    }
}

### 6.2 MatchHelper.cs

```csharp
namespace ET.Server
{
    public static class MatchHelper
    {
        /// <summary>
        /// 获取指定模式需要的玩家数量
        /// </summary>
        public static int GetRequiredPlayerCount(
            MatchQueueComponent queue, int gameMode)
        {
            if (queue.ModePlayerCountDict.TryGetValue(
                gameMode, out int count))
            {
                return count;
            }
            return 1; // 默认单人
        }
    }
}
```

---

## 7. Proto 消息设计

文件：`Proto/Match_C_10400.proto`

### 7.1 客户端 ↔ Gate 消息

```protobuf
syntax = "proto3";
package ET;

//===================== 客户端 ↔ Gate 匹配消息 =====================

// ResponseType G2C_MatchRequest
message C2G_MatchRequest // ISessionRequest
{
	int32 RpcId = 1;
	int32 GameMode = 2;     // 游戏模式，见 GameModeType
}

message G2C_MatchRequest // ISessionResponse
{
	int32 RpcId = 1;
	int32 Error = 2;
	string Message = 3;
	int64 RequestId = 4;    // 匹配请求ID，用于取消
}

// ResponseType G2C_MatchCancel
message C2G_MatchCancel // ISessionRequest
{
	int32 RpcId = 1;
	int64 RequestId = 2;    // 要取消的匹配请求ID
}

message G2C_MatchCancel // ISessionResponse
{
	int32 RpcId = 1;
	int32 Error = 2;
	string Message = 3;
}

// 匹配成功通知（Gate 推送给客户端）
message G2C_MatchSuccess // IMessage
{
	int32 GameMode = 1;
	string MapName = 2;
	repeated int64 PlayerIds = 3;
}

// 匹配超时通知
message G2C_MatchTimeout // IMessage
{
	int32 GameMode = 1;
}
```

### 7.2 Gate ↔ Match Scene 内网消息

文件：`Proto/Match_S_10401.proto`

```protobuf
syntax = "proto3";
package ET;

//===================== Gate ↔ Match 内网消息 =====================

// ResponseType Match2G_MatchRequest
message G2Match_MatchRequest // IRequest
{
	int32 RpcId = 1;
	int64 PlayerId = 2;
	int32 GameMode = 3;
	int64 GateActorId = 4;  // Gate 的 ActorId，用于回调通知
}

message Match2G_MatchRequest // IResponse
{
	int32 RpcId = 1;
	int32 Error = 2;
	string Message = 3;
	int64 RequestId = 4;
}

// ResponseType Match2G_MatchCancel
message G2Match_MatchCancel // IRequest
{
	int32 RpcId = 1;
	int64 RequestId = 2;
}

message Match2G_MatchCancel // IResponse
{
	int32 RpcId = 1;
	int32 Error = 2;
	string Message = 3;
}

// Match 匹配成功后通知 Gate（Gate 再推送给客户端）
message Match2G_MatchSuccess // IRequest
{
	int32 RpcId = 1;
	int32 GameMode = 2;
	string MapName = 3;
	repeated int64 PlayerIds = 4;
}

message G2Match_MatchSuccess // IResponse
{
	int32 RpcId = 1;
	int32 Error = 2;
	string Message = 3;
}

// Match 匹配超时通知 Gate
message Match2G_MatchTimeout // IMessage
{
	int64 PlayerId = 1;
	int32 GameMode = 2;
}
```

---

## 8. 匹配流程

### 8.1 时序图

```
客户端                    Gate                    Match Scene
  |                        |                          |
  |-- C2G_MatchRequest --->|                          |
  |                        |-- G2Match_MatchRequest ->|
  |                        |<- Match2G_MatchRequest --|
  |<-- G2C_MatchRequest ---|  (返回 RequestId)        |
  |                        |                          |
  |                        |     [定时器每秒 TryMatch] |
  |                        |                          |
  |                        |<- Match2G_MatchSuccess --|
  |<-- G2C_MatchSuccess ---|                          |
  |   (含地图、玩家列表)   |                          |
  |                        |                          |
  |-- C2G_EnterMap ------->|  (复用现有进入地图流程)   |
```

### 8.2 核心流程

1. 客户端发送 `C2G_MatchRequest`，携带 GameMode
2. Gate 通过 `MessageSender` 转发 `G2Match_MatchRequest` 给 Match Scene
3. Match Scene 创建 `MatchRequest` 子 Entity，加入队列
4. 返回 `RequestId` → Gate → 客户端
5. 定时器每秒调用 `TryMatch`：
   - 按 GameMode 筛选 Children 中状态为 Waiting 的请求
   - 按 EnqueueTime 排序（FIFO）
   - 凑够人数 → 标记为 Matched → 产出 MatchResult
6. 匹配成功后：
   - Publish `MatchSuccessEvent`（供 battle 等高层包订阅）
   - 发送 `Match2G_MatchSuccess` 给各玩家所在的 Gate
   - Gate 推送 `G2C_MatchSuccess` 给客户端
7. 客户端收到成功通知后，发送 `C2G_EnterMap` 进入战局地图（复用现有流程）

### 8.3 取消流程

1. 客户端发送 `C2G_MatchCancel`，携带 RequestId
2. Gate 转发 `G2Match_MatchCancel` 给 Match Scene
3. Match Scene 找到 MatchRequest，标记 Cancelled 并移除
4. 返回成功 → Gate → 客户端

### 8.4 超时流程

1. 定时器同时调用 `CleanTimeoutRequests`
2. 超过 30 秒的请求标记为 Timeout 并移除
3. 发送 `Match2G_MatchTimeout` 给对应 Gate
4. Gate 推送 `G2C_MatchTimeout` 给客户端

---

## 9. 配置表设计（Luban Excel）

### GameModeConfig 游戏模式配置表

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 游戏模式 ID |
| Name | string | 模式名称 |
| RequiredPlayerCount | int | 需要的玩家数量 |
| MapName | string | 默认地图名 |
| MatchTimeoutMs | long | 匹配超时（毫秒） |
| AllowBot | bool | 是否允许机器人补位 |

> M0 阶段先用代码内字典配置，M1 迁移到 Excel 配置表。

---

## 10. 测试用例设计

### 10.1 Match_BasicFlow_Test
- 创建 MatchQueueComponent
- 加入 2 个 1v1 请求
- 调用 TryMatch
- 验证：返回 2 个玩家 ID，状态变为 Matched

### 10.2 Match_Cancel_Test
- 加入 1 个请求
- 调用 Cancel
- 验证：请求被移除，状态为 Cancelled
- 再次 TryMatch 不会匹配到已取消的请求

### 10.3 Match_Timeout_Test
- 加入 1 个请求（1v1 模式，凑不齐）
- 模拟时间超过 30 秒
- 调用 CleanTimeoutRequests
- 验证：请求被清理，状态为 Timeout

### 10.4 Match_Extraction_Test
- 设置搜打撤模式人数为 4
- 加入 4 个搜打撤请求
- 调用 TryMatch
- 验证：返回 4 个玩家 ID

---

## 11. 开发检查清单

- [ ] 创建 cn.etetet.match 包目录
- [ ] 配置 packagegit.json（ID=54，依赖 core/proto/startconfig/netinner）
- [ ] 创建 PackageType.cs（ID=54）
- [ ] 创建 SceneType.cs（SceneType.Match）
- [ ] 创建 TimerInvokeType.cs（MatchTick）
- [ ] 实现 GameModeType.cs、MatchState.cs
- [ ] 实现 MatchResult.cs、MatchSuccessEvent.cs
- [ ] 实现 MatchQueueComponent.cs（Entity，含去重索引和定时器Id）
- [ ] 实现 MatchRequest.cs（Entity，含 GateActorId）
- [ ] 实现 FiberInit_Match.cs（Scene 初始化）
- [ ] 实现 MatchQueueComponentSystem.cs + MatchTickTimer（System + 定时器）
- [ ] 实现 MatchHelper.cs
- [ ] 实现 G2Match_MatchRequestHandler.cs（Match Scene 侧 Handler）
- [ ] 实现 G2Match_MatchCancelHandler.cs
- [ ] 实现 Match2G_MatchSuccessHandler.cs（Gate 侧 Handler）
- [ ] 编写 Match_C_10400.proto（客户端消息）
- [ ] 编写 Match_S_10401.proto（内网消息）
- [ ] 更新 StartSceneConfig 添加 Match Scene 配置
- [ ] 编写测试用例（4个）
- [ ] 编译验证 `dotnet build ET.sln`
- [ ] 运行测试验证
- [ ] 编写 AGENTS.md

---

## 12. 风险和待定项

| 项目 | 说明 | 处理方式 |
|------|------|----------|
| 机器人补位 | 搜打撤凑不齐人时是否用 AI 补位 | M0 暂不做，M1 评估 |
| 跨服匹配 | 多服务器时的匹配合并 | M0 单服，不涉及 |
| 评分匹配 | MMR/ELO 公平匹配 | M2 排位模式再做 |
| Gate 侧 Handler 归属 | Match2G_MatchSuccessHandler 在 Gate 上执行，但代码在 match 包中，需确认 match 包是否能注册 Gate SceneType 的 Handler | 开发时验证，若不行则放 login 包 |
