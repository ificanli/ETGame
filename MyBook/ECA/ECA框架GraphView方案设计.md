# ECA 框架配置方式对比与 GraphView 方案

## 三种配置方式对比

### 1. Inspector 配置方式（Unity MonoBehaviour）

**实现方式**：
- 在场景中放置 GameObject
- 挂载 MonoBehaviour 组件
- 在 Inspector 面板配置参数

**优点**：
- ✅ 简单直观，上手快
- ✅ 场景中直接可见
- ✅ Gizmos 可视化范围
- ✅ 适合简单配置

**缺点**：
- ❌ 复杂逻辑配置困难（大量字段）
- ❌ 状态流转不直观
- ❌ 条件和动作组合难以表达
- ❌ 配置分散在多个场景
- ❌ 难以复用配置

**适用场景**：
- 简单的触发器
- 位置标记
- 基础参数配置

---

### 2. Excel 表格配置方式

**实现方式**：
- 设计多张关联表格
- 配置状态、行为、条件、动作
- 导出为配置文件

**优点**：
- ✅ 批量管理方便
- ✅ 数据导入导出容易
- ✅ 适合大量配置
- ✅ 版本控制友好
- ✅ 可以用公式和脚本处理

**缺点**：
- ❌ 不直观，需要理解表格关联
- ❌ 状态流转需要脑补
- ❌ 调试困难，跨表查找
- ❌ 策划学习成本高
- ❌ 容易配置错误

**适用场景**：
- 大量重复配置
- 数据驱动的系统
- 需要批量修改的场景

---

### 3. GraphView 可视化编辑器（推荐）

**实现方式**：
- 使用 Unity GraphView 创建可视化编辑器
- 节点表示状态、条件、动作
- 连线表示流转关系
- 保存为 ScriptableObject 或 JSON

**优点**：
- ✅ **可视化流程图**，一目了然
- ✅ **状态流转清晰**，连线表示关系
- ✅ **节点组合灵活**，拖拽即可
- ✅ **调试方便**，可以高亮执行路径
- ✅ **策划友好**，无需理解代码
- ✅ **可复用**，可以保存为模板
- ✅ **实时预览**，所见即所得

**缺点**：
- ⚠️ 开发成本较高（需要实现编辑器）
- ⚠️ 需要学习 GraphView API

**适用场景**：
- 复杂的状态机
- 有条件分支的逻辑
- 需要可视化调试的系统
- **ECA 框架（完美匹配）**

---

## 为什么 GraphView 最适合 ECA 框架

### 1. ECA 本质是状态机 + 流程图

```
[状态A] --事件--> [条件检查] --满足--> [执行动作] --> [状态B]
                      |
                      --不满足--> [保持状态A]
```

这种结构天然适合用图形化表示。

### 2. 参考项目中的 BehaviorTree

你的项目中已经有 BehaviorTree 编辑器，说明团队已经熟悉 GraphView 的使用。ECA 框架可以复用这套经验。

### 3. 对比 BehaviorTree 和 ECA

| 特性 | BehaviorTree | ECA 框架 |
|------|-------------|---------|
| 核心概念 | 行为节点树 | 状态 + 事件 + 条件 + 动作 |
| 执行方式 | 从根节点遍历 | 事件触发状态转换 |
| 适用场景 | AI 逻辑、技能系统 | 交互物、触发器 |
| 可视化 | ✅ 树形结构 | ✅ 状态图 |

两者都适合用 GraphView，但结构不同。

---

## GraphView ECA 编辑器设计方案

### 1. 节点类型设计

#### StateNode（状态节点）

```
┌─────────────────┐
│   [状态名称]     │
│                 │
│ 初始状态: ☑     │
│ 可重置: ☐       │
│                 │
│ ○ 进入端口      │
│ ○ 退出端口      │
└─────────────────┘
```

**属性**：
- 状态名称
- 是否初始状态
- 是否可重置
- 重置冷却时间

#### EventNode（事件节点）

```
┌─────────────────┐
│  [事件类型]      │
│                 │
│ ▼ OnPlayerEnter │
│                 │
│ ○ 输出端口      │
└─────────────────┘
```

