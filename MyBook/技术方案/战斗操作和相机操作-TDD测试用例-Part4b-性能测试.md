# TDD测试用例 - Part 4b: 性能测试

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 性能测试
- **优先级**: P2
- **测试用例数**: 4

---

## 1. 多人同屏性能测试

### 测试用例 4b.1: 12人同屏战斗性能

**测试目的**: 验证12人同屏战斗时性能满足要求

**前置条件**:
- 服务端Tick频率30Hz
- BT执行频率10Hz

**测试步骤**:
1. 创建12个Unit（6v6）
2. 所有人都在移动和射击
3. 运行30秒
4. 统计性能指标

**预期结果**:
- 服务端帧率稳定在30Hz
- CPU使用率 < 50%
- 内存无泄漏

**测试代码**:
```csharp
[Test]
public async ETTask Test_12PlayersCombatPerformance()
{
    // Arrange
    List<Unit> allUnits = new List<Unit>();

    // 创建阵营1（6人）
    for (int i = 0; i < 6; i++)
    {
        Unit unit = UnitFactory.Create(scene, i + 1, UnitType.Player);
        unit.Position = new float3(i * 2, 0, 0);
        unit.AddComponent<CampComponent, int>(1);
        SetupCombatUnit(unit, 1001, 1002);

        MoveComponent moveComp = unit.AddComponent<MoveComponent>();
        moveComp.StartJoystickMove();
        moveComp.JoystickDir = new float3(1, 0, 0);

        allUnits.Add(unit);
    }

    // 创建阵营2（6人）
    for (int i = 0; i < 6; i++)
    {
        Unit unit = UnitFactory.Create(scene, i + 7, UnitType.Player);
        unit.Position = new float3(i * 2, 0, 20);
        unit.AddComponent<CampComponent, int>(2);
        SetupCombatUnit(unit, 1001, 1002);

        MoveComponent moveComp = unit.AddComponent<MoveComponent>();
        moveComp.StartJoystickMove();
        moveComp.JoystickDir = new float3(0, 0, -1);

        allUnits.Add(unit);
    }

    // 设置AOI（所有人都能看到所有人）
    foreach (Unit unit in allUnits)
    {
        MockAOI(unit, allUnits.Where(u => u.Id != unit.Id).ToArray());
    }

    // 性能监控
    PerformanceMonitor monitor = new PerformanceMonitor();
    monitor.Start();

    // Act - 运行30秒
    int totalTicks = 30 * 30; // 30秒 * 30Hz
    for (int tick = 0; tick < totalTicks; tick++)
    {
        long tickStart = TimeInfo.Instance.ServerNow();

        // 所有Unit移动
        foreach (Unit unit in allUnits)
        {
            MoveComponent moveComp = unit.GetComponent<MoveComponent>();
            moveComp?.TickJoystickMove();
        }

        // 所有Unit BT（10Hz，每3个Tick执行一次）
        if (tick % 3 == 0)
        {
            foreach (Unit unit in allUnits)
            {
                BTComponent btComp = unit.GetChild<BTComponent>();
                btComp?.Update();
            }
        }

        long tickEnd = TimeInfo.Instance.ServerNow();
        monitor.RecordTick(tickEnd - tickStart);

        await TimerComponent.Instance.WaitAsync(33);
    }

    monitor.Stop();

    // Assert
    Assert.IsTrue(monitor.AverageTickTime < 30,
        $"Average tick time: {monitor.AverageTickTime}ms (should < 30ms)");
    Assert.IsTrue(monitor.MaxTickTime < 50,
        $"Max tick time: {monitor.MaxTickTime}ms (should < 50ms)");
    Assert.IsTrue(monitor.TickTimeStdDev < 10,
        $"Tick time std dev: {monitor.TickTimeStdDev}ms (should < 10ms)");

    Log.Info($"Performance Report:\n" +
             $"  Average Tick: {monitor.AverageTickTime}ms\n" +
             $"  Max Tick: {monitor.MaxTickTime}ms\n" +
             $"  Min Tick: {monitor.MinTickTime}ms\n" +
             $"  Std Dev: {monitor.TickTimeStdDev}ms");
}
```

---

### 测试用例 4b.2: BT执行频率优化验证

**测试目的**: 验证BT限制在10Hz能显著降低CPU消耗

**前置条件**:
- 12个Unit同时战斗

**测试步骤**:
1. 测试BT每帧执行的CPU消耗
2. 测试BT 10Hz执行的CPU消耗
3. 对比差异

**预期结果**:
- 10Hz比每帧执行CPU降低60%以上

