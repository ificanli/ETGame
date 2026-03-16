# ECA 框架设计方案

## 设计目标

创建一个通用的 Event-Condition-Action 框架，支持：
- ✅ 在 Unity 场景中直接配置交互点
- ✅ 撤离点、刷怪点、宝箱等多种类型
- ✅ 可视化配置，无需修改代码
- ✅ 支持复杂的条件判断和动作组合

---

## 核心概念

### ECA 三要素

**Event（事件）**：触发条件
- 玩家进入范围
- 玩家离开范围
- 玩家交互（按键）
- 时间到达
- 怪物死亡
- 等等...

**Condition（条件）**：判断逻辑
- 是否有特定物品
- 是否达到等级
- 是否在特定时间段
- 队伍人数是否满足
- 等等...

**Action（动作）**：执行效果
- 开始撤离倒计时
- 生成怪物
- 播放特效
- 显示UI
- 传送玩家
- 等等...

---

## 架构设计

### 1. Unity 场景配置层（客户端）

**ECAPointConfig.cs** - MonoBehaviour 组件
```csharp
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// ECA 配置点组件
    /// 挂在 Unity 场景的 GameObject 上
    /// </summary>
    public class ECAPointConfig : MonoBehaviour
    {
        [Header("基础配置")]
        [Tooltip("配置点类型")]
        public ECAPointType PointType = ECAPointType.EvacuationPoint;

        [Tooltip("唯一标识符")]
        public string PointId = "";

        [Tooltip("交互范围（米）")]
        public float InteractRange = 3f;

        [Header("事件配置")]
        [Tooltip("触发事件类型")]
        public ECAEventType[] EventTypes = new[] { ECAEventType.OnPlayerEnter };

        [Header("条件配置")]
        [Tooltip("条件类型")]
        public ECAConditionType[] ConditionTypes = new ECAConditionType[0];

        [Tooltip("条件参数（JSON格式）")]
        public string ConditionParams = "{}";

        [Header("动作配置")]
        [Tooltip("动作类型")]
        public ECAActionType[] ActionTypes = new[] { ECAActionType.StartEvacuation };

        [Tooltip("动作参数（JSON格式）")]
        public string ActionParams = "{\"evacuateTime\": 5000}";

        [Header("可视化")]
        [Tooltip("是否显示范围")]
        public bool ShowRange = true;

        [Tooltip("范围颜色")]
        public Color RangeColor = Color.green;

        // 在编辑器中绘制范围
        private void OnDrawGizmos()
        {
            if (ShowRange)
            {
                Gizmos.color = RangeColor;
                Gizmos.DrawWireSphere(transform.position, InteractRange);
            }
        }

        // 在编辑器中绘制选中时的范围
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, InteractRange);
        }
    }

    /// <summary>
    /// 配置点类型
    /// </summary>
    public enum ECAPointType
    {
        EvacuationPoint = 1,    // 撤离点
        SpawnPoint = 2,         // 刷怪点
        Container = 3,          // 容器/宝箱
        Teleport = 4,           // 传送点
        Trigger = 5,            // 触发器
        NPC = 6,                // NPC
    }

    /// <summary>
    /// 事件类型
    /// </summary>
    public enum ECAEventType
    {
        OnPlayerEnter = 1,      // 玩家进入范围
        OnPlayerLeave = 2,      // 玩家离开范围
        OnPlayerInteract = 3,   // 玩家交互（按键）
        OnTimerTick = 4,        // 定时触发
        OnMonsterDead = 5,      // 怪物死亡
        OnItemCollect = 6,      // 物品收集
    }

    /// <summary>
    /// 条件类型
    /// </summary>
    public enum ECAConditionType
    {
        None = 0,               // 无条件
        HasItem = 1,            // 拥有物品
        LevelCheck = 2,         // 等级检查
        TeamSizeCheck = 3,      // 队伍人数检查
        TimeRangeCheck = 4,     // 时间范围检查
        QuestCheck = 5,         // 任务检查
    }

    /// <summary>
    /// 动作类型
    /// </summary>
    public enum ECAActionType
    {
        StartEvacuation = 1,    // 开始撤离
        SpawnMonster = 2,       // 生成怪物
        OpenContainer = 3,      // 打开容器
        PlayEffect = 4,         // 播放特效
        ShowUI = 5,             // 显示UI
        TeleportPlayer = 6,     // 传送玩家
        GiveReward = 7,         // 给予奖励
    }
}
```

