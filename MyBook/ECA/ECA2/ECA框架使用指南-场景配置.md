# ECA 框架使用指南 - 场景配置流程

## 配置流程

### 第一步：在场景中创建撤离点

1. **在 Unity 场景中创建 GameObject**
   - 在 Hierarchy 中右键 → Create Empty
   - 命名为 "EvacuationPoint_001"
   - 移动到你想要的位置（例如：100, 0, 50）

2. **添加 ECAPointMarker 组件**
   - 选中 GameObject
   - 在 Inspector 中点击 "Add Component"
   - 搜索 "ECAPointMarker"
   - 添加组件

3. **配置参数**
   - **Config**: 关联 ECAConfig（见第二步）
   - **Interact Range**: 5（交互范围5米）
   - **Show Range**: ✓（显示范围）
   - **Range Color**: 绿色

### 第二步：创建 ECA 配置（临时方案）

**注意**：由于 GraphView 编辑器还未实现，暂时使用代码方式创建配置。

在 Unity 中创建一个临时脚本：

```csharp
using UnityEngine;
using ET;

public class CreateTestECAConfig : MonoBehaviour
{
    [ContextMenu("Create Test Evacuation Config")]
    void CreateConfig()
    {
        // 创建配置
        ECAConfig config = ScriptableObject.CreateInstance<ECAConfig>();
        config.ConfigId = "evac_001";
        config.PointType = 1; // EvacuationPoint
        config.InteractRange = 5f;
        config.InitialState = 0; // Available
        config.IsResetable = false;

        // 创建状态节点
        ECAStateNode availableState = new ECAStateNode
        {
            Id = 1,
            StateName = "Available",
            IsInitialState = true,
            IsResetable = false
        };

        ECAStateNode evacuatedState = new ECAStateNode
        {
            Id = 2,
            StateName = "Evacuated",
            IsInitialState = false,
            IsResetable = false
        };

        // 创建事件节点
        ECAEventNode enterEvent = new ECAEventNode
        {
            Id = 3,
            EventType = 1, // OnPlayerEnter
            EventParams = "{}"
        };

        // 创建条件节点
        ECAConditionNode stateCheck = new ECAConditionNode
        {
            Id = 4,
            ConditionType = 3, // StateCheck
            ConditionParams = "{\"allowedStates\": [0]}"
        };

        // 创建动作节点
        ECAActionNode startEvacuation = new ECAActionNode
        {
            Id = 5,
            ActionType = 1, // StartEvacuation
            ActionParams = "{\"evacuateTime\": 10000}" // 10秒
        };

        // 添加节点
        config.Nodes.Add(availableState);
        config.Nodes.Add(evacuatedState);
        config.Nodes.Add(enterEvent);
        config.Nodes.Add(stateCheck);
        config.Nodes.Add(startEvacuation);

        // 创建连接
        config.Connections.Add(new ECAConnection
        {
            FromNodeId = 1, // Available
            FromPortName = "exit",
            ToNodeId = 3, // OnPlayerEnter
            ToPortName = "input"
        });

        config.Connections.Add(new ECAConnection
        {
            FromNodeId = 3, // OnPlayerEnter
            FromPortName = "output",
            ToNodeId = 4, // StateCheck
            ToPortName = "input"
        });

        config.Connections.Add(new ECAConnection
        {
            FromNodeId = 4, // StateCheck
            FromPortName = "True",
            ToNodeId = 5, // StartEvacuation
            ToPortName = "input"
        });

        config.Connections.Add(new ECAConnection
        {
            FromNodeId = 5, // StartEvacuation
            FromPortName = "output",
            ToNodeId = 2, // Evacuated
            ToPortName = "input"
        });

        // 保存为 Asset
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(config, "Assets/ECAConfigs/evac_001.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("ECA Config created at: Assets/ECAConfigs/evac_001.asset");
#endif
    }
}
```

**使用方法**：
1. 创建 `Assets/ECAConfigs` 文件夹
2. 创建上述脚本并挂到任意 GameObject 上
3. 右键组件 → "Create Test Evacuation Config"
4. 会在 `Assets/ECAConfigs/evac_001.asset` 生成配置

### 第三步：关联配置到场景标记

