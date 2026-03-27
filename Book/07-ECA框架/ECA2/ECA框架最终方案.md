# ECA 框架最终方案（2026-02-26 确认版）

## 一、架构设计

### 1.1 核心理念
- **场景配置优先**：在 Unity 场景中直接配置 ECA 点，所见即所得
- **服务器驱动**：所有逻辑在服务器端执行，客户端只负责显示
- **ET 规范严格遵守**：Component 在 Model，System 在 Hotfix，使用 EntityRef
- **框架与业务分离**：参考 BehaviorTree 的设计，分为核心框架包和业务节点包

### 1.2 分包策略

#### cn.etetet.eca (ID=52, Level 2) - 核心框架包
**职责**：
- ECA 核心数据结构（ECAConfig, ECANode 基类, ECAConnection）
- 运行时引擎（ECAPointComponent, ECAManagerComponent）
- 加载器（ECALoader）
- 编辑器基础设施（GraphView 框架）
- 基础 Helper 类

**依赖**：core, excel, proto, unit, behaviortree, http, startconfig, console, numeric, netinner, router, actorlocation, aoi, yooassets, yiuiframework

**不包含**：具体的业务节点实现（撤离、刷怪等）

#### cn.etetet.ecanode (ID=53, Level 5) - 业务节点包
**职责**：
- 撤离点节点（PlayerEvacuationComponent, PlayerEvacuationComponentSystem）
- 刷怪点节点（SpawnMonsterComponent, SpawnMonsterComponentSystem）
- 容器节点（ContainerComponent, ContainerComponentSystem）
- 其他业务相关的节点实现

**依赖**：eca, map, move（需要访问地图和移动系统）

**特性**：`AllowSameLevelAccess: true` - 允许同层包访问

### 1.3 三层架构

```
┌─────────────────────────────────────────┐
│  Unity 编辑器层 (ModelView)              │
│  - ECAPointMarker (MonoBehaviour)       │
│  - ECAConfigAsset (ScriptableObject)    │
│  - Gizmos 可视化                         │
└─────────────────────────────────────────┘
                  ↓ 运行时加载
┌─────────────────────────────────────────┐
│  数据层 (Model/Share)                    │
│  - ECAConfig (运行时配置)                │
│  - ECANode (节点基类)                    │
│  - ECAConnection (连接数据)              │
└─────────────────────────────────────────┘
                  ↓ 服务器使用
┌─────────────────────────────────────────┐
│  服务器逻辑层 (Model/Server + Hotfix)    │
│  - ECAPointComponent (Entity)           │
│  - PlayerEvacuationComponent (Entity)   │
│  - ECAPointComponentSystem (System)     │
│  - PlayerEvacuationComponentSystem      │
└─────────────────────────────────────────┘
```

## 二、撤离点完整流程

### 2.1 配置阶段（Unity 编辑器）
1. 在场景中创建 GameObject
2. 添加 ECAPointMarker 组件
3. 创建 ECAConfigAsset 配置
4. 关联配置到 Marker

### 2.2 加载阶段（地图初始化）
```csharp
// 地图加载完成后调用
await ECALoader.LoadECAPoints(mapScene);
```

**实现位置**：`Hotfix/Server/ECALoader.cs`
- 查找场景中所有 ECAPointMarker
- 转换为 ECAConfig
- 创建 ECAPointComponent Entity
- 添加到场景的 ECAManagerComponent

### 2.3 检测阶段（玩家移动）
```csharp
// 在 MoveTimer 中每帧检测
ECAHelper.CheckPlayerInRange(unit);
```

**实现位置**：`Hotfix/Server/ECAHelper.cs`
- 遍历所有 ECA 点
- 计算玩家与 ECA 点的距离
- 触发 OnPlayerEnter/OnPlayerLeave 事件

### 2.4 撤离阶段（进入范围）
```csharp
// 玩家进入撤离点范围
1. 创建 PlayerEvacuationComponent
2. 设置 Status = 1 (InProgress)
3. 启动倒计时（10秒）
```

**实现位置**：`Hotfix/Server/PlayerEvacuationComponentSystem.cs`
- Update 方法每帧检查倒计时
- 检查玩家是否离开范围
- 倒计时结束后调用 CompleteEvacuation

### 2.5 完成阶段（跳转大厅）
```csharp
// 撤离完成
await TransferHelper.TransferAtFrameFinish(unit, "Lobby", lobbyMapId);
```

**实现位置**：`PlayerEvacuationComponentSystem.CompleteEvacuation`
- 使用 TransferHelper 跳转场景
- 清理 PlayerEvacuationComponent

## 三、关键技术点

### 3.1 EntityRef 使用
```csharp
EntityRef<Unit> unitRef = unit;
await SomeAsyncOperation();
unit = unitRef;  // 必须重新获取
if (unit == null || unit.IsDisposed) return;  // 检查有效性
```

### 3.2 Scene 获取
```csharp
// ❌ 错误：unit.Domain()
// ✅ 正确：
Scene scene = unit.Scene();
Scene root = unit.Root();
```

### 3.3 范围检测优化
- 使用 HashSet 记录范围内玩家
- 只在进入/离开时触发事件
- 避免每帧重复触发

### 3.4 Timer 机制
```csharp
// 撤离倒计时使用 Timer
self.TimerId = unit.Root().GetComponent<TimerComponent>()
    .NewFrameTimer(TimerInvokeType.PlayerEvacuationTimer, self);
```

## 四、文件结构