### 2. 服务端 Entity 层

**ECAPointComponent.cs** (Model/Share)
```csharp
namespace ET
{
    /// <summary>
    /// ECA 配置点组件
    /// 服务端运行时的 ECA 点实体
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ECAPointComponent : Entity, IAwake<ECAPointConfig>, IDestroy
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
        /// 事件类型列表
        /// </summary>
        public List<int> EventTypes;

        /// <summary>
        /// 条件类型列表
        /// </summary>
        public List<int> ConditionTypes;

        /// <summary>
        /// 条件参数（JSON）
        /// </summary>
        public string ConditionParams;

        /// <summary>
        /// 动作类型列表
        /// </summary>
        public List<int> ActionTypes;

        /// <summary>
        /// 动作参数（JSON）
        /// </summary>
        public string ActionParams;

        /// <summary>
        /// 当前范围内的玩家
        /// </summary>
        public HashSet<long> PlayersInRange;

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive;
    }

    /// <summary>
    /// ECA 配置数据（用于序列化场景配置）
    /// </summary>
    public struct ECAPointConfig
    {
        public string PointId;
        public int PointType;
        public float InteractRange;
        public int[] EventTypes;
        public int[] ConditionTypes;
        public string ConditionParams;
        public int[] ActionTypes;
        public string ActionParams;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
    }
}
```

**ECAEventComponent.cs** (Model/Share)
```csharp
namespace ET
{
    /// <summary>
    /// ECA 事件组件
    /// 管理事件的触发和处理
    /// </summary>
    [ComponentOf(typeof(ECAPointComponent))]
    public class ECAEventComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 事件处理器字典 EventType -> Handler
        /// </summary>
        public Dictionary<int, EntityRef<IECAEventHandler>> EventHandlers;
    }

    /// <summary>
    /// ECA 事件处理器接口
    /// </summary>
    public interface IECAEventHandler
    {
        /// <summary>
        /// 处理事件
        /// </summary>
        ETTask<bool> Handle(Unit player, ECAPointComponent ecaPoint);
    }
}
```

**ECAConditionComponent.cs** (Model/Share)
```csharp
namespace ET
{
    /// <summary>
    /// ECA 条件组件
    /// 管理条件的判断
    /// </summary>
    [ComponentOf(typeof(ECAPointComponent))]
    public class ECAConditionComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 条件检查器字典 ConditionType -> Checker
        /// </summary>
        public Dictionary<int, EntityRef<IECAConditionChecker>> ConditionCheckers;
    }

    /// <summary>
    /// ECA 条件检查器接口
    /// </summary>
    public interface IECAConditionChecker
    {
        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        bool Check(Unit player, ECAPointComponent ecaPoint);
    }
}
```

**ECAActionComponent.cs** (Model/Share)
```csharp
namespace ET
{
    /// <summary>
    /// ECA 动作组件
    /// 管理动作的执行
    /// </summary>
    [ComponentOf(typeof(ECAPointComponent))]
    public class ECAActionComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 动作执行器字典 ActionType -> Executor
        /// </summary>
        public Dictionary<int, EntityRef<IECAActionExecutor>> ActionExecutors;
    }

    /// <summary>
    /// ECA 动作执行器接口
    /// </summary>
    public interface IECAActionExecutor
    {
        /// <summary>
        /// 执行动作
        /// </summary>
        ETTask Execute(Unit player, ECAPointComponent ecaPoint);
    }
}
```

