# ECA 框架最终方案确认

## 确认：我已理解 AGENTS.md 文档

### ✅ Skills 目录理解
- et-arch：架构和规范守护（创建 Entity/System 时使用）
- et-build：编译构建（编译项目时使用）
- et-test-write：测试编写（编写测试用例时使用）
- et-test-run：测试执行（运行测试时使用）
- et-tdd：测试驱动开发（TDD 流程）

### ✅ 核心规范理解
1. **ECS 架构**：Entity 只有数据，System 只有逻辑
2. **程序集分层**：Model（数据）、Hotfix（逻辑）、ModelView（UI数据）、HotfixView（UI逻辑）
3. **包依赖规则**：单向依赖，递归添加依赖，不能相互依赖
4. **EntityRef 规范**：await 后必须通过 EntityRef 重新获取 Entity
5. **绝对禁止 hard code**
6. **统一编译命令**：`dotnet build ET.sln`

---

## 最终方案总结

### 1. 包结构设计

#### 包名：`cn.etetet.eca`
- **层级**：第2层（和 behaviortree 同级）
- **PackageType ID**：50
- **依赖**：cn.etetet.core, cn.etetet.proto

#### 目录结构
```
cn.etetet.eca/
├── packagegit.json
├── AGENTS.md
├── Scripts/
│   ├── Model/
│   │   └── Share/
│   │       ├── PackageType.cs                  # ID = 50
│   │       ├── ECANode.cs                      # 节点基类
│   │       ├── ECAStateNode.cs                 # 状态节点
│   │       ├── ECAEventNode.cs                 # 事件节点
│   │       ├── ECAConditionNode.cs             # 条件节点
│   │       ├── ECAActionNode.cs                # 动作节点
│   │       ├── ECAConnection.cs                # 连接数据
│   │       └── ECAConfig.cs                    # 配置根对象（ScriptableObject）
│   └── Hotfix/
│       ├── Server/
│       │   ├── ECAPointComponent.cs            # ECA 点组件
│       │   ├── ECAPointComponentSystem.cs      # ECA 点系统
│       │   ├── ECAStateComponent.cs            # 状态管理组件
│       │   ├── ECAStateComponentSystem.cs      # 状态管理系统
│       │   ├── ECAConditionHelper.cs           # 条件检查辅助类
│       │   ├── ECAActionHelper.cs              # 动作执行辅助类
│       │   └── ECALoader.cs                    # 配置加载器
│       └── Test/
│           └── ECA_BasicFlow_Test.cs           # 基础流程测试
└── Editor/
    └── ECAEditor/
        ├── ECAEditor.cs                        # 主编辑器窗口
        ├── ECAGraphView.cs                     # GraphView 实现
        ├── ECANodeView.cs                      # 节点视图基类
        ├── ECAStateNodeView.cs                 # 状态节点视图
        ├── ECAEventNodeView.cs                 # 事件节点视图
        ├── ECAConditionNodeView.cs             # 条件节点视图
        ├── ECAActionNodeView.cs                # 动作节点视图
        └── ECAEditor.uss                       # 样式表
```

---

### 2. 核心设计决策

#### 决策1：配置方式 - GraphView ✅
- **原因**：状态流转可视化，策划友好，调试方便
- **实现**：Unity GraphView 编辑器 + ScriptableObject 保存

#### 决策2：状态管理 ✅
- **原因**：容器打开后不能再次打开，刷怪点需要冷却
- **实现**：ECAStateComponent 管理状态转换，支持状态历史记录

#### 决策3：数据结构 - 图形结构 ✅
- **原因**：ECA 是状态图，不是树形结构
- **实现**：ECANode + ECAConnection，节点之间通过连接关联

#### 决策4：新建包而非复用 BT 包 ✅
- **原因**：BT 是树形结构，ECA 是图形结构，数据结构完全不同
- **实现**：创建 cn.etetet.eca 包，参考 BT 的 GraphView 实现经验

---

### 3. ECS 架构规范遵循

#### Entity 设计（Model/Share）

