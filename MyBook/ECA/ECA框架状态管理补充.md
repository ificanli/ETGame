# ECA 框架 - 状态管理补充设计

## 问题描述

原设计缺少状态管理，导致无法处理以下场景：
- ❌ 容器打开后不能再次打开
- ❌ 刷怪点刷怪后需要冷却
- ❌ 撤离点被使用后可能需要禁用
- ❌ 任务触发器只能触发一次

## 解决方案：添加状态管理系统

---

## 1. 状态定义

### 通用状态枚举

```csharp
namespace ET
{
    /// <summary>
    /// ECA 点通用状态
    /// </summary>
    public enum ECAPointState
    {
        Idle = 0,           // 空闲/可用
        Active = 1,         // 激活中
        Completed = 2,      // 已完成
        Disabled = 3,       // 已禁用
        Cooldown = 4,       // 冷却中
    }
}
```

### 特定类型的状态

不同类型的 ECA 点可以有自己的状态定义：

**容器状态**：
- Closed (0) - 未打开
- Opening (1) - 打开中
- Opened (2) - 已打开
- Looted (3) - 已掠夺

**刷怪点状态**：
- Ready (0) - 准备就绪
- Spawning (1) - 正在刷怪
- Spawned (2) - 已刷怪
- Cooldown (3) - 冷却中

**撤离点状态**：
- Available (0) - 可用
- InUse (1) - 使用中
- Evacuated (2) - 已撤离
- Disabled (3) - 已禁用

---

## 2. Entity 设计补充

### ECAPointComponent 添加状态字段

```csharp
namespace ET
{
    /// <summary>
    /// ECA 配置点组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ECAPointComponent : Entity, IAwake<ECAPointConfig>, IDestroy
    {
        // ... 原有字段 ...

        /// <summary>
        /// 当前状态
        /// </summary>
        public int CurrentState;

        /// <summary>
        /// 初始状态（用于重置）
        /// </summary>
        public int InitialState;

        /// <summary>
        /// 状态数据（JSON格式，存储状态相关的额外数据）
        /// </summary>
        public string StateData;

        /// <summary>
        /// 状态改变时间
        /// </summary>
        public long StateChangeTime;

        /// <summary>
        /// 是否可重置（例如刷怪点可以重复触发）
        /// </summary>
        public bool IsResetable;

        /// <summary>
        /// 重置冷却时间（毫秒）
        /// </summary>
        public long ResetCooldown;

        /// <summary>
        /// 状态历史（用于调试和回溯）
        /// </summary>
        public List<StateChangeRecord> StateHistory;
    }

    /// <summary>
    /// 状态变更记录
    /// </summary>
    public struct StateChangeRecord
    {
        public int FromState;
        public int ToState;
        public long Timestamp;
        public long TriggeredBy; // 触发者ID（玩家ID）
        public string Reason;
    }
}
```

### ECAStateComponent - 状态管理组件

```csharp
namespace ET
{
    /// <summary>
    /// ECA 状态管理组件
    /// 专门管理状态转换和状态相关逻辑
    /// </summary>
    [ComponentOf(typeof(ECAPointComponent))]
    public class ECAStateComponent : Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 状态转换规则字典 FromState -> List<ToState>
        /// </summary>
        public Dictionary<int, List<int>> StateTransitions;

        /// <summary>
        /// 状态转换条件字典 (FromState, ToState) -> ConditionFunc
        /// </summary>
        public Dictionary<(int, int), Func<bool>> TransitionConditions;

        /// <summary>
        /// 状态进入回调
        /// </summary>
        public Dictionary<int, Action> OnStateEnter;

        /// <summary>
        /// 状态退出回调
        /// </summary>
        public Dictionary<int, Action> OnStateExit;

        /// <summary>
        /// 是否正在转换状态
        /// </summary>
        public bool IsTransitioning;
    }
}
```

---

## 3. Unity 配置层补充

### ECAPointConfig 添加状态配置

