# 匹配系统代码分析文档

生成时间：2026-02-27
**最后状态更新：2026-03-26**

---

## 〇、集成缺口修复状态

> 以下对照原文"三、主流程集成缺口"逐项标注当前进展。

| # | 缺口描述 | 状态 | 备注 |
|---|----------|------|------|
| 1 | match 包层级升级第5层 | ✅ 已修复 | Level=5, AllowSameLevelAccess=true |
| 2 | Gate 收 C2G_MatchRequest 处理器 | ✅ 已修复 | `Gate/C2G_MatchRequestHandler.cs` |
| 3 | Gate 收 C2G_MatchCancel 处理器 | ✅ 已修复 | `Gate/C2G_MatchCancelHandler.cs` |
| 4 | MatchTickTimer 匹配成功后通知 Gate | ❌ 待办 | `MatchQueueComponentSystem.cs` 仍需补充 |
| 5 | Gate 收 Match2G_MatchSuccess 处理器 | ❌ 待办 | `Gate/Match2G_MatchSuccessHandler.cs` 不存在 |
| 6 | 定时器注册被注释 | ⚠️ 待确认 | 需检查 FiberInit_Match.cs 当前状态 |
| 7 | Localhost StartScene 缺 Match 场景 | ⚠️ 待确认 | 需检查 StartScene.xlsx |

### 测试修复状态

| 问题 | 状态 | 备注 |
|------|------|------|
| 测试 SceneType.Match → TestEmpty | ❌ 未修复 | 4个测试文件仍使用 SceneType.Match |

---

## 一、现有代码结构

### 1.1 包信息
- **包名**：`cn.etetet.match`
- **PackageType ID**：54
- **层级**：第3层 → **升级为第5层**（见执行计划）
- **`packagegit.json` 实际依赖**：`core`, `proto`, `startconfig`, `netinner`
- **`package.json` 声明依赖**（超出实际需要）：还多了 `unit`, `actorlocation`, `console`, `http`, `router`（这些在 packagegit.json 中没有，存在不一致）

---

### 1.2 Model 文件

| 文件 | 说明 |
|------|------|
| `PackageType.cs` | `PackageType.Match = 54` |
| `SceneType.cs` | `SceneType.Match = 54001` |
| `TimerInvokeType.cs` | `MatchTick = 54001` |
| `GameModeType.cs` | PVE=1, OneVsOne=2, ThreeVsThree=3, Extraction=4 |
| `MatchState.cs` | Waiting=0, Matched=1, Timeout=2, Cancelled=3 |
| `MatchQueueComponent.cs` | 挂在 Scene 上，管理匹配队列 |
| `MatchRequest.cs` | 子 Entity，代表一个排队玩家（含 PlayerId, GameMode, GateActorId, EnqueueTime, State） |
| `MatchResult.cs` | 匹配结果结构体（含 GameMode, MapName, PlayerIds, GateActorIds） |
| `MatchSuccessEvent.cs` | 匹配成功事件（**仅含 GameMode, MapName, PlayerIds，缺少 GateActorIds**） |

---

### 1.3 Hotfix/Server 文件

| 文件 | 说明 |
|------|------|
| `FiberInit_Match.cs` | Match Scene 初始化：添加 MailBox/Timer/Lock/ProcessInner/MessageSender/MatchQueueComponent，并注册 ServiceDiscovery |
| `MatchQueueComponentSystem.cs` | Enqueue / Cancel / TryMatch / CleanTimeoutRequests / GetQueueCount |
| `MatchHelper.cs` | GetRequiredPlayerCount() |
| `G2Match_MatchRequestHandler.cs` | 处理 Gate→Match 的匹配请求消息 |
| `G2Match_MatchCancelHandler.cs` | 处理 Gate→Match 的取消匹配消息 |

### 1.4 定时器（MatchTickTimer）

```csharp
[Invoke(TimerInvokeType.MatchTick)]
public class MatchTickTimer : ATimer<MatchQueueComponent>
{
    protected override void Run(MatchQueueComponent self)
    {
        // 对每种模式尝试 TryMatch
        // 匹配成功后 Publish MatchSuccessEvent（但 MatchSuccessEvent 没有 GateActorIds）
        // 清理超时请求
    }
}
```

> ⚠️ **已知问题**：定时器注册代码被注释掉（ET0037 分析器限制），因此**定时器在生产环境中不会自动触发**。