### 3. 服务端 System 层

**ECAPointComponentSystem.cs** (Hotfix/Server)
```csharp
namespace ET.Server
{
    [EntitySystemOf(typeof(ECAPointComponent))]
    public static partial class ECAPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAPointComponent self, ECAPointConfig config)
        {
            self.PointId = config.PointId;
            self.PointType = config.PointType;
            self.InteractRange = config.InteractRange;
            self.EventTypes = new List<int>(config.EventTypes);
            self.ConditionTypes = new List<int>(config.ConditionTypes);
            self.ConditionParams = config.ConditionParams;
            self.ActionTypes = new List<int>(config.ActionTypes);
            self.ActionParams = config.ActionParams;
            self.PlayersInRange = new HashSet<long>();
            self.IsActive = true;

            // 初始化事件、条件、动作组件
            self.AddComponent<ECAEventComponent>();
            self.AddComponent<ECAConditionComponent>();
            self.AddComponent<ECAActionComponent>();
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
            if (!self.IsActive)
            {
                return;
            }

            long playerId = player.Id;
            if (self.PlayersInRange.Contains(playerId))
            {
                return; // 已经在范围内
            }

            self.PlayersInRange.Add(playerId);

            // 检查是否有 OnPlayerEnter 事件
            if (!self.EventTypes.Contains(1)) // 1 = OnPlayerEnter
            {
                return;
            }

            // 检查条件
            if (!self.CheckConditions(player))
            {
                return;
            }

            // 执行动作
            await self.ExecuteActions(player);
        }

        /// <summary>
        /// 玩家离开范围
        /// </summary>
        public static void OnPlayerLeave(this ECAPointComponent self, Unit player)
        {
            long playerId = player.Id;
            self.PlayersInRange.Remove(playerId);

            // 检查是否有 OnPlayerLeave 事件
            if (self.EventTypes.Contains(2)) // 2 = OnPlayerLeave
            {
                // 处理离开逻辑（例如取消撤离）
                self.HandlePlayerLeave(player);
            }
        }

        /// <summary>
        /// 检查所有条件
        /// </summary>
        public static bool CheckConditions(this ECAPointComponent self, Unit player)
        {
            if (self.ConditionTypes.Count == 0)
            {
                return true; // 无条件，直接通过
            }

            ECAConditionComponent conditionComp = self.GetComponent<ECAConditionComponent>();

            foreach (int conditionType in self.ConditionTypes)
            {
                // 根据条件类型执行检查
                bool passed = ECAConditionHelper.Check(conditionType, player, self);
                if (!passed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 执行所有动作
        /// </summary>
        public static async ETTask ExecuteActions(this ECAPointComponent self, Unit player)
        {
            ECAActionComponent actionComp = self.GetComponent<ECAActionComponent>();

            foreach (int actionType in self.ActionTypes)
            {
                // 根据动作类型执行动作
                await ECAActionHelper.Execute(actionType, player, self);
            }
        }

        /// <summary>
        /// 处理玩家离开
        /// </summary>
        private static void HandlePlayerLeave(this ECAPointComponent self, Unit player)
        {
            // 例如：取消撤离
            if (self.PointType == 1) // EvacuationPoint
            {
                PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
                if (evacuation != null && evacuation.EvacuationPointId == self.GetParent<Unit>().Id)
                {
                    evacuation.CancelEvacuation();
                }
            }
        }
    }
}
```