**测试代码**:
```csharp
[Test]
public async ETTask Test_BTFrequencyOptimization()
{
    // Arrange
    List<Unit> units = new List<Unit>();
    for (int i = 0; i < 12; i++)
    {
        Unit unit = UnitFactory.Create(scene, i + 1, UnitType.Player);
        unit.AddComponent<CampComponent, int>(i % 2 + 1);
        SetupCombatUnit(unit, 1001, 1002);
        units.Add(unit);
    }

    foreach (Unit unit in units)
    {
        MockAOI(unit, units.Where(u => u.Id != unit.Id).ToArray());
    }

    // Test 1: 每帧执行BT
    PerformanceMonitor monitor1 = new PerformanceMonitor();
    monitor1.Start();

    for (int i = 0; i < 300; i++) // 10秒 * 30fps
    {
        long start = TimeInfo.Instance.ServerNow();

        foreach (Unit unit in units)
        {
            BTComponent btComp = unit.GetChild<BTComponent>();
            btComp.BTTickInterval = 0; // 每帧执行
            btComp.Update();
        }

        long end = TimeInfo.Instance.ServerNow();
        monitor1.RecordTick(end - start);

        await TimerComponent.Instance.WaitAsync(33);
    }

    monitor1.Stop();

    // Test 2: 10Hz执行BT
    PerformanceMonitor monitor2 = new PerformanceMonitor();
    monitor2.Start();

    for (int i = 0; i < 300; i++)
    {
        long start = TimeInfo.Instance.ServerNow();

        foreach (Unit unit in units)
        {
            BTComponent btComp = unit.GetChild<BTComponent>();
            btComp.BTTickInterval = 100; // 10Hz
            btComp.Update();
        }

        long end = TimeInfo.Instance.ServerNow();
        monitor2.RecordTick(end - start);

        await TimerComponent.Instance.WaitAsync(33);
    }

    monitor2.Stop();

    // Assert
    float improvement = (monitor1.AverageTickTime - monitor2.AverageTickTime) /
                        monitor1.AverageTickTime * 100;

    Assert.IsTrue(improvement > 60,
        $"BT frequency optimization improvement: {improvement}% (should > 60%)");

    Log.Info($"BT Frequency Optimization:\n" +
             $"  Every Frame: {monitor1.AverageTickTime}ms\n" +
             $"  10Hz: {monitor2.AverageTickTime}ms\n" +
             $"  Improvement: {improvement}%");
}
```

---

## 2. 内存泄漏测试

### 测试用例 4b.3: Timer生命周期内存测试

**测试目的**: 验证Timer创建和销毁无内存泄漏

**前置条件**:
- 多次创建和销毁Unit

**测试步骤**:
1. 记录初始内存
2. 创建1000个Unit并销毁
3. 强制GC
4. 检查内存增长

**预期结果**:
- 内存增长 < 10MB

**测试代码**:
```csharp
[Test]
public async ETTask Test_TimerMemoryLeak()
{
    // Arrange
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    long initialMemory = GC.GetTotalMemory(true);

    // Act - 创建和销毁1000个Unit
    for (int i = 0; i < 1000; i++)
    {
        Unit unit = UnitFactory.Create(scene, i + 1, UnitType.Player);
        MoveComponent moveComp = unit.AddComponent<MoveComponent>();

        // 启动摇杆移动（创建Timer）
        moveComp.StartJoystickMove();

        await TimerComponent.Instance.WaitAsync(10);

        // 停止移动（销毁Timer）
        moveComp.StopJoystickMove();

        // 销毁Unit
        unit.Dispose();

        if (i % 100 == 0)
        {
            GC.Collect();
        }
    }

    // 强制GC
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    long finalMemory = GC.GetTotalMemory(true);
    long memoryGrowth = (finalMemory - initialMemory) / 1024 / 1024; // MB

    // Assert
    Assert.IsTrue(memoryGrowth < 10,
        $"Memory growth: {memoryGrowth}MB (should < 10MB)");

    Log.Info($"Memory Leak Test:\n" +
             $"  Initial: {initialMemory / 1024 / 1024}MB\n" +
             $"  Final: {finalMemory / 1024 / 1024}MB\n" +
             $"  Growth: {memoryGrowth}MB");
}
```

---

### 测试用例 4b.4: 长时间运行稳定性测试

**测试目的**: 验证系统长时间运行无内存泄漏和性能衰减

**前置条件**:
- 12人战斗场景

**测试步骤**:
1. 运行10分钟
2. 每分钟记录性能和内存
3. 检查趋势

**预期结果**:
- 性能无衰减
- 内存增长线性且缓慢

