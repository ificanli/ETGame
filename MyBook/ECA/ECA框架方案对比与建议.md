# ECA 框架方案对比与建议

## 你的实际需求分析

### 搜打撤游戏的交互物类型

#### 1. 搜索类容器
**容器类型1**：
- 靠近 x 米显示"打开容器"按钮
- 点击后显示容器+背包 UI
- 搜索 x 秒后可拾取物品
- 已打开后可再次打开，但不显示搜索过程

**容器类型2**：
- 进入 x 米自动触发
- 显示计时条
- 时间到后物品掉落地上
- 已打开后不可再次触发

#### 2. 撤离点（4种类型）
1. **时间触发型**：战斗 x 秒后出现
2. **任务触发型**：完成局内任务后出现
3. **条件撤离型**：背包重量 < x 或击杀 > x 人
4. **拉闸撤离型**：拉闸后地图倒计时，时间到所有人可撤离

#### 3. 地图任务点（4种任务）
1. 刷怪任务：接取后刷怪，击杀获得奖励
2. 占领任务：在区域内占领 x 秒，全图广播
3. 护送任务：护送目标到指定地点
4. 击杀任务：击杀指定 Boss

#### 4. 其他交互物
1. **滑索**：点击滑到另一边
2. **草丛**：进入后隐身

---

## 你的上个项目方案分析

### 核心设计思想

> "可交互物就像一堆'状态'盒子的集合。触发或条件的达成会让可交互物在各个状态盒子中切换。"

### 表格结构

1. **CommonInteractor**（交互物基础信息）
   - 交互物 ID
   - 状态列表
   - 初始状态
   - 玩家交互次数
   - 静态模型、特效等

2. **InteractorState**（状态表）
   - 状态 ID
   - 执行的 Behavior 列表
   - 状态流转规则（BehaviorId → NextState）

3. **InteractorEventType**（事件类型表）
   - EventTypeId
   - 程序功能定义

4. **InteractorBehavior**（行为表）
   - BehaviorId
   - TriggerList（触发器列表）
   - ConditionList（条件列表）
   - FailedCD（失败冷却）

### 优点
✅ **配置驱动**：所有逻辑都在配置表中
✅ **状态清晰**：状态和行为分离
✅ **可复用**：Behavior 可以被多个状态引用
✅ **灵活**：状态流转规则可配置

### 缺点
❌ **配置复杂**：需要维护多张表的关联关系
❌ **不直观**：策划需要理解表格之间的引用关系
❌ **调试困难**：问题定位需要跨多张表查找

---

## 我的方案分析

### 核心设计思想

Unity 场景配置 + 服务端 ECA 组件 + 状态管理

### 优点
✅ **可视化**：在 Unity 场景中直接放置和配置
✅ **直观**：Gizmos 显示范围，所见即所得
✅ **易调试**：可以在场景中直接测试
✅ **灵活**：支持 JSON 参数配置

### 缺点
❌ **场景依赖**：需要导出场景配置
❌ **不够通用**：每种类型需要单独实现
❌ **配置分散**：配置在场景中，不便于批量管理

---

## 对比总结

| 维度 | 你的方案（配置表） | 我的方案（场景配置） |
|------|-------------------|---------------------|
| **配置方式** | Excel 表格 | Unity 场景 + MonoBehaviour |
| **可视化** | ❌ 需要工具支持 | ✅ 直接在场景中看到 |
| **灵活性** | ✅ 高度可配置 | ⚠️ 中等 |
| **复杂度** | ⚠️ 多表关联复杂 | ✅ 相对简单 |
| **批量管理** | ✅ 表格便于批量操作 | ❌ 需要逐个场景配置 |
| **调试** | ❌ 跨表查找困难 | ✅ 场景中直接测试 |
| **策划友好** | ⚠️ 需要培训 | ✅ 直观易懂 |
| **热更新** | ✅ 配置表热更新 | ⚠️ 需要导出配置 |

---

## 推荐方案：混合架构

结合两种方案的优点，设计一个混合架构：

### 架构设计

```
Unity 场景配置（位置、范围）
        ↓
    导出工具
        ↓
配置表（Excel）（逻辑、参数）
        ↓
    服务端加载
        ↓
   ECA 组件运行
```

### 具体实现

#### 1. Unity 场景层（简化）

只配置**位置和基础信息**：

```csharp
public class ECAPointMarker : MonoBehaviour
{
    [Header("基础信息")]
    public string PointId = "";           // 配置表 ID
    public float InteractRange = 3f;      // 交互范围

    [Header("可视化")]
    public bool ShowRange = true;
    public Color RangeColor = Color.green;

    // 绘制范围
    private void OnDrawGizmos()
    {
        if (ShowRange)
        {
            Gizmos.color = RangeColor;
            Gizmos.DrawWireSphere(transform.position, InteractRange);
        }
    }
}
```

