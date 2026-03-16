# TDD测试用例 - Part 3: 行为树系统

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 行为树系统
- **优先级**: P1
- **测试用例数**: 10

---

## 1. BT节点基础测试

### 测试用例 3.1: Sequence节点顺序执行

**测试目的**: 验证Sequence节点按顺序执行子节点

**前置条件**:
- 创建Sequence节点和多个子节点

**测试步骤**:
1. 创建Sequence节点
2. 添加3个子节点（都返回Success）
3. 执行Tick

**预期结果**:
- 按顺序执行所有子节点
- 所有成功则返回Success

**测试代码**:
```csharp
[Test]
public void Test_SequenceNode()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    BTContext context = new BTContext(unit);

    BTSequence sequence = new BTSequence();

    MockActionNode action1 = new MockActionNode("Action1", BTStatus.Success);
    MockActionNode action2 = new MockActionNode("Action2", BTStatus.Success);
    MockActionNode action3 = new MockActionNode("Action3", BTStatus.Success);

    sequence.AddChild(action1);
    sequence.AddChild(action2);
    sequence.AddChild(action3);

    // Act
    BTStatus result = sequence.Tick(context);

    // Assert
    Assert.AreEqual(BTStatus.Success, result);
    Assert.IsTrue(action1.Executed);
    Assert.IsTrue(action2.Executed);
    Assert.IsTrue(action3.Executed);
}
```

---

### 测试用例 3.2: Sequence节点遇到失败停止

**测试目的**: 验证Sequence节点遇到失败立即停止

**前置条件**:
- Sequence有多个子节点
- 第二个节点返回Failure

**测试步骤**:
1. 创建Sequence节点
2. 第二个子节点返回Failure
3. 执行Tick

**预期结果**:
- 第一个节点执行
- 第二个节点执行并返回Failure
- 第三个节点不执行
- Sequence返回Failure

**测试代码**:
```csharp
[Test]
public void Test_SequenceFailure()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    BTContext context = new BTContext(unit);

    BTSequence sequence = new BTSequence();

    MockActionNode action1 = new MockActionNode("Action1", BTStatus.Success);
    MockActionNode action2 = new MockActionNode("Action2", BTStatus.Failure);
    MockActionNode action3 = new MockActionNode("Action3", BTStatus.Success);

    sequence.AddChild(action1);
    sequence.AddChild(action2);
    sequence.AddChild(action3);

    // Act
    BTStatus result = sequence.Tick(context);

    // Assert
    Assert.AreEqual(BTStatus.Failure, result);
    Assert.IsTrue(action1.Executed);
    Assert.IsTrue(action2.Executed);
    Assert.IsFalse(action3.Executed); // 第三个不执行
}
```

---

### 测试用例 3.3: Selector节点选择执行

**测试目的**: 验证Selector节点找到第一个成功的子节点

**前置条件**:
- Selector有多个子节点

**测试步骤**:
1. 创建Selector节点
2. 第一个失败，第二个成功
3. 执行Tick

**预期结果**:
- 第一个节点执行并失败
- 第二个节点执行并成功
- 第三个节点不执行
- Selector返回Success

**测试代码**:
```csharp
[Test]
public void Test_SelectorNode()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    BTContext context = new BTContext(unit);

    BTSelector selector = new BTSelector();

    MockActionNode action1 = new MockActionNode("Action1", BTStatus.Failure);
    MockActionNode action2 = new MockActionNode("Action2", BTStatus.Success);
    MockActionNode action3 = new MockActionNode("Action3", BTStatus.Success);

    selector.AddChild(action1);
    selector.AddChild(action2);
    selector.AddChild(action3);

    // Act
    BTStatus result = selector.Tick(context);

    // Assert
    Assert.AreEqual(BTStatus.Success, result);
    Assert.IsTrue(action1.Executed);
    Assert.IsTrue(action2.Executed);
    Assert.IsFalse(action3.Executed); // 找到成功的就停止
}
```

---

### 测试用例 3.4: Parallel节点并行执行

**测试目的**: 验证Parallel节点并行执行所有子节点

**前置条件**:
- Parallel有多个子节点

**测试步骤**:
1. 创建Parallel节点
2. 添加多个子节点
3. 执行Tick