**测试代码**:
```csharp
[Test]
public async ETTask Test_LongRunStability()
{
    // Arrange
    List<Unit> units = CreateCombatScene(12);

    List<PerformanceSnapshot> snapshots = new List<PerformanceSnapshot>();

    // Act - 运行10分钟
    int totalMinutes = 10;
    for (int minute = 0; minute < totalMinutes; minute++)
    {
        PerformanceMonitor monitor = new PerformanceMonitor();
        monitor.Start();

        long memoryStart = GC.GetTotalMemory(false);

        // 运行1分钟
        for (int tick = 0; tick < 30 * 60; tick++) // 30Hz * 60秒
        {
            long tickStart = TimeInfo.Instance.ServerNow();

            // 移动
            foreach (Unit unit in units)
            {
                unit.GetComponent<MoveComponent>()?.TickJoystickMove();
            }

            // BT
            if (tick % 3 == 0)
            {
                foreach (Unit unit in units)
                {
                    unit.GetChild<BTComponent>()?.Update();
                }
            }

            long tickEnd = TimeInfo.Instance.ServerNow();
            monitor.RecordTick(tickEnd - tickStart);

            await TimerComponent.Instance.WaitAsync(33);
        }

        monitor.Stop();
        long memoryEnd = GC.GetTotalMemory(false);

        snapshots.Add(new PerformanceSnapshot
        {
            Minute = minute + 1,
            AverageTickTime = monitor.AverageTickTime,
            MaxTickTime = monitor.MaxTickTime,
            MemoryMB = memoryEnd / 1024 / 1024
        });

        Log.Info($"Minute {minute + 1}: " +
                 $"AvgTick={monitor.AverageTickTime}ms, " +
                 $"MaxTick={monitor.MaxTickTime}ms, " +
                 $"Memory={memoryEnd / 1024 / 1024}MB");
    }

    // Assert - 检查性能衰减
    float firstMinuteAvg = snapshots[0].AverageTickTime;
    float lastMinuteAvg = snapshots[totalMinutes - 1].AverageTickTime;
    float performanceDegradation = (lastMinuteAvg - firstMinuteAvg) / firstMinuteAvg * 100;

    Assert.IsTrue(performanceDegradation < 10,
        $"Performance degradation: {performanceDegradation}% (should < 10%)");

    // Assert - 检查内存增长
    long memoryGrowth = snapshots[totalMinutes - 1].MemoryMB - snapshots[0].MemoryMB;
    Assert.IsTrue(memoryGrowth < 100,
        $"Memory growth: {memoryGrowth}MB (should < 100MB in 10 minutes)");
}

private List<Unit> CreateCombatScene(int playerCount)
{
    List<Unit> units = new List<Unit>();

    for (int i = 0; i < playerCount; i++)
    {
        Unit unit = UnitFactory.Create(scene, i + 1, UnitType.Player);
        unit.Position = new float3(i * 2, 0, (i % 2) * 20);
        unit.AddComponent<CampComponent, int>(i % 2 + 1);
        SetupCombatUnit(unit, 1001, 1002);

        MoveComponent moveComp = unit.AddComponent<MoveComponent>();
        moveComp.StartJoystickMove();
        moveComp.JoystickDir = i % 2 == 0 ? new float3(0, 0, 1) : new float3(0, 0, -1);

        units.Add(unit);
    }

    foreach (Unit unit in units)
    {
        MockAOI(unit, units.Where(u => u.Id != unit.Id).ToArray());
    }

    return units;
}
```

---

## 3. 网络带宽测试

### 测试用例 4b.5: 广播带宽优化验证

**测试目的**: 验证状态广播优化能有效降低带宽

**前置条件**:
- 12人场景
- 广播频率10Hz

**测试步骤**:
1. 测试无优化的带宽消耗
2. 测试有优化的带宽消耗
3. 对比差异

**预期结果**:
- 优化后带宽降低50%以上