**导出工具**：
```csharp
[MenuItem("Tools/Export ECA Points")]
public static void ExportECAPoints()
{
    var markers = FindObjectsOfType<ECAPointMarker>();
    var sb = new StringBuilder();

    foreach (var marker in markers)
    {
        // 导出为 CSV 或直接写入 Excel
        sb.AppendLine($"{marker.PointId},{marker.transform.position.x},{marker.transform.position.y},{marker.transform.position.z},{marker.InteractRange}");
    }

    File.WriteAllText("ECAPoints_Export.csv", sb.ToString());
}
```

#### 2. Excel 配置表（详细逻辑）

**CommonInteractor.xlsx**（主表）

| InteractorId | Name | Type | InitialState | StateList | PositionX | PositionY | PositionZ | InteractRange |
|--------------|------|------|--------------|-----------|-----------|-----------|-----------|---------------|
| 10001 | 容器1 | Container1 | Closed | Closed,Opening,Opened | 100.0 | 0.0 | 50.0 | 3.0 |
| 10002 | 容器2 | Container2 | Closed | Closed,Opening,Opened | -100.0 | 0.0 | -50.0 | 5.0 |
| 20001 | 撤离点1 | Evacuation | Available | Available,InUse,Evacuated | 200.0 | 0.0 | 100.0 | 10.0 |

**InteractorState.xlsx**（状态表）

| StateId | InteractorId | StateName | BehaviorList | StateTransitions |
|---------|--------------|-----------|--------------|------------------|
| 1001 | 10001 | Closed | 1,2 | 1→Opening,2→Opened |
| 1002 | 10001 | Opening | 3 | 3→Opened |
| 1003 | 10001 | Opened | 4 | - |

**InteractorBehavior.xlsx**（行为表）

| BehaviorId | Name | EventTypes | ConditionTypes | ConditionParams | ActionTypes | ActionParams |
|------------|------|------------|----------------|-----------------|-------------|--------------|
| 1 | 玩家进入触发 | OnPlayerEnter | StateCheck | {"allowedStates":[0]} | ShowUI | {"uiType":"OpenButton"} |
| 2 | 玩家点击打开 | OnPlayerInteract | StateCheck,HasItem | {"allowedStates":[0],"itemId":1001} | OpenContainer,ChangeState | {"searchTime":3000,"nextState":2} |
| 3 | 搜索完成 | OnTimerTick | None | {} | SpawnLoot,ChangeState | {"lootId":2001,"nextState":3} |

#### 3. 服务端加载

```csharp
namespace ET.Server
{
    public static class ECALoader
    {
        /// <summary>
        /// 加载地图的 ECA 配置点
        /// </summary>
        public static void LoadECAPoints(Scene map, string mapName)
        {
            // 1. 从配置表读取该地图的所有交互物
            var configs = CommonInteractorConfigCategory.Instance.GetByMapName(mapName);

            UnitComponent unitComponent = map.GetComponent<UnitComponent>();

            foreach (var config in configs)
            {
                // 2. 创建 Unit
                Unit ecaUnit = unitComponent.AddChild<Unit, int>(config.InteractorId);
                ecaUnit.Position = new float3(config.PositionX, config.PositionY, config.PositionZ);

                // 3. 添加 ECA 组件
                ECAPointComponent ecaComp = ecaUnit.AddComponent<ECAPointComponent>();
                ecaComp.PointId = config.InteractorId.ToString();
                ecaComp.PointType = config.Type;
                ecaComp.InteractRange = config.InteractRange;
                ecaComp.CurrentState = config.InitialState;
                ecaComp.InitialState = config.InitialState;

                // 4. 加载状态配置
                var states = InteractorStateConfigCategory.Instance.GetByInteractorId(config.InteractorId);
                ECAStateComponent stateComp = ecaComp.AddComponent<ECAStateComponent>();

                foreach (var state in states)
                {
                    // 解析状态转换规则
                    stateComp.LoadStateConfig(state);

                    // 加载行为配置
                    foreach (int behaviorId in state.BehaviorList)
                    {
                        var behavior = InteractorBehaviorConfigCategory.Instance.Get(behaviorId);
                        ecaComp.LoadBehaviorConfig(behavior);
                    }
                }

                Log.Debug($"Loaded ECA point {config.InteractorId} at {ecaUnit.Position}");
            }
        }
    }
}
```

---

## 针对搜打撤游戏的具体建议

### 1. 容器系统

**配置示例**（容器类型1）：

```
InteractorId: 10001
Type: Container1
InitialState: Closed
States:
  - Closed:
      Behavior: OnPlayerEnter → ShowUI("打开容器")
      Transition: OnPlayerInteract → Opening
  - Opening:
      Behavior: StartTimer(3s) → ShowSearchUI
      Transition: OnTimerComplete → Opened
  - Opened:
      Behavior: OnPlayerInteract → ShowContainerUI(no search)
      Transition: None (可重复打开)
```

**配置示例**（容器类型2）：

```
InteractorId: 10002
Type: Container2
InitialState: Closed
States:
  - Closed:
      Behavior: OnPlayerEnter → StartTimer(5s) + ShowProgressBar
      Transition: OnTimerComplete → Opened
  - Opened:
      Behavior: SpawnLoot
      Transition: None (不可重复)
```