1. 选中场景中的 "EvacuationPoint_001" GameObject
2. 在 Inspector 中找到 ECAPointMarker 组件
3. 将 `evac_001.asset` 拖到 "Config" 字段
4. 保存场景

### 第四步：在地图加载时加载 ECA 点

找到地图初始化代码（例如 `MapHelper.cs` 或地图加载相关的 System），添加：

```csharp
// 在地图加载完成后
ECALoader.LoadECAPoints(mapScene, mapName);
```

**示例位置**：
- 可能在 `cn.etetet.map` 包中
- 可能在地图的 `Awake` 或 `Start` 方法中
- 搜索关键字：`MapComponent`、`MapHelper`、地图初始化

### 第五步：添加玩家移动检测

找到玩家移动相关的 System，添加 ECA 点检测：

```csharp
// 在玩家移动更新中（例如 MoveComponentSystem.cs）
public static void Update(this MoveComponent self)
{
    // ... 原有移动逻辑 ...

    // 检查 ECA 点
    Unit player = self.GetParent<Unit>();
    CheckECAPoints(player);
}

private static void CheckECAPoints(Unit player)
{
    UnitComponent unitComponent = player.Domain().GetComponent<UnitComponent>();
    if (unitComponent == null) return;

    foreach (Unit unit in unitComponent.GetAll())
    {
        ECAPointComponent ecaPoint = unit.GetComponent<ECAPointComponent>();
        if (ecaPoint == null || !ecaPoint.IsActive)
        {
            continue;
        }

        float distance = Unity.Mathematics.math.distance(player.Position, unit.Position);
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
```

### 第六步：集成跳转回 Lobby

找到 `PlayerEvacuationComponentSystem.cs` 的第 58 行，替换 TODO：

```csharp
// 原代码（第58行）
// TODO: 跳转回 lobby
// 示例：await MapHelper.TransferToLobby(player);

// 替换为你的实际跳转逻辑
// 例如：
await TransferHelper.Transfer(player, SceneType.Lobby);
// 或者：
await MapHelper.ExitMap(player);
```

---

## 测试流程

1. **启动游戏，进入地图**
2. **走到撤离点附近**（100, 0, 50）
3. **进入5米范围内**
   - 应该看到日志：`Player {id} started evacuation, time: 10000ms`
4. **等待10秒**
   - 可以看到撤离进度（如果有UI）
5. **撤离完成**
   - 应该看到日志：`Player {id} evacuation completed, returning to lobby`
   - 跳转回 Lobby

---

## 调试技巧

### 查看日志

关键日志位置：
- `ECALoader.LoadECAPoints`: 加载了多少个 ECA 点
- `ECAPointComponentSystem.OnPlayerEnter`: 玩家进入撤离点
- `ECAActionHelper.ExecuteStartEvacuation`: 开始撤离
- `PlayerEvacuationComponentSystem.CompleteEvacuation`: 撤离完成

### 检查状态

在 Unity 编辑器中：
1. 运行游戏
2. 在 Hierarchy 中找到撤离点 GameObject
3. 查看 ECAPointMarker 组件的参数
4. 确认 Config 已关联

### 常见问题

**问题1：看不到撤离点范围**
- 检查 ECAPointMarker 的 "Show Range" 是否勾选
- 检查 Scene 视图是否开启 Gizmos

**问题2：进入范围没有反应**
- 检查是否调用了 `ECALoader.LoadECAPoints`
- 检查日志是否有 "Loading ECA points" 信息
- 检查是否添加了玩家移动检测代码

**问题3：撤离倒计时不工作**
- 检查 `PlayerEvacuationComponent` 的 `Status` 是否为 1
- 检查 `Update` 方法是否被调用
- 检查玩家是否在范围内（距离 <= 5米）

**问题4：撤离完成后没有跳转**
- 检查 `PlayerEvacuationComponentSystem.cs` 第58行的 TODO 是否已替换
- 检查跳转逻辑是否正确

---

## 场景可视化

在 Scene 视图中，你应该能看到：
- 绿色的线框球体（撤离点范围）
- 黄色的标签显示配置ID
- 选中时高亮显示

---

## 下一步

等 GraphView 编辑器实现后，你就可以：
1. 双击 ECAConfig 打开可视化编辑器
2. 拖拽节点配置状态流转
3. 可视化调试执行路径

但现在你已经可以测试基本的撤离功能了！