**测试代码**:
```csharp
[Test]
public async ETTask Test_BroadcastBandwidthOptimization()
{
    // Arrange
    List<Unit> units = CreateCombatScene(12);

    // Test 1: 无优化（每次都广播）
    NetworkMonitor monitor1 = new NetworkMonitor();

    for (int i = 0; i < 300; i++) // 30秒
    {
        foreach (Unit unit in units)
        {
            MoveComponent moveComp = unit.GetComponent<MoveComponent>();

            // 强制广播（无优化）
            M2C_JoystickState msg = M2C_JoystickState.Create();
            msg.UnitId = unit.Id;
            msg.Position = unit.Position;
            msg.MoveDir = moveComp.JoystickDir;

            int msgSize = ProtobufHelper.ToBytes(msg).Length;
            monitor1.RecordBroadcast(msgSize * 11); // 广播给其他11人
        }

        await TimerComponent.Instance.WaitAsync(100);
    }

    // Test 2: 有优化（状态变化才广播）
    NetworkMonitor monitor2 = new NetworkMonitor();

    for (int i = 0; i < 300; i++)
    {
        foreach (Unit unit in units)
        {
            MoveComponent moveComp = unit.GetComponent<MoveComponent>();

            // 检查是否需要广播
            if (math.distance(moveComp.LastSyncPos, unit.Position) >= 0.1f ||
                math.distance(moveComp.LastSyncDir, moveComp.JoystickDir) >= 0.01f)
            {
                M2C_JoystickState msg = M2C_JoystickState.Create();
                msg.UnitId = unit.Id;
                msg.Position = unit.Position;
                msg.MoveDir = moveComp.JoystickDir;

                int msgSize = ProtobufHelper.ToBytes(msg).Length;
                monitor2.RecordBroadcast(msgSize * 11);

                moveComp.LastSyncPos = unit.Position;
                moveComp.LastSyncDir = moveComp.JoystickDir;
            }
        }

        await TimerComponent.Instance.WaitAsync(100);
    }

    // Assert
    float reduction = (monitor1.TotalBytes - monitor2.TotalBytes) /
                      (float)monitor1.TotalBytes * 100;

    Assert.IsTrue(reduction > 50,
        $"Bandwidth reduction: {reduction}% (should > 50%)");

    Log.Info($"Broadcast Bandwidth Optimization:\n" +
             $"  No Optimization: {monitor1.TotalBytes / 1024}KB\n" +
             $"  With Optimization: {monitor2.TotalBytes / 1024}KB\n" +
             $"  Reduction: {reduction}%");
}
```

---

## 4. 测试辅助类

### 4.1 PerformanceMonitor

```csharp
public class PerformanceMonitor
{
    private List<long> tickTimes = new List<long>();
    private long startTime;

    public void Start()
    {
        startTime = TimeInfo.Instance.ServerNow();
        tickTimes.Clear();
    }

    public void RecordTick(long tickTime)
    {
        tickTimes.Add(tickTime);
    }

    public void Stop()
    {
        // 计算统计数据
    }

    public float AverageTickTime => tickTimes.Count > 0 ? tickTimes.Average() : 0;
    public long MaxTickTime => tickTimes.Count > 0 ? tickTimes.Max() : 0;
    public long MinTickTime => tickTimes.Count > 0 ? tickTimes.Min() : 0;

    public float TickTimeStdDev
    {
        get
        {
            if (tickTimes.Count == 0) return 0;
            float avg = AverageTickTime;
            float sumSquares = tickTimes.Sum(t => (t - avg) * (t - avg));
            return (float)Math.Sqrt(sumSquares / tickTimes.Count);
        }
    }
}
```

### 4.2 PerformanceSnapshot

```csharp
public class PerformanceSnapshot
{
    public int Minute;
    public float AverageTickTime;
    public long MaxTickTime;
    public long MemoryMB;
}
```

### 4.3 NetworkMonitor

```csharp
public class NetworkMonitor
{
    public long TotalBytes = 0;
    public int MessageCount = 0;

    public void RecordBroadcast(int bytes)
    {
        TotalBytes += bytes;
        MessageCount++;
    }
}
```

---

## 5. 性能基准

### 5.1 目标性能指标

| 指标 | 目标值 | 说明 |
|------|--------|------|
| 服务端帧率 | 30Hz | 稳定30帧 |
| 平均Tick时间 | < 30ms | 留有余量 |
| 最大Tick时间 | < 50ms | 偶尔峰值 |
| Tick时间标准差 | < 10ms | 稳定性 |
| 12人场景CPU | < 50% | 单核 |
| 内存增长 | < 10MB/10min | 长期稳定 |
| 广播带宽 | < 100KB/s | 12人场景 |

---

## 6. TDD实施步骤

### 第三周第一天：性能测试

**Step 1: 建立性能基准**
```bash
dotnet test --filter Test_12PlayersCombatPerformance
# 记录基准数据
```

**Step 2: 识别性能瓶颈**
- 使用Profiler分析
- 找出热点代码

**Step 3: 优化并验证**
- 实施优化
- 重新运行性能测试
- 对比改进

**Step 4: 长期稳定性验证**
```bash
dotnet test --filter Test_LongRunStability
```

---

**文档版本**: v1.0
**测试用例数**: 4
**预计时间**: 4小时
**状态**: 待实施