### 2. 撤离点系统

**时间触发型**：

```
InteractorId: 20001
Type: EvacuationTime
InitialState: Hidden
States:
  - Hidden:
      Behavior: OnBattleTimeReach(300s) → ChangeState(Available)
      Transition: OnConditionMet → Available
  - Available:
      Behavior: OnPlayerEnter → StartEvacuation(10s)
      Transition: OnEvacuationComplete → Evacuated
```

**条件撤离型**：

```
InteractorId: 20002
Type: EvacuationCondition
InitialState: Available
States:
  - Available:
      Behavior: OnPlayerEnter + CheckCondition(weight<50 OR kills>5) → StartEvacuation(10s)
      Transition: OnEvacuationComplete → Evacuated
```

**拉闸撤离型**：

```
InteractorId: 20003 (拉闸)
Type: EvacuationSwitch
States:
  - Idle:
      Behavior: OnPlayerInteract → ActivateEvacuationPoints + StartMapTimer(120s)

InteractorId: 20004 (撤离点)
Type: EvacuationPoint
InitialState: Disabled
States:
  - Disabled:
      Behavior: OnSwitchActivated → ChangeState(Available)
  - Available:
      Behavior: OnPlayerEnter → StartEvacuation(10s)
```

### 3. 任务点系统

**刷怪任务**：

```
InteractorId: 30001
Type: QuestSpawn
States:
  - Idle:
      Behavior: OnPlayerEnter → ShowUI("接取任务")
      Transition: OnPlayerAccept → Active
  - Active:
      Behavior: SpawnMonsters(count=10) + TrackKills
      Transition: OnAllKilled → Completed
  - Completed:
      Behavior: GiveReward
```

**占领任务**：

```
InteractorId: 30002
Type: QuestOccupy
States:
  - Idle:
      Behavior: OnPlayerEnter → ShowUI("接取任务")
      Transition: OnPlayerAccept → Active
  - Active:
      Behavior: StartOccupyTimer(30s) + BroadcastToAll + CheckPlayerInZone
      Transition: OnTimerComplete → Completed
```

### 4. 其他交互物

**滑索**：

```
InteractorId: 40001
Type: Zipline
States:
  - Idle:
      Behavior: OnPlayerEnter → ShowUI("使用滑索")
      Transition: OnPlayerInteract → InUse
  - InUse:
      Behavior: MovePlayerToTarget(targetPos) + PlayAnimation
      Transition: OnMoveComplete → Idle
```

**草丛**：

```
InteractorId: 40002
Type: Bush
States:
  - Idle:
      Behavior: OnPlayerEnter → AddBuff(Stealth)
      Transition: OnPlayerLeave → Idle (移除Buff)
```

---

## 最终推荐架构

### 工作流程

1. **策划在 Unity 场景中放置标记点**
   - 使用 `ECAPointMarker` 组件
   - 配置位置、范围、ID
   - 可视化查看范围

2. **导出场景配置到 Excel**
   - 运行导出工具
   - 自动生成位置信息到 Excel

3. **策划在 Excel 中配置详细逻辑**
   - 配置状态、行为、条件、动作
   - 可以批量修改和管理

4. **服务端加载配置**
   - 读取 Excel 配置
   - 生成 ECA 组件
   - 运行时执行逻辑

### 优势

✅ **可视化**：场景中直观看到位置和范围
✅ **灵活配置**：Excel 中配置复杂逻辑
✅ **批量管理**：Excel 便于批量操作
✅ **易于调试**：场景中测试位置，表格中调试逻辑
✅ **策划友好**：场景放置简单，表格配置清晰
✅ **热更新**：配置表支持热更新

---

## 开发建议

### 第1步：搭建基础框架（2-3天）
1. 创建 `ECAPointMarker` 组件
2. 实现导出工具
3. 设计 Excel 表格结构
4. 实现服务端加载逻辑

### 第2步：实现容器系统（2天）
1. 容器类型1（点击打开）
2. 容器类型2（自动触发）
3. 测试状态流转

### 第3步：实现撤离点系统（3天）
1. 时间触发型
2. 任务触发型
3. 条件撤离型
4. 拉闸撤离型

### 第4步：实现任务点系统（3-4天）
1. 刷怪任务
2. 占领任务
3. 护送任务
4. 击杀任务

### 第5步：实现其他交互物（1-2天）
1. 滑索
2. 草丛

### 第6步：测试和优化（2-3天）
1. 集成测试
2. 性能优化
3. 工具完善

**总计：约 2-3 周**

---

## 总结

对于搜打撤游戏，推荐使用**混合架构**：
- Unity 场景配置位置和范围（可视化）
- Excel 配置详细逻辑（灵活性）
- 服务端 ECA 组件执行（性能）

这样既保留了你上个项目配置表的灵活性，又增加了场景配置的直观性，是最适合搜打撤游戏的方案。
