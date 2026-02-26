# cn.etetet.eca - ECA 框架包

## 包概述

ECA（Event-Condition-Action）框架是一个通用的交互物和触发器系统，用于实现搜打撤游戏中的各种场景交互物。

### 核心功能

- **状态管理**：支持交互物的状态转换和状态历史记录
- **事件驱动**：基于事件触发的状态转换机制
- **条件判断**：灵活的条件检查系统
- **动作执行**：可组合的动作执行系统
- **可视化编辑**：GraphView 可视化编辑器

### 适用场景

- 撤离点：玩家进入范围后倒计时撤离
- 搜索容器：玩家搜索容器获得物品
- 任务触发器：触发任务和事件
- 刷怪点：定时或条件刷怪
- 其他交互物：滑索、草丛等

---

## 包信息

- **包名**：cn.etetet.eca
- **PackageType ID**：50
- **层级**：第2层
- **依赖**：cn.etetet.core, cn.etetet.proto

---

## 目录结构

```
cn.etetet.eca/
├── packagegit.json
├── AGENTS.md
├── Scripts/
│   ├── Model/
│   │   └── Share/
│   │       ├── PackageType.cs              # 包类型定义
│   │       ├── ECANode.cs                  # 节点基类
│   │       ├── ECAStateNode.cs             # 状态节点
│   │       ├── ECAEventNode.cs             # 事件节点
│   │       ├── ECAConditionNode.cs         # 条件节点
│   │       ├── ECAActionNode.cs            # 动作节点
│   │       ├── ECAConnection.cs            # 连接数据
│   │       └── ECAConfig.cs                # 配置根对象
│   └── Hotfix/
│       ├── Server/
│       │   ├── ECAPointComponent.cs        # ECA 点组件
│       │   ├── ECAPointComponentSystem.cs  # ECA 点系统
│       │   ├── ECAStateComponent.cs        # 状态管理组件
│       │   ├── ECAStateComponentSystem.cs  # 状态管理系统
│       │   ├── ECAConditionHelper.cs       # 条件检查辅助类
│       │   ├── ECAActionHelper.cs          # 动作执行辅助类
│       │   └── ECALoader.cs                # 配置加载器
│       └── Test/
│           ├── ECA_BasicFlow_Test.cs       # 基础流程测试
│           └── ECA_Evacuation_Test.cs      # 撤离点测试
└── Editor/
    └── ECAEditor/
        ├── ECAEditor.cs                    # 主编辑器窗口
        ├── ECAGraphView.cs                 # GraphView 实现
        └── ...                             # 节点视图类
```

---

## 核心概念

### ECA 三要素

#### Event（事件）
触发状态转换的事件：
- OnPlayerEnter：玩家进入范围
- OnPlayerLeave：玩家离开范围
- OnPlayerInteract：玩家交互
- OnTimerTick：定时触发
- OnMonsterDead：怪物死亡

#### Condition（条件）
状态转换的条件判断：
- StateCheck：状态检查
- HasItem：拥有物品
- LevelCheck：等级检查
- TimeRange：时间范围
- Custom：自定义条件

#### Action（动作）
状态转换后执行的动作：
- StartEvacuation：开始撤离
- SpawnMonster：生成怪物
- OpenContainer：打开容器
- ShowUI：显示UI
- PlayEffect：播放特效

### 状态管理

每个 ECA 点都有状态管理系统：
- **当前状态**：交互物的当前状态
- **状态转换**：定义合法的状态转换路径
- **状态历史**：记录状态变更历史
- **状态回调**：状态进入/退出时的回调

---

## 使用示例

### 示例1：撤离点

**配置**：
- 初始状态：Available
- 事件：OnPlayerEnter
- 条件：StateCheck(Available)
- 动作：StartEvacuation(10秒)
- 目标状态：Evacuated

**流程**：
1. 玩家进入撤离点范围
2. 检查状态是否为 Available
3. 开始撤离倒计时（10秒）
4. 倒计时结束后跳转回 lobby
5. 状态变为 Evacuated

### 示例2：搜索容器

**配置**：
- 初始状态：Closed
- 事件：OnPlayerInteract
- 条件：StateCheck(Closed)
- 动作：OpenContainer(搜索3秒)
- 目标状态：Opened

**流程**：
1. 玩家点击容器
2. 检查状态是否为 Closed
3. 显示搜索UI，倒计时3秒
4. 搜索完成后显示物品
5. 状态变为 Opened

---

## 开发规范

### Entity 规范

```csharp
/// <summary>
/// ECA 配置点组件
/// </summary>
[ComponentOf(typeof(Unit))]
public class ECAPointComponent : Entity, IAwake<ECAConfig>, IDestroy
{
    // 只包含数据字段，无方法
    public string PointId;
    public int PointType;
    public int CurrentState;
    // ...
}
```

**要点**：
- 继承 Entity
- 实现 IAwake, IDestroy
- 添加 [ComponentOf] 特性
- 只包含数据字段
- 详细的中文注释

### System 规范

```csharp
/// <summary>
/// ECA 配置点系统
/// </summary>
[EntitySystemOf(typeof(ECAPointComponent))]
public static partial class ECAPointComponentSystem
{
    [EntitySystem]
    private static void Awake(this ECAPointComponent self, ECAConfig config)
    {
        // 初始化逻辑
    }

    /// <summary>
    /// 玩家进入范围
    /// </summary>
    public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
    {
        // 业务逻辑
    }
}
```

**要点**：
- 静态类 + partial
- 添加 [EntitySystemOf] 特性
- 生命周期方法添加 [EntitySystem] 特性
- 业务方法是静态扩展方法

### EntityRef 规范

```csharp
public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
{
    // 1. await 前创建 EntityRef
    EntityRef<ECAPointComponent> selfRef = self;
    EntityRef<Unit> playerRef = player;

    // 2. 执行异步操作
    await SomeAsyncOperation();

    // 3. await 后重新获取
    self = selfRef;
    player = playerRef;

    // 4. 检查有效性
    if (self == null || player == null)
    {
        return;
    }

    // 5. 安全使用
    self.PlayersInRange.Add(player.Id);
}
```

---

## 测试规范

### 测试命名

```csharp
/// <summary>
/// 测试 ECA 基础流程
/// </summary>
[Test]
public class ECA_BasicFlow_Test : ATestHandler
{
    protected override async ETTask<int> Run(Fiber fiber, TestArgs args)
    {
        // 测试逻辑
        return ErrorCode.ERR_Success;
    }
}
```

**要点**：
- 命名格式：`ECA_{TestName}_Test`
- 继承 ATestHandler
- 使用 Log.Console 输出错误
- 每个错误返回唯一数字

---

## 注意事项

### 必须遵守

1. **Entity 只包含数据**：不能有方法
2. **System 只包含逻辑**：静态扩展方法
3. **EntityRef 规范**：await 后重新获取
4. **禁止 hard code**：所有配置从 ECAConfig 读取
5. **状态转换验证**：确保状态转换合法

### 常见错误

1. ❌ Entity 中定义方法
2. ❌ System 忘记添加特性
3. ❌ await 后直接使用 Entity
4. ❌ 在代码中写死配置
5. ❌ 状态转换规则配置错误

---

## 更新日志

### 2026-02-26
- 创建 cn.etetet.eca 包
- 定义包基础结构
- 编写 AGENTS.md 文档