```csharp
namespace ET.Client
{
    public class ECAPointConfig : MonoBehaviour
    {
        // ... 原有字段 ...

        [Header("状态配置")]
        [Tooltip("初始状态")]
        public int InitialState = 0;

        [Tooltip("是否可重置")]
        public bool IsResetable = false;

        [Tooltip("重置冷却时间（毫秒）")]
        public long ResetCooldown = 0;

        [Tooltip("状态转换规则（JSON格式）")]
        [TextArea(3, 10)]
        public string StateTransitions = @"{
            ""0"": [1, 3],
            ""1"": [2],
            ""2"": [0, 3]
        }";

        [Header("状态条件配置")]
        [Tooltip("每个状态的条件检查")]
        public StateConditionConfig[] StateConditions = new StateConditionConfig[0];
    }

    [System.Serializable]
    public class StateConditionConfig
    {
        [Tooltip("状态值")]
        public int State;

        [Tooltip("该状态下的条件类型")]
        public ECAConditionType[] ConditionTypes;

        [Tooltip("条件参数")]
        public string ConditionParams;
    }
}
```

---

## 4. System 实现

### ECAStateComponentSystem

```csharp
namespace ET.Server
{
    [EntitySystemOf(typeof(ECAStateComponent))]
    public static partial class ECAStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAStateComponent self)
        {
            self.StateTransitions = new Dictionary<int, List<int>>();
            self.TransitionConditions = new Dictionary<(int, int), Func<bool>>();
            self.OnStateEnter = new Dictionary<int, Action>();
            self.OnStateExit = new Dictionary<int, Action>();
            self.IsTransitioning = false;
        }

        [EntitySystem]
        private static void Update(this ECAStateComponent self)
        {
            // 检查是否需要自动状态转换（例如冷却结束）
            self.CheckAutoTransition();
        }

        [EntitySystem]
        private static void Destroy(this ECAStateComponent self)
        {
            self.StateTransitions.Clear();
            self.TransitionConditions.Clear();
            self.OnStateEnter.Clear();
            self.OnStateExit.Clear();
        }

        /// <summary>
        /// 尝试转换状态
        /// </summary>
        public static bool TryChangeState(this ECAStateComponent self, int newState, long triggeredBy, string reason = "")
        {
            ECAPointComponent ecaPoint = self.GetParent<ECAPointComponent>();
            int currentState = ecaPoint.CurrentState;

            // 检查是否正在转换
            if (self.IsTransitioning)
            {
                Log.Warning($"State is transitioning, cannot change to {newState}");
                return false;
            }

            // 检查状态转换是否合法
            if (!self.CanTransition(currentState, newState))
            {
                Log.Warning($"Invalid state transition: {currentState} -> {newState}");
                return false;
            }

            // 检查转换条件
            if (!self.CheckTransitionCondition(currentState, newState))
            {
                Log.Debug($"Transition condition not met: {currentState} -> {newState}");
                return false;
            }

            // 执行状态转换
            self.IsTransitioning = true;

            // 退出旧状态
            self.OnStateExit.TryGetValue(currentState, out var exitCallback);
            exitCallback?.Invoke();

            // 记录状态变更
            ecaPoint.StateHistory ??= new List<StateChangeRecord>();
            ecaPoint.StateHistory.Add(new StateChangeRecord
            {
                FromState = currentState,
                ToState = newState,
                Timestamp = TimeInfo.Instance.ServerNow(),
                TriggeredBy = triggeredBy,
                Reason = reason
            });

            // 更新状态
            ecaPoint.CurrentState = newState;
            ecaPoint.StateChangeTime = TimeInfo.Instance.ServerNow();

            // 进入新状态
            self.OnStateEnter.TryGetValue(newState, out var enterCallback);
            enterCallback?.Invoke();

            self.IsTransitioning = false;

            Log.Debug($"State changed: {currentState} -> {newState}, reason: {reason}");
            return true;
        }

        /// <summary>
        /// 检查是否可以转换到目标状态
        /// </summary>
        public static bool CanTransition(this ECAStateComponent self, int fromState, int toState)
        {
            if (!self.StateTransitions.TryGetValue(fromState, out var allowedStates))
            {
                return false;
            }

            return allowedStates.Contains(toState);
        }

        /// <summary>
        /// 检查转换条件
        /// </summary>
        private static bool CheckTransitionCondition(this ECAStateComponent self, int fromState, int toState)
        {
            if (self.TransitionConditions.TryGetValue((fromState, toState), out var condition))
            {
                return condition();
            }

            return true; // 没有条件则默认通过
        }

        /// <summary>
        /// 检查自动状态转换（例如冷却结束）
        /// </summary>
        private static void CheckAutoTransition(this ECAStateComponent self)
        {
            ECAPointComponent ecaPoint = self.GetParent<ECAPointComponent>();

            // 检查冷却状态
            if (ecaPoint.CurrentState == (int)ECAPointState.Cooldown)
            {
                long elapsed = TimeInfo.Instance.ServerNow() - ecaPoint.StateChangeTime;
                if (elapsed >= ecaPoint.ResetCooldown)
                {
                    // 冷却结束，重置到初始状态
                    self.TryChangeState(ecaPoint.InitialState, 0, "Cooldown finished");
                }
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public static void ResetState(this ECAStateComponent self)
        {
            ECAPointComponent ecaPoint = self.GetParent<ECAPointComponent>();

            if (!ecaPoint.IsResetable)
            {
                Log.Warning($"ECA point {ecaPoint.PointId} is not resetable");
                return;
            }

            self.TryChangeState(ecaPoint.InitialState, 0, "Manual reset");
        }
    }
}
```

