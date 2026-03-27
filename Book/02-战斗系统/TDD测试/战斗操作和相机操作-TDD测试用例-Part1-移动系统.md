# TDD测试用例 - Part 1: 移动系统

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 移动系统
- **优先级**: P0
- **测试用例数**: 12

---

## 1. 移动模式切换测试

### 测试用例 1.1: 从路径移动切换到摇杆移动

**测试目的**: 验证收到摇杆输入时能正确从路径移动切换到摇杆移动

**前置条件**:
- Unit正在进行路径移动
- MoveComponent.Mode = MoveMode.Path

**测试步骤**:
1. 创建Unit并启动路径移动
2. 发送摇杆输入消息
3. 检查移动模式是否切换

**预期结果**:
- MoveComponent.Mode 变为 MoveMode.Joystick
- 路径移动Timer被停止
- 发布MoveStop事件

**测试代码**:
```csharp
[Test]
public async ETTask Test_SwitchFromPathToJoystick()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    List<float3> path = new List<float3>
    {
        new float3(0, 0, 0),
        new float3(10, 0, 0)
    };

    // 启动路径移动
    var moveTask = moveComp.MoveToAsync(path, 5f);
    await TimerComponent.Instance.WaitAsync(100);

    Assert.AreEqual(MoveMode.Path, moveComp.Mode);

    // Act - 发送摇杆输入
    C2M_JoystickInput input = C2M_JoystickInput.Create();
    input.MoveDir = new float3(1, 0, 0);
    input.Seq = 1;

    await MessageHelper.CallActor(unit, input);

    // Assert
    Assert.AreEqual(MoveMode.Joystick, moveComp.Mode);
    Assert.IsTrue(moveTask.IsCompleted); // 路径移动被中断
}
```

---

### 测试用例 1.2: 从摇杆移动切换到路径移动

**测试目的**: 验证调用MoveToAsync时能正确从摇杆移动切换到路径移动

**前置条件**:
- Unit正在进行摇杆移动
- MoveComponent.Mode = MoveMode.Joystick

**测试步骤**:
1. 创建Unit并启动摇杆移动
2. 调用MoveToAsync
3. 检查移动模式是否切换

**预期结果**:
- MoveComponent.Mode 变为 MoveMode.Path
- 摇杆移动Timer被停止
- JoystickDir被清零

**测试代码**:
```csharp
[Test]
public async ETTask Test_SwitchFromJoystickToPath()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    // 启动摇杆移动
    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0);

    await TimerComponent.Instance.WaitAsync(100);
    Assert.AreEqual(MoveMode.Joystick, moveComp.Mode);

    // Act - 启动路径移动
    List<float3> path = new List<float3>
    {
        unit.Position,
        unit.Position + new float3(10, 0, 0)
    };

    var moveTask = moveComp.MoveToAsync(path, 5f);

    // Assert
    Assert.AreEqual(MoveMode.Path, moveComp.Mode);
    Assert.AreEqual(float3.zero, moveComp.JoystickDir);
    Assert.AreEqual(0, moveComp.MoveTimer); // Timer已停止
}
```

---

## 2. 摇杆输入处理测试

### 测试用例 2.1: 服务端接收并处理摇杆输入

**测试目的**: 验证服务端能正确接收和处理客户端摇杆输入

**前置条件**:
- Unit已创建
- 输入方向向量合法（长度<=1）

**测试步骤**:
1. 发送摇杆输入消息
2. 等待服务端处理
3. 检查MoveComponent状态

**预期结果**:
- JoystickDir被更新
- LastSeq被更新
- Mode切换为Joystick

**测试代码**:
```csharp
[Test]
public async ETTask Test_ServerReceiveJoystickInput()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    float3 inputDir = math.normalize(new float3(1, 0, 1));

    // Act
    C2M_JoystickInput input = C2M_JoystickInput.Create();
    input.MoveDir = inputDir;
    input.Seq = 1;
    input.ClientTime = TimeInfo.Instance.ServerNow();

    await MessageHelper.CallActor(unit, input);

    // Assert
    Assert.AreEqual(MoveMode.Joystick, moveComp.Mode);
    Assert.AreEqual(inputDir, moveComp.JoystickDir);
    Assert.AreEqual(1, moveComp.LastSeq);
}
```

---

### 测试用例 2.2: 拒绝非法输入（方向向量过长）

**测试目的**: 验证服务端能拒绝长度超过1.1的方向向量

**前置条件**:
- Unit已创建

**测试步骤**:
1. 发送长度>1.1的方向向量
2. 检查是否被拒绝

**预期结果**:
- 输入被忽略
- JoystickDir不变
- LastSeq不变