---

### 1.5 Proto 消息

#### Match_C_22000.proto（客户端↔Gate）
| 消息 | 类型 | 说明 |
|------|------|------|
| `C2G_MatchRequest` | ISessionRequest | 客户端发起匹配，包含 GameMode |
| `G2C_MatchRequest` | ISessionResponse | Gate 回复，包含 RequestId |
| `C2G_MatchCancel` | ISessionRequest | 客户端取消匹配，包含 RequestId |
| `G2C_MatchCancel` | ISessionResponse | Gate 回复取消结果 |
| `G2C_MatchSuccess` | IMessage | Gate 推送匹配成功（GameMode, MapName, PlayerIds） |
| `G2C_MatchTimeout` | IMessage | Gate 推送匹配超时 |

#### Match_S_22010.proto（Gate↔Match，服务端内部）
| 消息 | 类型 | 说明 |
|------|------|------|
| `G2Match_MatchRequest` | IRequest | Gate 转发匹配请求到 Match（含 PlayerId, GameMode, GateActorId） |
| `Match2G_MatchRequest` | IResponse | Match 回复 Gate（含 RequestId） |
| `G2Match_MatchCancel` | IRequest | Gate 转发取消到 Match |
| `Match2G_MatchCancel` | IResponse | Match 回复取消结果 |
| `Match2G_MatchSuccess` | **IRequest** | Match 主动通知 Gate 匹配成功 |
| `G2Match_MatchSuccess` | **IResponse** | Gate 回复确认 |
| `Match2G_MatchTimeout` | IMessage | Match 通知 Gate 超时 |

> ✅ 所有消息已生成 C# 代码到 `cn.etetet.proto` 包中，Opcode 从 22001 起。

---

## 二、测试用例分析

### 2.1 测试文件位置
`Packages/cn.etetet.match/Scripts/Hotfix/Test/`

| 测试类 | 功能 |
|--------|------|
| `Match_BasicFlow_Test` | 2人1v1匹配基础流程 |
| `Match_Cancel_Test` | 取消匹配 |
| `Match_Timeout_Test` | 超时清理 |
| `Match_Extraction_Test` | 搜打撤4人匹配 |

### 2.2 🔴 发现的 Bug：SceneType 错误

**所有 4 个测试文件**都使用了：
```csharp
await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
    context.Fiber, SceneType.Match, nameof(XXX_Test));  // ← 错误！
```

**问题根因**：
`SceneType.Match` 会触发 `FiberInit_Match`，而 `FiberInit_Match` 会：
1. 添加 `TimerComponent`、`CoroutineLockComponent`、`MatchQueueComponent`
2. 调用 `RegisterToServiceDiscovery()`（网络请求，测试环境必然失败）

随后测试代码**再次** `root.AddComponent<TimerComponent>()` 等，导致重复组件错误。

**修复方案**：改为 `SceneType.TestEmpty`（空 FiberInit，让测试自行初始化组件）。

```csharp
// 修改前（错误）
await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
    context.Fiber, SceneType.Match, nameof(Match_BasicFlow_Test));

// 修改后（正确）
await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
    context.Fiber, SceneType.TestEmpty, nameof(Match_BasicFlow_Test));
```

---

## 三、主流程集成缺口

当前匹配系统**只有服务端 Match 内部逻辑**，缺少完整的端到端流程接入。

### 3.1 完整流程图

```
客户端                  Gate                   Match服务器
  |                      |                         |
  |--C2G_MatchRequest--> |                         |
  |                      |--G2Match_MatchRequest-->|
  |                      |<--Match2G_MatchRequest--|  (返回 RequestId)
  |<--G2C_MatchRequest---|                         |
  |                      |                         |
  |                      |                   [定时器匹配成功]
  |                      |<--Match2G_MatchSuccess--|
  |                      |--G2Match_MatchSuccess-->|  (确认)
  |<--G2C_MatchSuccess---|                         |
```

### 3.2 缺少的代码

#### ❌ 缺 1：match 包层级需升级
将 `packagegit.json` 的 `Level: 3` 改为 `Level: 5`，并加上 `AllowSameLevelAccess: true`。
升级后 match 包可直接访问 login 的 `SessionPlayerComponent`、`Player` 等类型，Gate 处理器可以写在 match 包内，无需改动 login。