**事件类型**：
- OnPlayerEnter（玩家进入）
- OnPlayerLeave（玩家离开）
- OnPlayerInteract（玩家交互）
- OnTimerTick（定时触发）
- OnMonsterDead（怪物死亡）
- 自定义事件...

#### ConditionNode（条件节点）

```
┌─────────────────┐
│  [条件检查]      │
│                 │
│ ▼ HasItem       │
│ ItemId: 1001    │
│                 │
│ ○ True 端口     │
│ ○ False 端口    │
└─────────────────┘
```

**条件类型**：
- HasItem（拥有物品）
- LevelCheck（等级检查）
- StateCheck（状态检查）
- TimeRange（时间范围）
- 自定义条件...

#### ActionNode（动作节点）

```
┌─────────────────┐
│  [执行动作]      │
│                 │
│ ▼ OpenContainer │
│ SearchTime: 3s  │
│                 │
│ ○ 完成端口      │
└─────────────────┘
```

**动作类型**：
- StartEvacuation（开始撤离）
- SpawnMonster（生成怪物）
- OpenContainer（打开容器）
- ShowUI（显示UI）
- PlayEffect（播放特效）
- 自定义动作...

#### GroupNode（组合节点）

```
┌─────────────────┐
│  [动作组]        │
│                 │
│ ├─ Action1      │
│ ├─ Action2      │
│ └─ Action3      │
│                 │
│ ○ 完成端口      │
└─────────────────┘
```

用于组合多个动作顺序执行。

---

### 2. 连线规则

**状态流转连线**：
```
[状态A] --事件--> [条件] --True--> [动作] --> [状态B]
```

**端口连接规则**：
- StateNode 的退出端口 → EventNode 的输入端口
- EventNode 的输出端口 → ConditionNode 的输入端口
- ConditionNode 的 True 端口 → ActionNode 的输入端口
- ActionNode 的完成端口 → StateNode 的进入端口

**连线颜色**：
- 绿色：正常流转
- 红色：错误/失败分支
- 蓝色：条件分支
- 黄色：调试高亮

---

### 3. 编辑器界面设计

```
┌─────────────────────────────────────────────────────────┐
│  ECA Editor - Container_001                    [保存]   │
├──────────┬──────────────────────────────────────────────┤
│          │                                              │
│  节点库   │              画布区域                         │
│          │                                              │
│ 状态节点  │    ┌─────┐                                   │
│ 事件节点  │    │Closed│                                  │
│ 条件节点  │    └──┬──┘                                   │
│ 动作节点  │       │ OnPlayerInteract                     │
│          │       ↓                                      │
│          │    ┌─────────┐                               │
│          │    │StateCheck│                              │
│          │    └──┬───┬──┘                               │
│          │       │   │                                  │
│          │    True  False                               │
│          │       ↓                                      │
│          │    ┌──────────┐                              │
│          │    │OpenContainer│                           │
│          │    └─────┬────┘                              │
│          │          ↓                                   │
│          │       ┌─────┐                                │
│          │       │Opened│                               │
│          │       └─────┘                                │
│          │                                              │
├──────────┴──────────────────────────────────────────────┤
│  属性面板                                                │
│  ┌────────────────────────────────────────────────┐    │
│  │ 节点: OpenContainer                             │    │
│  │ SearchTime: 3000ms                             │    │
│  │ ShowUI: true                                   │    │
│  └────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

**界面分区**：
1. **顶部工具栏**：保存、导出、调试、帮助
2. **左侧节点库**：拖拽节点到画布
3. **中间画布区**：编辑状态流转图
4. **右侧属性面板**：编辑选中节点的属性
5. **底部调试面板**：运行时显示执行路径

---

### 4. 容器示例（GraphView 配置）

#### 容器类型1（点击打开）

```
[Closed] --OnPlayerEnter--> [ShowUI("打开容器")]
    |
    --OnPlayerInteract--> [StateCheck(Closed)] --True--> [Opening]
                                                            |
                                                            --StartTimer(3s)
                                                            |
                                                            --OnTimerComplete--> [Opened]
                                                                                    |