**测试代码**:
```csharp
[Test]
public async ETTask Test_RejectInvalidDirection()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    float3 oldDir = moveComp.JoystickDir;
    int oldSeq = moveComp.LastSeq;

    // Act - 发送非法输入
    C2M_JoystickInput input = C2M_JoystickInput.Create();
    input.MoveDir = new float3(2, 0, 2); // 长度 > 1.1
    input.Seq = 1;

    await MessageHelper.CallActor(unit, input);

    // Assert
    Assert.AreEqual(oldDir, moveComp.JoystickDir);
    Assert.AreEqual(oldSeq, moveComp.LastSeq);
}
```

---

### 测试用例 2.3: 输入频率限制

**测试目的**: 验证服务端能限制输入频率（最高25Hz）

**前置条件**:
- Unit已创建

**测试步骤**:
1. 快速发送多个输入（间隔<40ms）
2. 检查是否被限流

**预期结果**:
- 间隔<40ms的输入被忽略
- 只有第一个输入生效

**测试代码**:
```csharp
[Test]
public async ETTask Test_InputRateLimit()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    // Act - 发送第一个输入
    C2M_JoystickInput input1 = C2M_JoystickInput.Create();
    input1.MoveDir = new float3(1, 0, 0);
    input1.Seq = 1;
    await MessageHelper.CallActor(unit, input1);

    Assert.AreEqual(1, moveComp.LastSeq);

    // 立即发送第二个输入（间隔<40ms）
    C2M_JoystickInput input2 = C2M_JoystickInput.Create();
    input2.MoveDir = new float3(0, 0, 1);
    input2.Seq = 2;
    await MessageHelper.CallActor(unit, input2);

    // Assert - 第二个输入被忽略
    Assert.AreEqual(1, moveComp.LastSeq);
    Assert.AreEqual(new float3(1, 0, 0), moveComp.JoystickDir);
}
```

---

### 测试用例 2.4: Seq防重放攻击

**测试目的**: 验证服务端能拒绝旧的或重复的Seq

**前置条件**:
- Unit已创建
- 已接收Seq=5的输入

**测试步骤**:
1. 发送Seq=5的输入
2. 发送Seq=3的输入（旧）
3. 发送Seq=5的输入（重复）
4. 检查是否被拒绝

**预期结果**:
- 旧Seq被拒绝
- 重复Seq被拒绝
- LastSeq保持为5

**测试代码**:
```csharp
[Test]
public async ETTask Test_SeqReplayProtection()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    // 发送Seq=5
    C2M_JoystickInput input1 = C2M_JoystickInput.Create();
    input1.MoveDir = new float3(1, 0, 0);
    input1.Seq = 5;
    await MessageHelper.CallActor(unit, input1);
    await TimerComponent.Instance.WaitAsync(50);

    Assert.AreEqual(5, moveComp.LastSeq);

    // Act - 发送旧Seq
    C2M_JoystickInput input2 = C2M_JoystickInput.Create();
    input2.MoveDir = new float3(0, 0, 1);
    input2.Seq = 3;
    await MessageHelper.CallActor(unit, input2);
    await TimerComponent.Instance.WaitAsync(50);

    // Assert
    Assert.AreEqual(5, moveComp.LastSeq);
    Assert.AreEqual(new float3(1, 0, 0), moveComp.JoystickDir);
}
```

---

## 3. 客户端预测测试

### 测试用例 3.1: 客户端每帧预测移动

**测试目的**: 验证客户端能每帧根据摇杆方向预测位置

**前置条件**:
- 客户端场景已创建
- InputSystemComponent.IsMoving = true

**测试步骤**:
1. 设置摇杆方向
2. 模拟多帧Update
3. 检查位置是否更新

**预期结果**:
- 每帧位置都更新
- 移动方向正确
- 移动速度正确

**测试代码**:
```csharp
[Test]
public void Test_ClientPrediction()
{
    // Arrange
    Scene clientScene = TestHelper.CreateClientScene();
    Unit myUnit = UnitFactory.Create(clientScene, 1, UnitType.Player);

    MoveComponent moveComp = myUnit.AddComponent<MoveComponent>();
    moveComp.JoystickDir = new float3(1, 0, 0);

    NumericComponent numericComp = myUnit.AddComponent<NumericComponent>();
    numericComp.Set(NumericType.Speed, 5f);

    InputSystemComponent inputComp = clientScene.AddComponent<InputSystemComponent>();
    inputComp.IsMoving = true;

    float3 startPos = myUnit.Position;

    // Act - 模拟3帧（假设每帧0.033秒）
    for (int i = 0; i < 3; i++)
    {
        inputComp.Update();
        TimerComponent.Instance.WaitFrameAsync().Coroutine();
    }

    // Assert
    float expectedDistance = 5f * 0.033f * 3; // speed * dt * frames
    float actualDistance = math.distance(startPos, myUnit.Position);

    Assert.AreEqual(expectedDistance, actualDistance, 0.01f);
    Assert.AreEqual(new float3(1, 0, 0), math.normalize(myUnit.Position - startPos));
}
```