**预期结果**:
- 所有子节点都执行
- 所有成功则返回Success
- 任一失败则返回Failure

**测试代码**:
```csharp
[Test]
public void Test_ParallelNode()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    BTContext context = new BTContext(unit);

    BTParallel parallel = new BTParallel();

    MockActionNode action1 = new MockActionNode("Action1", BTStatus.Success);
    MockActionNode action2 = new MockActionNode("Action2", BTStatus.Success);

    parallel.AddChild(action1);
    parallel.AddChild(action2);

    // Act
    BTStatus result = parallel.Tick(context);

    // Assert
    Assert.AreEqual(BTStatus.Success, result);
    Assert.IsTrue(action1.Executed);
    Assert.IsTrue(action2.Executed); // 都执行了
}
```

---

## 2. BT条件节点测试

### 测试用例 3.5: CheckHasTarget条件

**测试目的**: 验证CheckHasTarget能正确检查是否有目标

**前置条件**:
- Unit有TargetSelectorComponent

**测试步骤**:
1. 创建Unit和目标
2. 执行CheckHasTarget
3. 检查返回值和Context

**预期结果**:
- 有目标返回Success
- 目标设置到Context
- 无目标返回Failure

**测试代码**:
```csharp
[Test]
public void Test_CheckHasTarget()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    unit.AddComponent<CampComponent, int>(1);

    Unit target = UnitFactory.Create(scene, 2, UnitType.Player);
    target.Position = new float3(5, 0, 0);
    target.AddComponent<CampComponent, int>(2);
    target.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    TargetSelectorComponent selector = unit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    MockAOI(unit, new[] { target });

    BTContext context = new BTContext(unit);
    CheckHasTarget checkNode = new CheckHasTarget();

    // Act
    BTStatus result = checkNode.Tick(context);

    // Assert
    Assert.AreEqual(BTStatus.Success, result);
    Assert.AreEqual(target.Id, context.GetTarget().Id);
}
```

---

### 测试用例 3.6: CheckInRange条件

**测试目的**: 验证CheckInRange能正确检查目标是否在范围内

**前置条件**:
- Context中有目标

**测试步骤**:
1. 设置目标位置
2. 设置检查范围
3. 执行CheckInRange

**预期结果**:
- 在范围内返回Success
- 超出范围返回Failure

**测试代码**:
```csharp
[Test]
public void Test_CheckInRange()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    unit.Position = new float3(0, 0, 0);

    Unit target = UnitFactory.Create(scene, 2, UnitType.Player);
    target.Position = new float3(8, 0, 0); // 距离8米

    BTContext context = new BTContext(unit);
    context.SetTarget(target);

    CheckInRange checkNode = new CheckInRange();
    checkNode.Range = 10f;

    // Act & Assert - 在范围内
    Assert.AreEqual(BTStatus.Success, checkNode.Tick(context));

    // Act & Assert - 超出范围
    checkNode.Range = 5f;
    Assert.AreEqual(BTStatus.Failure, checkNode.Tick(context));
}
```

---

### 测试用例 3.7: CheckNotMoving条件

**测试目的**: 验证CheckNotMoving能正确检查是否静止

**前置条件**:
- Unit有MoveComponent

**测试步骤**:
1. 设置摇杆方向为零
2. 执行CheckNotMoving
3. 设置摇杆方向非零
4. 再次执行

**预期结果**:
- 静止时返回Success
- 移动时返回Failure

**测试代码**:
```csharp
[Test]
public void Test_CheckNotMoving()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    MoveComponent moveComp = unit.AddComponent<MoveComponent>();

    BTContext context = new BTContext(unit);
    CheckNotMoving checkNode = new CheckNotMoving();

    // Act & Assert - 静止
    moveComp.Mode = MoveMode.Joystick;
    moveComp.JoystickDir = float3.zero;
    Assert.AreEqual(BTStatus.Success, checkNode.Tick(context));

    // Act & Assert - 移动中
    moveComp.JoystickDir = new float3(1, 0, 0);
    Assert.AreEqual(BTStatus.Failure, checkNode.Tick(context));
}
```

---

## 3. 武器BT测试

### 测试用例 3.8: 步枪BT静止射击

**测试目的**: 验证步枪BT只在静止时射击

**前置条件**:
- Unit装备步枪
- 有目标在范围内