#### ❌ 缺 2：Gate 收 C2G_MatchRequest 处理器
位置：`cn.etetet.match/Scripts/Hotfix/Server/Gate/`
需要新建：`C2G_MatchRequestHandler.cs`
逻辑：
- 从 Session 获取 PlayerId（通过 `SessionPlayerComponent`）
- 发现 Match 服务器的 ActorId（通过服务发现）
- 转发 `G2Match_MatchRequest`，返回 `RequestId` 给客户端

#### ❌ 缺 3：Gate 收 C2G_MatchCancel 处理器
位置：`cn.etetet.match/Scripts/Hotfix/Server/Gate/`
需要新建：`C2G_MatchCancelHandler.cs`

#### ❌ 缺 4：Match 成功后通知 Gate
位置：`cn.etetet.match/Scripts/Hotfix/Server/MatchQueueComponentSystem.cs`
问题：`MatchTickTimer.Run` 匹配成功后没有向 Gate 发送通知
修复：直接在 `MatchTickTimer.Run` 中，按 GateActorId 分组，通过 `MessageSender` 发送 `Match2G_MatchSuccess`

#### ❌ 缺 5：Gate 收 Match2G_MatchSuccess 处理器
位置：`cn.etetet.match/Scripts/Hotfix/Server/Gate/`
需要新建：`Match2G_MatchSuccessHandler.cs`
逻辑：
- 遍历 `PlayerIds`，在 `PlayerComponent` 中找到对应玩家
- 通过玩家的 Session 推送 `G2C_MatchSuccess`

#### ❌ 缺 6：定时器注册被注释
位置：`MatchQueueComponentSystem.cs`，`Awake` 方法
当前状态：定时器代码已注释（ET0037 分析器问题）
影响：生产环境中匹配永远不会自动触发

#### ❌ 缺 7：Localhost StartScene 没有 Match 场景
需要在 `cn.etetet.startconfig/Luban/Localhost/Datas/StartScene.xlsx` 中添加 Match 场景配置。

---

## 四、定时器注册问题详解

当前注释掉的代码：
```csharp
// Awake 中（注释掉）
// self.TimerId = timerComponent.NewRepeatedTimer(1000, TimerInvokeType.MatchTick, self);

// Destroy 中（注释掉）
// timerComponent?.Remove(ref self.TimerId);
```

**ET0037 分析器**限制了跨包访问 `TimerComponent`，导致此处编译报错。
**解决方案**：在 `FiberInit_Match.cs` 添加 `MatchQueueComponent` 后，再通过 root 的 TimerComponent 来注册定时器（在 System 外部注册）。

或者通过 `IUpdate` 接口每帧检查，但定时器方式更优。

---

## 五、执行计划

### 第一阶段：修复测试并验证

| 步骤 | 操作 | 文件 |
|------|------|------|
| 1 | `SceneType.Match` → `SceneType.TestEmpty` | 4个测试文件 |
| 2 | `dotnet build ET.sln` 编译 | - |
| 3 | 运行测试 `Test --Name=Match` | - |
| 4 | 查看日志，修复失败用例 | `Logs/All.log` |

### 第二阶段：主流程集成

| 步骤 | 操作 | 位置 |
|------|------|------|
| 1 | match 包升级到第5层，加 AllowSameLevelAccess | `packagegit.json` |
| 2 | 修复定时器注册 | `FiberInit_Match.cs` |
| 3 | MatchTickTimer 匹配成功后发 Match2G_MatchSuccess | `MatchQueueComponentSystem.cs` |
| 4 | 新建 `C2G_MatchRequestHandler.cs` | match/Hotfix/Server/Gate/ |
| 5 | 新建 `C2G_MatchCancelHandler.cs` | match/Hotfix/Server/Gate/ |
| 6 | 新建 `Match2G_MatchSuccessHandler.cs` | match/Hotfix/Server/Gate/ |
| 7 | 更新 `StartScene.xlsx` 添加 Match 场景 | cn.etetet.startconfig/Luban/Localhost/ |
| 8 | 编译 + 验证 | - |

---

## 六、结论

- 匹配系统的**核心逻辑**（队列管理、匹配算法、超时清理）实现完整，代码质量良好
- 测试用例逻辑正确，但有 **SceneType 使用错误**导致无法运行
- 缺少 Gate 侧的消息处理器，**端到端流程尚未打通**
- 定时器注册被注释，需要修复
- 主流程集成工作量约 5-6 个新文件 + 2个现有文件修改