**ECAPointComponent.cs**：
```csharp
namespace ET
{
    /// <summary>
    /// ECA 配置点组件
    /// 服务端运行时的 ECA 点实体
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ECAPointComponent : Entity, IAwake<ECAConfig>, IDestroy
    {
        /// <summary>
        /// 配置点ID
        /// </summary>
        public string PointId;

        /// <summary>
        /// 配置点类型
        /// </summary>
        public int PointType;

        /// <summary>
        /// 交互范围
        /// </summary>
        public float InteractRange;

        /// <summary>
        /// 当前状态
        /// </summary>
        public int CurrentState;

        /// <summary>
        /// 初始状态
        /// </summary>
        public int InitialState;

        /// <summary>
        /// 当前范围内的玩家
        /// </summary>
        public HashSet<long> PlayersInRange;

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// 状态改变时间
        /// </summary>
        public long StateChangeTime;

        /// <summary>
        /// 是否可重置
        /// </summary>
        public bool IsResetable;

        /// <summary>
        /// 重置冷却时间（毫秒）
        /// </summary>
        public long ResetCooldown;
    }
}
```

**关键点**：
- ✅ 继承 Entity
- ✅ 实现 IAwake, IDestroy
- ✅ 添加 [ComponentOf] 特性
- ✅ 只包含数据字段，无方法
- ✅ 使用 HashSet<long> 而非 List<Entity>（Entity 只能管理 Entity 和 struct）
- ✅ 详细的中文注释

#### System 设计（Hotfix/Server）

**ECAPointComponentSystem.cs**：
```csharp
namespace ET.Server
{
    /// <summary>
    /// ECA 配置点系统
    /// </summary>
    [EntitySystemOf(typeof(ECAPointComponent))]
    public static partial class ECAPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAPointComponent self, ECAConfig config)
        {
            self.PointId = config.ConfigId;
            self.PointType = config.PointType;
            self.InteractRange = config.InteractRange;
            self.CurrentState = config.InitialState;
            self.InitialState = config.InitialState;
            self.PlayersInRange = new HashSet<long>();
            self.IsActive = true;
            self.StateChangeTime = TimeInfo.Instance.ServerNow();
            self.IsResetable = config.IsResetable;
            self.ResetCooldown = config.ResetCooldown;

            // 添加状态管理组件
            self.AddComponent<ECAStateComponent>();
        }

        [EntitySystem]
        private static void Destroy(this ECAPointComponent self)
        {
            self.PlayersInRange.Clear();
        }

        /// <summary>
        /// 玩家进入范围
        /// </summary>
        public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
        {
            // 业务逻辑实现
        }
    }
}
```

**关键点**：
- ✅ 静态类 + partial
- ✅ 添加 [EntitySystemOf] 特性
- ✅ 生命周期方法添加 [EntitySystem] 特性，声明为 private static
- ✅ 业务方法是静态扩展方法
- ✅ 详细的中文注释

#### Helper 设计（Hotfix/Server）

**ECAConditionHelper.cs**：
```csharp
namespace ET.Server
{
    /// <summary>
    /// ECA 条件检查辅助类
    /// 处理条件相关的业务逻辑
    /// </summary>
    public static class ECAConditionHelper
    {
        /// <summary>
        /// 检查条件
        /// </summary>
        public static bool Check(int conditionType, Unit player, ECAPointComponent ecaPoint)
        {
            return conditionType switch
            {
                0 => true, // None
                1 => CheckHasItem(player, ecaPoint),
                2 => CheckLevel(player, ecaPoint),
                // ...
                _ => true
            };
        }

        private static bool CheckHasItem(Unit player, ECAPointComponent ecaPoint)
        {
            // 具体业务逻辑
            return true;
        }
    }
}
```

**关键点**：
- ✅ 复杂业务逻辑放在 Helper 中，不放在 System 中
- ✅ 静态类，静态方法

---

### 4. EntityRef 和 await 规范

#### 正确示例