**测试步骤**:
1. Unit移动中
2. 执行步枪BT
3. Unit停止
4. 再次执行BT

**预期结果**:
- 移动中不射击
- 静止时射击

**测试代码**:
```csharp
[Test]
public void Test_RifleBT_RequireStatic()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    unit.Position = new float3(0, 0, 0);
    unit.AddComponent<CampComponent, int>(1);

    MoveComponent moveComp = unit.AddComponent<MoveComponent>();
    moveComp.Mode = MoveMode.Joystick;
    moveComp.JoystickDir = new float3(1, 0, 0); // 移动中

    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);
    weaponComp.CurrentWeapon = WeaponType.Rifle;

    Unit target = UnitFactory.Create(scene, 2, UnitType.Player);
    target.Position = new float3(5, 0, 0);
    target.AddComponent<CampComponent, int>(2);
    target.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    TargetSelectorComponent selector = unit.AddComponent<TargetSelectorComponent>();
    MockAOI(unit, new[] { target });

    MockWeaponConfig(1001, new WeaponConfig
    {
        Id = 1001,
        Type = WeaponType.Rifle,
        AttackRange = 10f,
        AttackInterval = 0.5f,
        MagazineSize = 30
    });

    BTNode rifleBT = BTFactory.CreateRifleBT(WeaponConfigCategory.Instance.Get(1001));
    BTContext context = new BTContext(unit);

    // Act 1 - 移动中
    BTStatus result1 = rifleBT.Tick(context);

    // Assert 1 - 失败（因为在移动）
    Assert.AreEqual(BTStatus.Failure, result1);
    Assert.AreEqual(30, weaponComp.RifleAmmo); // 没有消耗弹药

    // Act 2 - 停止移动
    moveComp.JoystickDir = float3.zero;
    BTStatus result2 = rifleBT.Tick(context);

    // Assert 2 - 成功射击
    Assert.AreEqual(BTStatus.Success, result2);
    Assert.AreEqual(29, weaponComp.RifleAmmo); // 消耗了弹药
}
```

---

### 测试用例 3.9: 冲锋枪BT移动射击

**测试目的**: 验证冲锋枪BT可以边移动边射击

**前置条件**:
- Unit装备冲锋枪
- 有目标在范围内

**测试步骤**:
1. Unit移动中
2. 执行冲锋枪BT

**预期结果**:
- 移动中也能射击

**测试代码**:
```csharp
[Test]
public void Test_SMGBT_CanMoveAndFire()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    unit.Position = new float3(0, 0, 0);
    unit.AddComponent<CampComponent, int>(1);

    MoveComponent moveComp = unit.AddComponent<MoveComponent>();
    moveComp.Mode = MoveMode.Joystick;
    moveComp.JoystickDir = new float3(1, 0, 0); // 移动中

    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);
    weaponComp.CurrentWeapon = WeaponType.SMG;

    Unit target = UnitFactory.Create(scene, 2, UnitType.Player);
    target.Position = new float3(5, 0, 0);
    target.AddComponent<CampComponent, int>(2);
    target.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    TargetSelectorComponent selector = unit.AddComponent<TargetSelectorComponent>();
    MockAOI(unit, new[] { target });

    MockWeaponConfig(1002, new WeaponConfig
    {
        Id = 1002,
        Type = WeaponType.SMG,
        AttackRange = 8f,
        AttackInterval = 0.1f,
        MagazineSize = 40,
        CanMoveWhileFire = true
    });

    BTNode smgBT = BTFactory.CreateSMGBT(WeaponConfigCategory.Instance.Get(1002));
    BTContext context = new BTContext(unit);

    // Act - 移动中射击
    BTStatus result = smgBT.Tick(context);

    // Assert - 成功射击
    Assert.AreEqual(BTStatus.Success, result);
    Assert.AreEqual(39, weaponComp.SMGAmmo); // 消耗了弹药
}
```

---

### 测试用例 3.10: 换弹BT流程

**测试目的**: 验证换弹BT能正确执行换弹流程

**前置条件**:
- 弹药已耗尽

**测试步骤**:
1. 消耗所有弹药
2. 执行换弹BT
3. 模拟等待换弹时间

**预期结果**:
- 设置换弹状态
- 等待换弹时间
- 补充弹药
- 清除换弹状态