### ECAPointComponentSystem 状态集成

```csharp
namespace ET.Server
{
    public static partial class ECAPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAPointComponent self, ECAPointConfig config)
        {
            // ... 原有初始化 ...

            // 状态初始化
            self.CurrentState = config.InitialState;
            self.InitialState = config.InitialState;
            self.StateChangeTime = TimeInfo.Instance.ServerNow();
            self.IsResetable = config.IsResetable;
            self.ResetCooldown = config.ResetCooldown;
            self.StateHistory = new List<StateChangeRecord>();

            // 添加状态管理组件
            ECAStateComponent stateComp = self.AddComponent<ECAStateComponent>();

            // 解析状态转换规则
            if (!string.IsNullOrEmpty(config.StateTransitions))
            {
                var transitions = MongoHelper.FromJson<Dictionary<string, int[]>>(config.StateTransitions);
                foreach (var kvp in transitions)
                {
                    int fromState = int.Parse(kvp.Key);
                    stateComp.StateTransitions[fromState] = new List<int>(kvp.Value);
                }
            }

            // 初始化状态回调（根据类型）
            self.InitializeStateCallbacks(stateComp);
        }

        /// <summary>
        /// 初始化状态回调
        /// </summary>
        private static void InitializeStateCallbacks(this ECAPointComponent self, ECAStateComponent stateComp)
        {
            switch (self.PointType)
            {
                case 3: // Container
                    self.InitializeContainerStateCallbacks(stateComp);
                    break;
                case 2: // SpawnPoint
                    self.InitializeSpawnPointStateCallbacks(stateComp);
                    break;
                // 其他类型...
            }
        }

        /// <summary>
        /// 初始化容器状态回调
        /// </summary>
        private static void InitializeContainerStateCallbacks(this ECAPointComponent self, ECAStateComponent stateComp)
        {
            // 进入 Opened 状态
            stateComp.OnStateEnter[2] = () =>
            {
                Log.Debug($"Container {self.PointId} opened");
                // 可以在这里播放特效、更新UI等
            };

            // 退出 Closed 状态
            stateComp.OnStateExit[0] = () =>
            {
                Log.Debug($"Container {self.PointId} is no longer closed");
            };
        }

        /// <summary>
        /// 初始化刷怪点状态回调
        /// </summary>
        private static void InitializeSpawnPointStateCallbacks(this ECAPointComponent self, ECAStateComponent stateComp)
        {
            // 进入 Spawned 状态后，自动进入冷却
            stateComp.OnStateEnter[2] = () =>
            {
                Log.Debug($"Spawn point {self.PointId} spawned monsters");

                // 如果可重置，进入冷却状态
                if (self.IsResetable)
                {
                    stateComp.TryChangeState((int)ECAPointState.Cooldown, 0, "Enter cooldown after spawn");
                }
            };
        }

        /// <summary>
        /// 玩家进入范围（添加状态检查）
        /// </summary>
        public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
        {
            if (!self.IsActive)
            {
                return;
            }

            // 检查状态是否允许触发
            if (!self.CanTriggerInCurrentState())
            {
                Log.Debug($"ECA point {self.PointId} cannot trigger in state {self.CurrentState}");
                return;
            }

            // ... 原有逻辑 ...

            // 执行动作后，可能需要改变状态
            await self.ExecuteActions(player);

            // 根据类型改变状态
            self.UpdateStateAfterAction(player.Id);
        }

        /// <summary>
        /// 检查当前状态是否可以触发
        /// </summary>
        private static bool CanTriggerInCurrentState(this ECAPointComponent self)
        {
            // 根据类型判断
            switch (self.PointType)
            {
                case 3: // Container
                    return self.CurrentState == 0; // 只有 Closed 状态可以打开
                case 2: // SpawnPoint
                    return self.CurrentState == 0; // 只有 Ready 状态可以刷怪
                case 1: // EvacuationPoint
                    return self.CurrentState == 0; // 只有 Available 状态可以撤离
                default:
                    return true;
            }
        }

        /// <summary>
        /// 动作执行后更新状态
        /// </summary>
        private static void UpdateStateAfterAction(this ECAPointComponent self, long playerId)
        {
            ECAStateComponent stateComp = self.GetComponent<ECAStateComponent>();

            switch (self.PointType)
            {
                case 3: // Container
                    // 打开后进入 Opened 状态
                    stateComp.TryChangeState(2, playerId, "Container opened");
                    break;
                case 2: // SpawnPoint
                    // 刷怪后进入 Spawned 状态
                    stateComp.TryChangeState(2, playerId, "Monsters spawned");
                    break;
                // 其他类型...
            }
        }
    }
}
```