```csharp
public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
{
    // 1. await 前创建 EntityRef
    EntityRef<ECAPointComponent> selfRef = self;
    EntityRef<Unit> playerRef = player;

    // 2. 执行异步操作
    await SomeAsyncOperation();

    // 3. await 后重新获取 Entity
    self = selfRef;
    player = playerRef;

    // 4. 检查 Entity 是否有效
    if (self == null || player == null)
    {
        return;
    }

    // 5. 现在可以安全使用
    self.PlayersInRange.Add(player.Id);
}
```

**关键点**：
- ✅ await 前创建 EntityRef
- ✅ await 后通过 EntityRef 重新获取
- ✅ 检查 Entity 是否为 null
- ✅ 不使用 EntityRef.Entity 属性（直接赋值）

---

### 5. 包依赖配置

#### packagegit.json
```json
{
  "Id": 50,
  "Name": "ECA",
  "AllowAccessField": true,
  "Level": 2
}
```

#### package.json（如果需要）
```json
{
  "dependencies": {
    "cn.etetet.core": "1.0.0",
    "cn.etetet.proto": "1.0.0"
  }
}
```

**关键点**：
- ✅ ID 唯一（50）
- ✅ Level 2（第2层）
- ✅ 依赖 core 和 proto
- ✅ 递归添加依赖的依赖（core 和 proto 没有其他依赖）

---

### 6. 测试驱动开发（TDD）

#### 测试用例命名规范

**ECA_BasicFlow_Test.cs**：
```csharp
namespace ET.Server
{
    /// <summary>
    /// 测试 ECA 基础流程
    /// </summary>
    [Test]
    public class ECA_BasicFlow_Test : ATestHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, TestArgs args)
        {
            Scene scene = fiber.Root.CurrentScene();

            // 1. 创建 ECA 点
            Unit ecaUnit = scene.GetComponent<UnitComponent>().AddChild<Unit, int>(1001);
            ECAPointComponent ecaPoint = ecaUnit.AddComponent<ECAPointComponent, ECAConfig>(testConfig);

            if (ecaPoint == null)
            {
                Log.Console("Failed to create ECAPointComponent");
                return 1;
            }

            // 2. 测试状态初始化
            if (ecaPoint.CurrentState != 0)
            {
                Log.Console($"Initial state error, expected: 0, actual: {ecaPoint.CurrentState}");
                return 2;
            }

            // 3. 测试玩家进入
            Unit player = scene.GetComponent<UnitComponent>().AddChild<Unit, int>(2001);
            await ecaPoint.OnPlayerEnter(player);

            if (!ecaPoint.PlayersInRange.Contains(player.Id))
            {
                Log.Console("Player not in range");
                return 3;
            }

            Log.Debug("ECA basic flow test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
```

**关键点**：
- ✅ 命名格式：`ECA_{TestName}_Test`
- ✅ 继承 ATestHandler
- ✅ 使用 Log.Console 输出错误
- ✅ 每个错误返回唯一数字
- ✅ 成功返回 ErrorCode.ERR_Success

---

### 7. 开发流程（TDD）

#### 第1步：创建包结构（1天）
1. 创建 `Packages/cn.etetet.eca` 目录
2. 创建 packagegit.json（ID=50）
3. 创建 PackageType.cs（ID=50）
4. 创建 AGENTS.md 文档

#### 第2步：定义数据结构（1-2天）
1. 创建 ECANode.cs（基类）
2. 创建 ECAStateNode.cs
3. 创建 ECAEventNode.cs
4. 创建 ECAConditionNode.cs
5. 创建 ECAActionNode.cs
6. 创建 ECAConnection.cs
7. 创建 ECAConfig.cs（ScriptableObject）

#### 第3步：实现服务端组件（2-3天）
1. 创建 ECAPointComponent.cs（Entity）
2. 创建 ECAPointComponentSystem.cs（System）
3. 创建 ECAStateComponent.cs（Entity）
4. 创建 ECAStateComponentSystem.cs（System）
5. 创建 ECAConditionHelper.cs
6. 创建 ECAActionHelper.cs
7. 创建 ECALoader.cs