[Opened] --OnPlayerInteract--> [ShowContainerUI(no search)]
```

**可视化图**：
```
┌────────┐
│ Closed │
└───┬────┘
    │ OnPlayerEnter
    ↓
┌──────────────┐
│ ShowUI       │
│ "打开容器"    │
└──────────────┘

┌────────┐
│ Closed │
└───┬────┘
    │ OnPlayerInteract
    ↓
┌──────────────┐
│ StateCheck   │
│ State=Closed │
└───┬──────────┘
    │ True
    ↓
┌──────────────┐
│ Opening      │
└───┬──────────┘
    │ StartTimer(3s)
    ↓
┌──────────────┐
│ Opened       │
└──────────────┘
```

#### 容器类型2（自动触发）

```
[Closed] --OnPlayerEnter--> [StateCheck(Closed)] --True--> [StartTimer(5s) + ShowProgressBar]
                                                                |
                                                                --OnTimerComplete--> [SpawnLoot] --> [Opened]
```

---

### 5. 撤离点示例（GraphView 配置）

#### 时间触发型撤离点

```
[Hidden] --OnBattleTimeReach(300s)--> [Available]
    |
    --OnPlayerEnter--> [StartEvacuation(10s) + ShowUI]
                            |
                            --OnTimerComplete--> [Evacuated]
                            |
                            --OnPlayerLeave--> [Available] (取消撤离)
```

**可视化图**：
```
┌────────┐
│ Hidden │
└───┬────┘
    │ OnBattleTimeReach(300s)
    ↓
┌───────────┐
│ Available │
└─────┬─────┘
      │ OnPlayerEnter
      ↓
┌─────────────────┐
│ StartEvacuation │
│ Time: 10s       │
│ ShowUI: true    │
└────┬────────────┘
     │ OnTimerComplete
     ↓
┌───────────┐
│ Evacuated │
└───────────┘
```

#### 条件撤离型

```
[Available] --OnPlayerEnter--> [ConditionCheck] --True--> [StartEvacuation(10s)]
                                    |                           |
                                    --False--> [ShowUI("条件不满足")]
                                                                |
                                                                --OnTimerComplete--> [Evacuated]
```

**条件节点**：
```
┌──────────────────┐
│ ConditionCheck   │
│                  │
│ OR:              │
│ ├─ Weight < 50   │
│ └─ Kills > 5     │
└──┬───────────┬───┘
   │           │
  True       False
```

---

### 6. 任务点示例（GraphView 配置）

#### 刷怪任务

```
[Idle] --OnPlayerEnter--> [ShowUI("接取任务")]
    |
    --OnPlayerAccept--> [Active]
                          |
                          --SpawnMonsters(count=10)
                          |
                          --TrackKills
                          |
                          --OnAllKilled--> [Completed]
                                              |
                                              --GiveReward
```

**可视化图**：
```
┌──────┐
│ Idle │
└──┬───┘
   │ OnPlayerEnter
   ↓
┌────────────────┐
│ ShowUI         │
│ "接取任务"      │
└────────────────┘

┌──────┐
│ Idle │
└──┬───┘
   │ OnPlayerAccept
   ↓
┌────────────────┐
│ Active         │
└──┬─────────────┘
   │
   ├─ SpawnMonsters(10)
   │
   ├─ TrackKills
   │
   │ OnAllKilled
   ↓
┌────────────────┐
│ Completed      │
└──┬─────────────┘
   │
   └─ GiveReward
```

---

### 7. 数据保存格式

#### ScriptableObject 方式（推荐）

```csharp
[CreateAssetMenu(fileName = "ECAConfig", menuName = "ET/ECA Config")]
public class ECAConfig : ScriptableObject
{
    public string ConfigId;
    public Vector3 Position;
    public float InteractRange;

    public List<StateNodeData> States;
    public List<EventNodeData> Events;
    public List<ConditionNodeData> Conditions;
    public List<ActionNodeData> Actions;
    public List<ConnectionData> Connections;
}

[Serializable]
public class StateNodeData
{
    public string StateId;
    public string StateName;
    public bool IsInitial;
    public bool IsResetable;
    public long ResetCooldown;
    public Vector2 Position; // 编辑器中的位置
}