**ECAConditionHelper.cs** (Hotfix/Server)
```csharp
namespace ET.Server
{
    /// <summary>
    /// ECA 条件检查辅助类
    /// </summary>
    public static class ECAConditionHelper
    {
        public static bool Check(int conditionType, Unit player, ECAPointComponent ecaPoint)
        {
            return conditionType switch
            {
                0 => true, // None - 无条件
                1 => CheckHasItem(player, ecaPoint),
                2 => CheckLevel(player, ecaPoint),
                3 => CheckTeamSize(player, ecaPoint),
                4 => CheckTimeRange(player, ecaPoint),
                5 => CheckQuest(player, ecaPoint),
                _ => true
            };
        }

        private static bool CheckHasItem(Unit player, ECAPointComponent ecaPoint)
        {
            // 解析参数
            // var param = JsonHelper.FromJson<HasItemParam>(ecaPoint.ConditionParams);
            // 检查玩家是否有指定物品
            return true;
        }

        private static bool CheckLevel(Unit player, ECAPointComponent ecaPoint)
        {
            // 检查玩家等级
            return true;
        }

        private static bool CheckTeamSize(Unit player, ECAPointComponent ecaPoint)
        {
            // 检查队伍人数
            return true;
        }

        private static bool CheckTimeRange(Unit player, ECAPointComponent ecaPoint)
        {
            // 检查时间范围
            return true;
        }

        private static bool CheckQuest(Unit player, ECAPointComponent ecaPoint)
        {
            // 检查任务状态
            return true;
        }
    }
}
```

**ECAActionHelper.cs** (Hotfix/Server)
```csharp
namespace ET.Server
{
    /// <summary>
    /// ECA 动作执行辅助类
    /// </summary>
    public static class ECAActionHelper
    {
        public static async ETTask Execute(int actionType, Unit player, ECAPointComponent ecaPoint)
        {
            switch (actionType)
            {
                case 1: // StartEvacuation
                    await ExecuteStartEvacuation(player, ecaPoint);
                    break;
                case 2: // SpawnMonster
                    await ExecuteSpawnMonster(player, ecaPoint);
                    break;
                case 3: // OpenContainer
                    await ExecuteOpenContainer(player, ecaPoint);
                    break;
                case 4: // PlayEffect
                    await ExecutePlayEffect(player, ecaPoint);
                    break;
                case 5: // ShowUI
                    await ExecuteShowUI(player, ecaPoint);
                    break;
                case 6: // TeleportPlayer
                    await ExecuteTeleportPlayer(player, ecaPoint);
                    break;
                case 7: // GiveReward
                    await ExecuteGiveReward(player, ecaPoint);
                    break;
            }
        }

        private static async ETTask ExecuteStartEvacuation(Unit player, ECAPointComponent ecaPoint)
        {
            // 解析参数
            var param = MongoHelper.FromJson<EvacuationParam>(ecaPoint.ActionParams);

            // 添加撤离组件
            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                evacuation = player.AddComponent<PlayerEvacuationComponent>();
            }

            evacuation.EvacuationPointId = ecaPoint.GetParent<Unit>().Id;
            evacuation.StartTime = TimeInfo.Instance.ServerNow();
            evacuation.RequiredTime = param.EvacuateTime;
            evacuation.Status = 1; // 撤离中

            Log.Debug($"Player {player.Id} started evacuation, time: {param.EvacuateTime}ms");

            await ETTask.CompletedTask;
        }

        private static async ETTask ExecuteSpawnMonster(Unit player, ECAPointComponent ecaPoint)
        {
            // 生成怪物逻辑
            var param = MongoHelper.FromJson<SpawnMonsterParam>(ecaPoint.ActionParams);
            Log.Debug($"Spawn monster: {param.MonsterId}, count: {param.Count}");
            await ETTask.CompletedTask;
        }

        private static async ETTask ExecuteOpenContainer(Unit player, ECAPointComponent ecaPoint)
        {
            // 打开容器逻辑
            Log.Debug($"Open container for player {player.Id}");
            await ETTask.CompletedTask;
        }

        private static async ETTask ExecutePlayEffect(Unit player, ECAPointComponent ecaPoint)
        {
            // 播放特效逻辑
            await ETTask.CompletedTask;
        }

        private static async ETTask ExecuteShowUI(Unit player, ECAPointComponent ecaPoint)
        {
            // 显示UI逻辑
            await ETTask.CompletedTask;
        }

        private static async ETTask ExecuteTeleportPlayer(Unit player, ECAPointComponent ecaPoint)
        {
            // 传送玩家逻辑
            await ETTask.CompletedTask;
        }

        private static async ETTask ExecuteGiveReward(Unit player, ECAPointComponent ecaPoint)
        {
            // 给予奖励逻辑
            await ETTask.CompletedTask;
        }
    }

    // 动作参数结构体
    public struct EvacuationParam
    {
        public long EvacuateTime;
    }

    public struct SpawnMonsterParam
    {
        public int MonsterId;
        public int Count;
        public float SpawnRadius;
    }
}
```