---

### 测试用例 3.2: 服务端回正（小误差平滑）

**测试目的**: 验证客户端能平滑回正小误差（0.1-0.5米）

**前置条件**:
- 客户端预测位置与服务端位置有0.3米误差

**测试步骤**:
1. 设置客户端预测位置
2. 接收服务端位置
3. 检查是否平滑插值

**预期结果**:
- 使用lerp平滑回正
- 插值系数为0.3

**测试代码**:
```csharp
[Test]
public void Test_ServerReconciliation_SmallError()
{
    // Arrange
    Scene clientScene = TestHelper.CreateClientScene();
    Unit myUnit = UnitFactory.Create(clientScene, 1, UnitType.Player);

    InputSystemComponent inputComp = clientScene.AddComponent<InputSystemComponent>();

    float3 clientPos = new float3(5, 0, 0);
    float3 serverPos = new float3(5.3f, 0, 0); // 0.3米误差

    myUnit.Position = clientPos;

    // 记录预测
    inputComp.PredictionHistory.Add(new PredictionRecord
    {
        Seq = 1,
        Time = TimeInfo.Instance.ClientNow(),
        Position = clientPos,
        MoveDir = new float3(1, 0, 0)
    });

    // Act - 接收服务端状态
    M2C_JoystickState msg = M2C_JoystickState.Create();
    msg.UnitId = myUnit.Id;
    msg.Position = serverPos;
    msg.Seq = 1;

    myUnit.OnJoystickState(msg);

    // Assert - 平滑插值
    float3 expectedPos = math.lerp(clientPos, serverPos, 0.3f);
    Assert.AreEqual(expectedPos, myUnit.Position);
}
```

---

### 测试用例 3.3: 服务端回正（大误差硬对齐）

**测试目的**: 验证客户端能硬对齐大误差（>0.5米）

**前置条件**:
- 客户端预测位置与服务端位置有1米误差

**测试步骤**:
1. 设置客户端预测位置
2. 接收服务端位置
3. 检查是否硬对齐

**预期结果**:
- 直接设置为服务端位置
- 不使用插值

**测试代码**:
```csharp
[Test]
public void Test_ServerReconciliation_LargeError()
{
    // Arrange
    Scene clientScene = TestHelper.CreateClientScene();
    Unit myUnit = UnitFactory.Create(clientScene, 1, UnitType.Player);

    InputSystemComponent inputComp = clientScene.AddComponent<InputSystemComponent>();

    float3 clientPos = new float3(5, 0, 0);
    float3 serverPos = new float3(6.5f, 0, 0); // 1.5米误差

    myUnit.Position = clientPos;

    inputComp.PredictionHistory.Add(new PredictionRecord
    {
        Seq = 1,
        Time = TimeInfo.Instance.ClientNow(),
        Position = clientPos,
        MoveDir = new float3(1, 0, 0)
    });

    // Act
    M2C_JoystickState msg = M2C_JoystickState.Create();
    msg.UnitId = myUnit.Id;
    msg.Position = serverPos;
    msg.Seq = 1;

    myUnit.OnJoystickState(msg);

    // Assert - 硬对齐
    Assert.AreEqual(serverPos, myUnit.Position);
}
```

---

## 4. 服务端Tick测试

### 测试用例 4.1: 服务端30Hz Tick移动

**测试目的**: 验证服务端能以30Hz频率更新位置

**前置条件**:
- Unit正在摇杆移动
- JoystickDir已设置

**测试步骤**:
1. 启动摇杆移动
2. 等待100ms（3次Tick）
3. 检查位置是否更新

**预期结果**:
- 位置按速度和时间正确更新
- 朝向与移动方向一致

**测试代码**:
```csharp
[Test]
public async ETTask Test_ServerTickMove()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    NumericComponent numericComp = unit.AddComponent<NumericComponent>();
    numericComp.Set(NumericType.Speed, 5f);

    float3 startPos = unit.Position;

    // Act
    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0);

    await TimerComponent.Instance.WaitAsync(100); // 等待3次Tick

    // Assert
    float expectedDistance = 5f * 0.033f * 3; // speed * dt * ticks
    float actualDistance = math.distance(startPos, unit.Position);

    Assert.AreEqual(expectedDistance, actualDistance, 0.1f);

    // 检查朝向
    float3 forward = math.mul(unit.Rotation, math.forward());
    Assert.AreEqual(new float3(1, 0, 0), forward);
}
```