[Serializable]
public class ConnectionData
{
    public string FromNodeId;
    public string FromPortName;
    public string ToNodeId;
    public string ToPortName;
}
```

#### JSON 导出格式

```json
{
  "configId": "container_001",
  "position": {"x": 100, "y": 0, "z": 50},
  "interactRange": 3.0,
  "states": [
    {
      "stateId": "closed",
      "stateName": "Closed",
      "isInitial": true,
      "isResetable": false
    },
    {
      "stateId": "opened",
      "stateName": "Opened",
      "isInitial": false,
      "isResetable": false
    }
  ],
  "events": [
    {
      "eventId": "evt_001",
      "eventType": "OnPlayerInteract",
      "fromState": "closed"
    }
  ],
  "conditions": [
    {
      "conditionId": "cond_001",
      "conditionType": "StateCheck",
      "params": {"allowedStates": ["closed"]}
    }
  ],
  "actions": [
    {
      "actionId": "act_001",
      "actionType": "OpenContainer",
      "params": {"searchTime": 3000}
    }
  ],
  "connections": [
    {
      "from": "closed",
      "fromPort": "exit",
      "to": "evt_001",
      "toPort": "input"
    },
    {
      "from": "evt_001",
      "fromPort": "output",
      "to": "cond_001",
      "toPort": "input"
    }
  ]
}
```

---

### 8. 调试功能

#### 运行时可视化

```
编辑器中实时显示：
- 当前状态（高亮显示）
- 触发的事件（闪烁）
- 条件判断结果（True/False 显示）
- 执行的动作（动画效果）
```

#### 调试面板

```
┌─────────────────────────────────┐
│ 调试信息                         │
├─────────────────────────────────┤
│ 当前状态: Opened                 │
│ 上次触发: OnPlayerInteract       │
│ 触发时间: 12:34:56              │
│ 触发者: Player_001              │
│                                 │
│ 状态历史:                        │
│ ├─ Closed → Opening (12:34:50)  │
│ ├─ Opening → Opened (12:34:53)  │
│ └─ Opened (当前)                │
└─────────────────────────────────┘
```

---

### 9. 开发计划

#### 第1阶段：基础框架（1周）
- [ ] 创建 GraphView 编辑器窗口
- [ ] 实现基础节点类型（State, Event, Condition, Action）
- [ ] 实现节点连接逻辑
- [ ] 实现数据保存和加载

#### 第2阶段：节点库（1周）
- [ ] 实现所有事件类型节点
- [ ] 实现所有条件类型节点
- [ ] 实现所有动作类型节点
- [ ] 实现节点属性编辑面板

#### 第3阶段：运行时支持（1周）
- [ ] 实现配置导出
- [ ] 实现服务端加载逻辑
- [ ] 实现运行时执行
- [ ] 集成到 ECA 框架

#### 第4阶段：调试功能（3-5天）
- [ ] 实现运行时可视化
- [ ] 实现调试面板
- [ ] 实现状态历史记录
- [ ] 实现断点功能

#### 第5阶段：优化和工具（3-5天）
- [ ] 实现模板系统
- [ ] 实现配置复用
- [ ] 实现批量导出
- [ ] 编写使用文档

**总计：约 3-4 周**

---

## 总结

### 为什么选择 GraphView

1. **可视化**：状态流转一目了然
2. **直观**：策划无需理解代码
3. **灵活**：节点组合方式多样
4. **调试方便**：实时可视化执行路径
5. **可复用**：配置可以保存为模板
6. **团队熟悉**：项目已有 BehaviorTree 编辑器经验

### 与其他方式的结合

- **Inspector**：用于场景中放置标记点（位置）
- **GraphView**：用于配置详细逻辑（状态流转）
- **Excel**：用于批量数据（可选，作为补充）

### 最佳实践

1. 在场景中放置 `ECAPointMarker`（只配置位置和 ID）
2. 在 GraphView 编辑器中配置详细逻辑
3. 保存为 ScriptableObject
4. 导出为 JSON 配置
5. 服务端加载并执行

这样既有可视化的优势，又保留了配置的灵活性。
