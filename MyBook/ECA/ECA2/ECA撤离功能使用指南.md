# ECA 撤离功能使用指南

## 一、已完成的功能

### ✅ 核心框架（cn.etetet.eca）
1. ECAManagerComponent - 管理所有 ECA 点
2. ECALoader - 从场景加载 ECA 点
3. ECAHelper - 范围检测辅助类
4. ECAPointComponentSystem - ECA 点系统（支持进入/离开事件）

### ✅ 撤离节点（cn.etetet.ecanode）
1. PlayerEvacuationComponent - 玩家撤离组件
2. PlayerEvacuationComponentSystem - 撤离系统（倒计时、范围检测）
3. PlayerEvacuationTimer - 每帧更新 Timer

## 二、使用流程

### 步骤1：在 Unity 场景中配置撤离点

1. **创建 ECAConfigAsset**：
   - 右键 Project → Create → ET → ECA Config
   - 配置参数：
     - Config Id: "evac_001"
     - Point Type: 1 (撤离点)
     - Interact Range: 5.0

2. **在场景中放置标记**：
   - 创建 GameObject，命名为 "EvacuationPoint_001"
   - 添加 ECAPointMarker 组件
   - 关联刚创建的 ECAConfigAsset
   - 调整位置和范围

### 步骤2：地图加载时加载 ECA 点

在地图初始化代码中添加：

```csharp
// 客户端收集配置
List<ECAConfig> configs = ET.Client.ECASceneHelper.CollectECAConfigs();

// 服务器加载 ECA 点
ECALoader.LoadECAPoints(mapScene, configs);
```

**建议位置**：
- `cn.etetet.map` 包的地图初始化 System
- 或者在 EnterMapFinish 事件处理中

**注意**：
- `ECASceneHelper.CollectECAConfigs()` 在客户端调用（可以访问 UnityEngine）
- `ECALoader.LoadECAPoints()` 在服务器调用（不依赖 UnityEngine）

### 步骤3：玩家移动时检测范围

在 MoveTimer 中添加范围检测：

```csharp
[Invoke(TimerInvokeType.MoveTimer)]
public class MoveTimer: ATimer<MoveComponent>
{
    protected override void Run(MoveComponent self)
    {
        try
        {
            self.MoveForward(true);

            // 添加 ECA 范围检测
            Unit unit = self.GetParent<Unit>();
            ECAHelper.CheckPlayerInRange(unit);
        }
        catch (Exception e)
        {
            Log.Error($"move timer error: {self.Id}\\n{e}");
        }
    }
}
```

**文件位置**：`D:\05ET\etowo\Packages\cn.etetet.move\Scripts\Hotfix\Share\MoveComponentSystem.cs`

### 步骤4：实现跳转回 Lobby（TODO）

在 `PlayerEvacuationComponentSystem.CompleteEvacuation` 方法中（第95行）：

```csharp
// 当前是临时方案，只打印日志
// 需要替换为实际的跳转逻辑：
await TransferHelper.TransferAtFrameFinish(player, "Lobby", lobbyMapId);
```

**需要确定**：
- Lobby 的 SceneType 值
- Lobby 的 MapId

## 三、测试流程

### 1. 启动游戏，进入地图

查看日志，应该看到：
```
[ECALoader] Found 1 ECA point markers in scene
[ECALoader] Loaded ECA point: evac_001, Type: 1, Pos: (x, y, z)
[ECALoader] Total loaded 1 ECA points
```

### 2. 走到撤离点附近

当进入5米范围内，应该看到：
```
[ECAPoint] Player 123456 entered ECA point: evac_001
[PlayerEvacuation] Player 123456 started evacuation, time: 10000ms
```

### 3. 等待10秒

倒计时完成后：
```
[PlayerEvacuation] Player 123456 evacuation completed, transferring to lobby
[PlayerEvacuation] Transfer to lobby not implemented yet. Player 123456 should be transferred now.
```

### 4. 测试取消撤离

在倒计时期间离开范围：
```
[ECAPoint] Player 123456 left ECA point: evac_001
[PlayerEvacuation] Player 123456 evacuation cancelled (left range)
```

## 四、调试技巧

### 查看 ECA 点是否加载成功

```csharp
Scene scene = ...;
ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
Log.Info($"Total ECA points: {ecaManager.ECAPoints.Count}");
```

### 查看玩家是否在撤离中

```csharp
Unit player = ...;
PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
if (evacuation != null)
{
    long elapsed = TimeInfo.Instance.ServerNow() - evacuation.StartTime;
    float progress = (float)elapsed / evacuation.RequiredTime * 100f;
    Log.Info($"Evacuation progress: {progress:F1}%");
}
```

### 常见问题

**Q: 看不到 ECA 点加载日志？**
- 检查是否调用了 `ECALoader.LoadECAPoints(mapScene)`
- 检查场景中是否有 ECAPointMarker 组件
- 检查 ECAPointMarker 是否关联了 ECAConfigAsset

**Q: 进入范围没有反应？**
- 检查是否添加了 `ECAHelper.CheckPlayerInRange(unit)` 调用
- 检查 MoveTimer 是否正常运行
- 检查玩家位置和 ECA 点位置的距离

**Q: 撤离倒计时不工作？**
- 检查 PlayerEvacuationTimer 是否注册
- 检查 TimerInvokeType.PlayerEvacuationTimer 是否定义
- 检查日志是否有 Timer 错误

## 五、下一步工作

### 必须完成（才能真正测试）

1. **集成到地图加载**：
   - 找到地图初始化代码
   - 添加 `ECALoader.LoadECAPoints(mapScene)`

2. **集成到玩家移动**：
   - 修改 MoveTimer
   - 添加 `ECAHelper.CheckPlayerInRange(unit)`

3. **实现 Lobby 跳转**：
   - 找到 Lobby 的 SceneType 和 MapId
   - 替换 CompleteEvacuation 中的 TODO

### 可选扩展

1. **UI 显示**：
   - 撤离进度条
   - 倒计时显示
   - 提示文字

2. **音效和特效**：
   - 进入范围提示音
   - 撤离完成特效

3. **其他节点类型**：
   - 刷怪点
   - 容器
   - 传送点

---

**最后更新**：2026-02-26 21:30
**状态**：核心功能已实现，等待集成测试