---

### 测试用例 4.2: 输入超时停止移动

**测试目的**: 验证1秒无输入后自动停止移动

**前置条件**:
- Unit正在摇杆移动
- 最后输入时间为1秒前

**测试步骤**:
1. 启动摇杆移动
2. 等待1秒不发送输入
3. 检查是否停止

**预期结果**:
- JoystickDir被清零
- SpeedScale为0

**测试代码**:
```csharp
[Test]
public async ETTask Test_InputTimeout()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0);
    moveComp.LastInputTime = TimeInfo.Instance.ServerNow();

    // Act - 等待1秒
    await TimerComponent.Instance.WaitAsync(1000);

    // 触发一次Tick
    moveComp.TickJoystickMove();

    // Assert
    Assert.AreEqual(float3.zero, moveComp.JoystickDir);
    Assert.AreEqual(0f, moveComp.SpeedScale);
}
```

---

## 5. NavMesh测试

### 测试用例 5.1: 条件NavMesh回贴

**测试目的**: 验证只在移动超过1米时才进行NavMesh查询

**前置条件**:
- 场景有NavMesh
- Unit正在移动

**测试步骤**:
1. 移动0.5米
2. 检查是否查询NavMesh
3. 移动1.5米
4. 检查是否查询NavMesh

**预期结果**:
- 移动<1米不查询
- 移动>1米才查询

**测试代码**:
```csharp
[Test]
public async ETTask Test_ConditionalNavMeshCheck()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    NumericComponent numericComp = unit.AddComponent<NumericComponent>();
    numericComp.Set(NumericType.Speed, 5f);

    MockRecastPathComponent mockNavMesh = scene.AddComponent<MockRecastPathComponent>();

    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0);
    moveComp.LastNavMeshPos = unit.Position;

    // Act 1 - 移动0.5米
    await TimerComponent.Instance.WaitAsync(100); // 移动约0.5米

    // Assert 1 - 未查询NavMesh
    Assert.AreEqual(0, mockNavMesh.QueryCount);

    // Act 2 - 继续移动到1.5米
    await TimerComponent.Instance.WaitAsync(200);

    // Assert 2 - 查询了NavMesh
    Assert.IsTrue(mockNavMesh.QueryCount > 0);
}
```

---

## 6. Timer生命周期测试

### 测试用例 6.1: Timer正确创建和销毁

**测试目的**: 验证摇杆移动Timer能正确创建和销毁，无内存泄漏

**前置条件**:
- Unit已创建

**测试步骤**:
1. 启动摇杆移动
2. 检查Timer是否创建
3. 停止摇杆移动
4. 检查Timer是否销毁

**预期结果**:
- MoveTimer和SyncTimer都被创建
- 停止时都被销毁
- 无内存泄漏

**测试代码**:
```csharp
[Test]
public async ETTask Test_TimerLifecycle()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    TimerComponent timerComp = scene.Root().GetComponent<TimerComponent>();
    int initialTimerCount = timerComp.GetTimerCount();

    // Act 1 - 启动
    moveComp.StartJoystickMove();

    // Assert 1 - Timer已创建
    Assert.AreNotEqual(0, moveComp.MoveTimer);
    Assert.AreNotEqual(0, moveComp.SyncTimer);
    Assert.AreEqual(initialTimerCount + 2, timerComp.GetTimerCount());

    // Act 2 - 停止
    moveComp.StopJoystickMove();

    // Assert 2 - Timer已销毁
    Assert.AreEqual(0, moveComp.MoveTimer);
    Assert.AreEqual(0, moveComp.SyncTimer);
    Assert.AreEqual(initialTimerCount, timerComp.GetTimerCount());
}
```

---

## 7. 测试辅助类

### 7.1 MockRecastPathComponent

```csharp
public class MockRecastPathComponent : Entity
{
    public int QueryCount = 0;

    public float3 RecastFindNearestPoint(float3 pos)
    {
        QueryCount++;
        return pos; // 简单返回原位置
    }
}
```

### 7.2 TestHelper

```csharp
public static class TestHelper
{
    public static Scene CreateTestScene()
    {
        Scene scene = EntitySceneFactory.CreateScene(1, SceneType.Map, "Test");
        scene.AddComponent<TimerComponent>();
        scene.AddComponent<UnitComponent>();
        return scene;
    }

    public static Scene CreateClientScene()
    {
        Scene scene = EntitySceneFactory.CreateScene(1, SceneType.Client, "TestClient");
        scene.AddComponent<TimerComponent>();
        scene.AddComponent<UnitComponent>();
        scene.AddComponent<PlayerComponent>();
        return scene;
    }
}
```

---

**文档版本**: v1.0
**测试用例数**: 12
**状态**: 待实施