### 4.1 cn.etetet.eca 包（核心框架）

```
cn.etetet.eca/
├── packagegit.json (ID=52, Level=2)
├── package.json
├── AGENTS.md
├── Scripts/
│   ├── Model/
│   │   ├── Share/
│   │   │   ├── ECAConfig.cs              ✅ 已完成
│   │   │   ├── ECANode.cs                ✅ 已完成（基类）
│   │   │   ├── ECAStateNode.cs           ✅ 已完成
│   │   │   ├── ECAEventNode.cs           ✅ 已完成
│   │   │   ├── ECAConditionNode.cs       ✅ 已完成
│   │   │   ├── ECAActionNode.cs          ✅ 已完成
│   │   │   ├── ECAConnection.cs          ✅ 已完成
│   │   │   └── PackageType.cs            ✅ 已完成
│   │   └── Server/
│   │       ├── ECAPointComponent.cs      ✅ 已完成
│   │       └── ECAManagerComponent.cs    ⚠️ 需创建
│   ├── Hotfix/
│   │   └── Server/
│   │       ├── ECAPointComponentSystem.cs    ⚠️ 需完善
│   │       ├── ECAManagerComponentSystem.cs  ⚠️ 需创建
│   │       ├── ECALoader.cs              ⚠️ 需创建
│   │       └── ECAHelper.cs              ⚠️ 需创建
│   └── ModelView/
│       ├── Share/
│       │   └── ECAConfigAsset.cs         ✅ 已完成
│       └── Client/
│           └── ECAPointMarker.cs         ✅ 已完成
└── Editor/
    └── ECAEditor/
        └── (GraphView 编辑器，后续实现)
```

### 4.2 cn.etetet.ecanode 包（业务节点）

```
cn.etetet.ecanode/
├── packagegit.json (ID=53, Level=5, AllowSameLevelAccess=true)
├── package.json
├── AGENTS.md
└── Scripts/
    ├── Model/
    │   └── Server/
    │       ├── PlayerEvacuationComponent.cs  ⚠️ 需创建
    │       ├── SpawnMonsterComponent.cs      📝 后续
    │       └── ContainerComponent.cs         📝 后续
    └── Hotfix/
        └── Server/
            ├── PlayerEvacuationComponentSystem.cs  ⚠️ 需创建
            ├── PlayerEvacuationTimer.cs            ⚠️ 需创建
            ├── SpawnMonsterComponentSystem.cs      📝 后续
            └── ContainerComponentSystem.cs         📝 后续
```

## 五、集成点

### 5.1 地图加载集成
**位置**：`cn.etetet.map` 包的地图初始化代码
```csharp
// 在地图加载完成后
await ECALoader.LoadECAPoints(mapScene);
```

### 5.2 玩家移动集成
**位置**：`cn.etetet.move` 包的 MoveTimer
```csharp
[Invoke(TimerInvokeType.MoveTimer)]
public class MoveTimer: ATimer<MoveComponent>
{
    protected override void Run(MoveComponent self)
    {
        self.MoveForward(true);

        // 添加 ECA 范围检测
        Unit unit = self.GetParent<Unit>();
        ECAHelper.CheckPlayerInRange(unit);
    }
}
```

### 5.3 Lobby 跳转
**SceneType 定义**：需要在 statesync 包中查找 Lobby 的 SceneType
**跳转代码**：
```csharp
await TransferHelper.TransferAtFrameFinish(unit, "Lobby", lobbyMapId);
```

## 六、测试验收标准

### 6.1 场景配置测试
- ✅ 能在 Unity 中创建 ECAPointMarker
- ✅ 能看到绿色范围球体
- ✅ 能关联 ECAConfigAsset

### 6.2 功能测试
- ⚠️ 进入地图后，ECA 点被正确加载
- ⚠️ 玩家进入范围，触发撤离倒计时
- ⚠️ 倒计时 10 秒后，跳转回 Lobby
- ⚠️ 玩家离开范围，取消撤离

### 6.3 日志验证
```
[ECALoader] Loading ECA points from scene: Map001
[ECALoader] Loaded 1 ECA points
[ECAPointSystem] Player 123456 entered evacuation point: evac_001
[PlayerEvacuation] Player 123456 started evacuation, time: 10000ms
[PlayerEvacuation] Player 123456 evacuation progress: 50%
[PlayerEvacuation] Player 123456 evacuation completed
[TransferHelper] Transferring player 123456 to Lobby
```

## 七、后续扩展

### 7.1 GraphView 编辑器（1-2周）
- 可视化节点编辑
- 状态流转图
- 调试工具

### 7.2 其他交互物类型
- 刷怪点（SpawnPoint）
- 容器（Container）
- 传送点（Teleport）
- 触发器（Trigger）

### 7.3 高级功能
- 条件检查（HasItem, LevelCheck）
- 复杂动作（SpawnMonster, ShowUI）
- 状态重置和冷却

## 八、注意事项

1. **严格遵守 ET 规范**：Component 只有数据，System 有逻辑
2. **EntityRef 必须使用**：await 后必须重新获取 Entity
3. **命名空间正确**：Server 代码使用 `namespace ET.Server`
4. **避免 Unity 类型**：服务器代码不能用 Vector3，用 float x,y,z
5. **Timer 正确使用**：注册 TimerInvokeType，实现 ATimer
6. **日志充分**：关键步骤都要有日志，方便调试

---

**最后更新**：2026-02-26 20:30
**状态**：方案确认完成，开始实现