#### 第4步：编写测试用例（1-2天）
1. 创建 ECA_BasicFlow_Test.cs
2. 创建 ECA_StateTransition_Test.cs
3. 创建 ECA_Condition_Test.cs
4. 创建 ECA_Action_Test.cs

#### 第5步：实现 GraphView 编辑器（1-2周）
1. 创建 ECAEditor.cs
2. 创建 ECAGraphView.cs
3. 创建节点视图类
4. 实现保存和加载
5. 实现调试功能

#### 第6步：集成测试和优化（3-5天）
1. 端到端测试
2. 性能优化
3. 文档完善

---

### 8. 潜在问题和注意事项

#### ⚠️ 问题1：EntityRef 使用
- **风险**：await 后忘记重新获取 Entity
- **解决**：严格遵循 EntityRef 规范，每次 await 后都重新获取

#### ⚠️ 问题2：状态转换死锁
- **风险**：状态转换规则配置错误，导致无法转换
- **解决**：在 ECAStateComponent 中添加状态转换验证

#### ⚠️ 问题3：循环依赖
- **风险**：包依赖配置错误
- **解决**：严格遵循包层级规则，ECA 在第2层，只依赖第1层

#### ⚠️ 问题4：hard code
- **风险**：在代码中写死配置
- **解决**：所有配置都从 ECAConfig 读取

#### ⚠️ 问题5：Entity 管理非 Entity class
- **风险**：在 Entity 中使用 List<SomeClass>
- **解决**：只使用 Entity、struct、基础类型

---

### 9. 编译和测试命令

#### 编译
```powershell
dotnet build ET.sln -p:TreatWarningsAsErrors=false
```

#### 运行测试
```powershell
# 运行所有 ECA 测试
dotnet Bin/ET.App.dll --Console=1 --Test=ECA_.*
```

---

### 10. 最终检查清单

#### 包结构检查
- [ ] packagegit.json 配置正确（ID=50）
- [ ] PackageType.cs 编号正确（ID=50）
- [ ] AGENTS.md 文档完整
- [ ] 目录结构符合规范

#### ECS 架构检查
- [ ] Entity 只包含数据，无方法
- [ ] System 是静态类 + partial
- [ ] 生命周期方法添加 [EntitySystem] 特性
- [ ] 业务逻辑在 Helper 中

#### EntityRef 检查
- [ ] await 前创建 EntityRef
- [ ] await 后重新获取 Entity
- [ ] 不使用 EntityRef.Entity 属性

#### 测试检查
- [ ] 测试命名符合规范
- [ ] 使用 Log.Console 输出错误
- [ ] 每个错误返回唯一数字
- [ ] 成功返回 ErrorCode.ERR_Success

#### 依赖检查
- [ ] 包依赖配置正确
- [ ] 递归添加依赖的依赖
- [ ] 无循环依赖

#### 代码质量检查
- [ ] 无 hard code
- [ ] 详细的中文注释
- [ ] 命名规范统一
- [ ] 无重复定义

---

## 确认声明

我已经：
1. ✅ 完整阅读并理解 AGENTS.md 文档
2. ✅ 理解 Skills 目录和使用场景
3. ✅ 理解 ECS 架构核心原则
4. ✅ 理解程序集分层结构
5. ✅ 理解包依赖规范
6. ✅ 理解 EntityRef 和 await 规范
7. ✅ 理解测试驱动开发流程
8. ✅ 理解绝对禁止事项

我承诺：
1. ✅ 严格遵循 ECS 架构规范
2. ✅ 严格遵循 EntityRef 使用规范
3. ✅ 严格遵循包依赖规则
4. ✅ 绝对禁止 hard code
5. ✅ 使用 TDD 流程开发
6. ✅ 编写详细的中文注释
7. ✅ 每次操作前说明原因

---

## 开始开发

**准备就绪，可以开始开发了！**

第一步：创建 `cn.etetet.eca` 包的基础结构。

是否开始？