---

## 5. 条件检查中添加状态判断

### 新增状态条件类型

```csharp
namespace ET.Client
{
    public enum ECAConditionType
    {
        None = 0,
        HasItem = 1,
        LevelCheck = 2,
        TeamSizeCheck = 3,
        TimeRangeCheck = 4,
        QuestCheck = 5,
        StateCheck = 6,         // 新增：状态检查
    }
}
```

### ECAConditionHelper 添加状态检查

```csharp
namespace ET.Server
{
    public static partial class ECAConditionHelper
    {
        public static bool Check(int conditionType, Unit player, ECAPointComponent ecaPoint)
        {
            return conditionType switch
            {
                0 => true,
                1 => CheckHasItem(player, ecaPoint),
                2 => CheckLevel(player, ecaPoint),
                3 => CheckTeamSize(player, ecaPoint),
                4 => CheckTimeRange(player, ecaPoint),
                5 => CheckQuest(player, ecaPoint),
                6 => CheckState(player, ecaPoint),  // 新增
                _ => true
            };
        }

        /// <summary>
        /// 检查状态条件
        /// </summary>
        private static bool CheckState(Unit player, ECAPointComponent ecaPoint)
        {
            // 解析参数
            var param = MongoHelper.FromJson<StateCheckParam>(ecaPoint.ConditionParams);

            // 检查当前状态是否在允许的状态列表中
            return param.AllowedStates.Contains(ecaPoint.CurrentState);
        }
    }

    public struct StateCheckParam
    {
        public int[] AllowedStates;
    }
}
```

---

## 6. 使用示例

### 示例1：容器（只能打开一次）

**Unity 配置**：
```
Point Type: Container
Point Id: "chest_001"
Initial State: 0 (Closed)
Is Resetable: false
State Transitions: {
    "0": [1, 2],    // Closed -> Opening, Opened
    "1": [2],       // Opening -> Opened
    "2": []         // Opened -> (无法转换)
}
Event Types: OnPlayerInteract
Condition Types: StateCheck
Condition Params: {"allowedStates": [0]}  // 只有 Closed 状态可以交互
Action Types: OpenContainer
```