### 4. 场景加载和初始化

**MapHelper.cs** (Hotfix/Server)
```csharp
namespace ET.Server
{
    public static partial class MapHelper
    {
        /// <summary>
        /// 加载地图的 ECA 配置点
        /// </summary>
        public static void LoadECAPoints(Scene map, string mapName)
        {
            // 从场景配置文件读取 ECA 点数据
            // 场景配置应该在地图加载时从客户端同步或从配置文件读取

            // 示例：假设从配置文件读取
            string configPath = $"Assets/GameRes/Maps/{mapName}/ECAPoints.json";
            // string json = File.ReadAllText(configPath);
            // ECAPointConfig[] configs = JsonHelper.FromJson<ECAPointConfig[]>(json);

            // 临时示例数据
            ECAPointConfig[] configs = new[]
            {
                new ECAPointConfig
                {
                    PointId = "evac_001",
                    PointType = 1, // EvacuationPoint
                    InteractRange = 3f,
                    EventTypes = new[] { 1, 2 }, // OnPlayerEnter, OnPlayerLeave
                    ConditionTypes = new int[0],
                    ConditionParams = "{}",
                    ActionTypes = new[] { 1 }, // StartEvacuation
                    ActionParams = "{\"evacuateTime\": 5000}",
                    PositionX = 100f,
                    PositionY = 0f,
                    PositionZ = 50f
                }
            };

            UnitComponent unitComponent = map.GetComponent<UnitComponent>();

            foreach (var config in configs)
            {
                // 创建 ECA 点 Unit
                Unit ecaUnit = unitComponent.AddChild<Unit, int>(config.PointId.GetHashCode());
                ecaUnit.Position = new float3(config.PositionX, config.PositionY, config.PositionZ);

                // 添加 ECA 组件
                ECAPointComponent ecaComp = ecaUnit.AddComponent<ECAPointComponent, ECAPointConfig>(config);

                Log.Debug($"Loaded ECA point {config.PointId} at position {ecaUnit.Position}");
            }
        }
    }
}
```

### 5. 玩家移动检测

**在玩家移动 System 中添加 ECA 点检测**
```csharp
namespace ET.Server
{
    public static partial class UnitMoveSystem
    {
        public static void CheckECAPoints(this Unit player)
        {
            UnitComponent unitComponent = player.Domain().GetComponent<UnitComponent>();

            foreach (Unit unit in unitComponent.GetAll())
            {
                ECAPointComponent ecaPoint = unit.GetComponent<ECAPointComponent>();
                if (ecaPoint == null || !ecaPoint.IsActive)
                {
                    continue;
                }

                float distance = math.distance(player.Position, unit.Position);
                bool inRange = distance <= ecaPoint.InteractRange;
                bool wasInRange = ecaPoint.PlayersInRange.Contains(player.Id);

                if (inRange && !wasInRange)
                {
                    // 进入范围
                    ecaPoint.OnPlayerEnter(player).Coroutine();
                }
                else if (!inRange && wasInRange)
                {
                    // 离开范围
                    ecaPoint.OnPlayerLeave(player);
                }
            }
        }
    }
}
```

---

## 使用流程

### 1. 在 Unity 场景中配置撤离点