**测试代码**:
```csharp
[Test]
public async ETTask Test_ReloadBT()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    // 消耗所有弹药
    weaponComp.ConsumeAmmo(1001, 30);
    Assert.AreEqual(0, weaponComp.RifleAmmo);

    MockWeaponConfig(1001, new WeaponConfig
    {
        Id = 1001,
        ReloadTime = 2.0f,
        MagazineSize = 30
    });

    BTNode reloadBT = BTFactory.CreateReloadBT(WeaponConfigCategory.Instance.Get(1001));
    BTContext context = new BTContext(unit);
    context.SetInt("WeaponId", 1001);

    // Act - 开始换弹
    BTStatus result1 = reloadBT.Tick(context);

    // Assert - 换弹中
    Assert.AreEqual(BTStatus.Running, result1);
    Assert.IsTrue(weaponComp.IsReloading(1001));

    // Act - 等待换弹时间
    await TimerComponent.Instance.WaitAsync(2000);
    BTStatus result2 = reloadBT.Tick(context);

    // Assert - 换弹完成
    Assert.AreEqual(BTStatus.Success, result2);
    Assert.AreEqual(30, weaponComp.RifleAmmo); // 弹药已补充
    Assert.IsFalse(weaponComp.IsReloading(1001)); // 换弹状态已清除
}
```

---

## 4. BT执行频率测试

### 测试用例 3.11: BT限制执行频率

**测试目的**: 验证BT执行频率限制在10Hz

**前置条件**:
- BTComponent.BTTickInterval = 100ms

**测试步骤**:
1. 连续多次Update
2. 检查BT实际执行次数

**预期结果**:
- 100ms内只执行一次

**测试代码**:
```csharp
[Test]
public async ETTask Test_BTTickInterval()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    BTComponent btComp = unit.AddChild<BTComponent, int>(1001);

    MockBTNode mockNode = new MockBTNode();
    btComp.RootNode = mockNode;
    btComp.BTTickInterval = 100; // 100ms

    // Act - 连续Update 10次（每次间隔10ms）
    for (int i = 0; i < 10; i++)
    {
        btComp.Update();
        await TimerComponent.Instance.WaitAsync(10);
    }

    // Assert - 只执行了1次（100ms才执行一次）
    Assert.AreEqual(1, mockNode.TickCount);

    // Act - 再等待100ms
    await TimerComponent.Instance.WaitAsync(100);
    btComp.Update();

    // Assert - 又执行了1次
    Assert.AreEqual(2, mockNode.TickCount);
}
```

---

## 5. 测试辅助类

### 5.1 MockActionNode

```csharp
public class MockActionNode : BTAction
{
    public string Name;
    public BTStatus ReturnStatus;
    public bool Executed = false;

    public MockActionNode(string name, BTStatus returnStatus)
    {
        Name = name;
        ReturnStatus = returnStatus;
    }

    protected override BTStatus OnUpdate(BTContext context)
    {
        Executed = true;
        return ReturnStatus;
    }
}
```

### 5.2 MockBTNode

```csharp
public class MockBTNode : BTNode
{
    public int TickCount = 0;

    public override BTStatus Tick(BTContext context)
    {
        TickCount++;
        return BTStatus.Success;
    }
}
```

---

## 6. TDD实施步骤

### 第二周第一天：行为树系统

**Step 1: Red - 编写基础节点测试**
```bash
dotnet test --filter Test_SequenceNode
# 结果：BTSequence类不存在
```

**Step 2: Green - 实现BTSequence**
```csharp
public class BTSequence: BTComposite
{
    private int currentIndex = 0;

    public override BTStatus Tick(BTContext context)
    {
        while (currentIndex < children.Count)
        {
            BTStatus status = children[currentIndex].Tick(context);

            if (status == BTStatus.Failure)
            {
                currentIndex = 0;
                return BTStatus.Failure;
            }

            if (status == BTStatus.Running)
            {
                return BTStatus.Running;
            }

            currentIndex++;
        }

        currentIndex = 0;
        return BTStatus.Success;
    }
}
```

**Step 3: 重复完成其他节点和武器BT**

---

**文档版本**: v1.0
**测试用例数**: 10
**预计时间**: 6小时
**状态**: 待实施