**运行效果**：
1. 玩家第一次交互：状态 Closed → Opening → Opened，容器打开
2. 玩家第二次交互：状态检查失败（当前是 Opened），不触发动作

### 示例2：刷怪点（可重复触发，有冷却）

**Unity 配置**：
```
Point Type: SpawnPoint
Point Id: "spawn_001"
Initial State: 0 (Ready)
Is Resetable: true
Reset Cooldown: 60000 (60秒)
State Transitions: {
    "0": [1, 2],    // Ready -> Spawning, Spawned
    "1": [2],       // Spawning -> Spawned
    "2": [4],       // Spawned -> Cooldown
    "4": [0]        // Cooldown -> Ready
}
Event Types: OnPlayerEnter
Condition Types: StateCheck
Condition Params: {"allowedStates": [0]}  // 只有 Ready 状态可以刷怪
Action Types: SpawnMonster
```

**运行效果**：
1. 玩家进入范围：状态 Ready → Spawned，生成怪物
2. 自动进入冷却：状态 Spawned → Cooldown
3. 60秒后自动重置：状态 Cooldown → Ready
4. 可以再次触发

### 示例3：撤离点（使用后禁用）

**Unity 配置**：
```
Point Type: EvacuationPoint
Point Id: "evac_001"
Initial State: 0 (Available)
Is Resetable: false
State Transitions: {
    "0": [1],       // Available -> InUse
    "1": [2, 0],    // InUse -> Evacuated, Available (取消)
    "2": [3]        // Evacuated -> Disabled
}
Event Types: OnPlayerEnter, OnPlayerLeave
Condition Types: StateCheck
Condition Params: {"allowedStates": [0]}
Action Types: StartEvacuation
```

---

## 7. 状态持久化（可选）

如果需要保存状态（例如容器打开后永久保持打开）：

```csharp
namespace ET.Server
{
    /// <summary>
    /// 保存 ECA 点状态
    /// </summary>
    public static void SaveECAPointState(ECAPointComponent ecaPoint)
    {
        // 保存到数据库或文件
        var stateData = new ECAPointStateData
        {
            PointId = ecaPoint.PointId,
            CurrentState = ecaPoint.CurrentState,
            StateChangeTime = ecaPoint.StateChangeTime,
            StateData = ecaPoint.StateData
        };

        // 保存逻辑...
    }

    /// <summary>
    /// 加载 ECA 点状态
    /// </summary>
    public static void LoadECAPointState(ECAPointComponent ecaPoint)
    {
        // 从数据库或文件加载
        // var stateData = LoadFromDB(ecaPoint.PointId);
        // ecaPoint.CurrentState = stateData.CurrentState;
        // ...
    }
}
```

---

## 8. 优势

✅ **状态管理**：清晰的状态定义和转换规则
✅ **防止重复触发**：容器打开后不会再次触发
✅ **支持冷却**：刷怪点可以重复触发，但有冷却时间
✅ **灵活配置**：状态转换规则可在 Unity 中配置
✅ **可追溯**：状态历史记录，方便调试
✅ **可扩展**：支持自定义状态和状态回调

---

## 9. 开发计划更新

在原有 ECA 框架开发计划基础上，添加状态管理：

### 第1步：基础框架（1-2天）
- [ ] 创建 ECAStateComponent
- [ ] 实现状态转换逻辑
- [ ] 实现状态检查条件

### 第2步：Unity 配置层（1天）
- [ ] 添加状态配置字段
- [ ] 实现状态可视化（显示当前状态）

### 第3步：核心功能实现（2-3天）
- [ ] 容器状态管理
- [ ] 刷怪点状态管理（含冷却）
- [ ] 撤离点状态管理

### 第4步：测试（1-2天）
- [ ] 测试状态转换
- [ ] 测试重复触发防护
- [ ] 测试冷却机制