1. 在场景中创建一个空 GameObject，命名为 "EvacuationPoint_001"
2. 添加 `ECAPointConfig` 组件
3. 配置参数：
   - Point Type: EvacuationPoint
   - Point Id: "evac_001"
   - Interact Range: 3
   - Event Types: OnPlayerEnter, OnPlayerLeave
   - Action Types: StartEvacuation
   - Action Params: `{"evacuateTime": 5000}`
4. 保存场景

### 2. 导出场景配置

创建编辑器工具导出场景中的 ECA 配置点：

```csharp
// Editor/ECAPointExporter.cs
[MenuItem("Tools/Export ECA Points")]
public static void ExportECAPoints()
{
    var ecaPoints = FindObjectsOfType<ECAPointConfig>();
    var configs = new List<ECAPointConfig>();

    foreach (var point in ecaPoints)
    {
        configs.Add(new ECAPointConfig
        {
            PointId = point.PointId,
            PointType = (int)point.PointType,
            InteractRange = point.InteractRange,
            EventTypes = point.EventTypes.Select(e => (int)e).ToArray(),
            ConditionTypes = point.ConditionTypes.Select(c => (int)c).ToArray(),
            ConditionParams = point.ConditionParams,
            ActionTypes = point.ActionTypes.Select(a => (int)a).ToArray(),
            ActionParams = point.ActionParams,
            PositionX = point.transform.position.x,
            PositionY = point.transform.position.y,
            PositionZ = point.transform.position.z
        });
    }

    string json = JsonUtility.ToJson(configs, true);
    string sceneName = SceneManager.GetActiveScene().name;
    string path = $"Assets/GameRes/Maps/{sceneName}/ECAPoints.json";
    File.WriteAllText(path, json);

    Debug.Log($"Exported {configs.Count} ECA points to {path}");
}
```

### 3. 运行时加载

地图加载时自动读取配置并生成 ECA 点：

```csharp
// 在地图初始化时调用
MapHelper.LoadECAPoints(mapScene, "Map001");
```

### 4. 运行效果

- 玩家走进撤离点 3 米范围内
- 自动触发撤离，开始 5 秒倒计时
- 如果玩家离开范围，撤离取消
- 倒计时完成后，触发撤离成功

---

## 扩展示例

### 刷怪点配置

在场景中配置刷怪点：

```
Point Type: SpawnPoint
Point Id: "spawn_001"
Interact Range: 10
Event Types: OnPlayerEnter
Condition Types: None
Action Types: SpawnMonster
Action Params: {"monsterId": 10001, "count": 5, "spawnRadius": 5}
```

### 宝箱配置

```
Point Type: Container
Point Id: "chest_001"
Interact Range: 2
Event Types: OnPlayerInteract
Condition Types: HasItem
Condition Params: {"itemId": 1001}
Action Types: OpenContainer, GiveReward
Action Params: {"rewardId": 2001}
```

---

## 开发计划

### 第1步：基础框架（1-2天）
- [ ] 创建 cn.etetet.eca 包
- [ ] 实现 ECAPointComponent 和基础 System
- [ ] 实现事件、条件、动作的基础接口

### 第2步：Unity 配置层（1天）
- [ ] 创建 ECAPointConfig MonoBehaviour
- [ ] 实现编辑器可视化（Gizmos）
- [ ] 创建导出工具

### 第3步：核心功能实现（2-3天）
- [ ] 实现撤离点功能
- [ ] 实现刷怪点功能
- [ ] 实现容器功能
- [ ] 实现玩家范围检测

### 第4步：测试（1-2天）
- [ ] 编写测试用例
- [ ] 场景测试
- [ ] 性能优化

---

## 优势

✅ **可视化配置**：策划可以直接在场景中配置，无需写代码
✅ **通用框架**：撤离点、刷怪点、宝箱等都基于同一套系统
✅ **易于扩展**：新增功能只需添加新的 Action/Condition 类型
✅ **性能优化**：可以使用 AOI 优化范围检测
✅ **热更新支持**：逻辑在 Hotfix 层，支持热更新
