# TDD测试用例 - Part 4a: 集成测试

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 集成测试
- **优先级**: P1
- **测试用例数**: 6

---

## 1. 移动+射击集成测试

### 测试用例 4a.1: 移动中自动射击（冲锋枪）

**测试目的**: 验证使用冲锋枪时能边移动边自动射击

**前置条件**:
- Unit装备冲锋枪
- 有敌人在范围内
- Unit正在移动

**测试步骤**:
1. 创建Unit和敌人
2. Unit开始摇杆移动
3. 执行多次BT Tick
4. 检查是否射击

**预期结果**:
- Unit持续移动
- 自动选择目标
- 持续射击
- 弹药消耗

**测试代码**:
```csharp
[Test]
public async ETTask Test_MoveAndShoot_SMG()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);
    myUnit.AddComponent<NumericComponent>().Set(NumericType.Speed, 5f);

    // 装备冲锋枪
    WeaponComponent weaponComp = myUnit.AddComponent<WeaponComponent, int, int>(1001, 1002);
    weaponComp.CurrentWeapon = WeaponType.SMG;

    // 添加目标选择器
    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    // 创建敌人
    Unit enemy = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy.Position = new float3(5, 0, 0);
    enemy.AddComponent<CampComponent, int>(2);
    enemy.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    MockAOI(myUnit, new[] { enemy });

    // 配置冲锋枪
    MockWeaponConfig(1002, new WeaponConfig
    {
        Id = 1002,
        Type = WeaponType.SMG,
        AttackRange = 10f,
        AttackInterval = 0.1f,
        MagazineSize = 40,
        CanMoveWhileFire = true
    });

    // 添加BT
    myUnit.InitWeaponBT(1001, 1002);

    // 开始移动
    MoveComponent moveComp = myUnit.AddComponent<MoveComponent>();
    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0);

    float3 startPos = myUnit.Position;
    int startAmmo = weaponComp.SMGAmmo;

    // Act - 模拟1秒（10次BT Tick）
    for (int i = 0; i < 10; i++)
    {
        moveComp.TickJoystickMove();

        BTComponent btComp = myUnit.GetChild<BTComponent>();
        btComp.Update();

        await TimerComponent.Instance.WaitAsync(100);
    }

    // Assert
    // 1. 位置改变（移动了）
    Assert.IsTrue(math.distance(startPos, myUnit.Position) > 0.1f);

    // 2. 弹药消耗（射击了）
    Assert.IsTrue(weaponComp.SMGAmmo < startAmmo);

    // 3. 敌人受伤
    NumericComponent enemyNumeric = enemy.GetComponent<NumericComponent>();
    Assert.IsTrue(enemyNumeric.GetAsFloat(NumericType.Hp) < 100);
}
```

---

### 测试用例 4a.2: 静止射击（步枪）

**测试目的**: 验证使用步枪时必须静止才能射击

**前置条件**:
- Unit装备步枪
- 有敌人在范围内

**测试步骤**:
1. Unit移动中尝试射击
2. Unit停止后射击

**预期结果**:
- 移动中不射击
- 静止后自动射击

**测试代码**:
```csharp
[Test]
public async ETTask Test_StaticShoot_Rifle()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);

    WeaponComponent weaponComp = myUnit.AddComponent<WeaponComponent, int, int>(1001, 1002);
    weaponComp.CurrentWeapon = WeaponType.Rifle;

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    Unit enemy = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy.Position = new float3(5, 0, 0);
    enemy.AddComponent<CampComponent, int>(2);
    enemy.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    MockAOI(myUnit, new[] { enemy });

    MockWeaponConfig(1001, new WeaponConfig
    {
        Id = 1001,
        Type = WeaponType.Rifle,
        AttackRange = 10f,
        AttackInterval = 0.5f,
        MagazineSize = 30
    });

    myUnit.InitWeaponBT(1001, 1002);

    MoveComponent moveComp = myUnit.AddComponent<MoveComponent>();
    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0); // 移动中

    int startAmmo = weaponComp.RifleAmmo;

    // Act 1 - 移动中尝试射击
    BTComponent btComp = myUnit.GetChild<BTComponent>();
    for (int i = 0; i < 5; i++)
    {
        btComp.Update();
        await TimerComponent.Instance.WaitAsync(100);
    }

    // Assert 1 - 没有射击
    Assert.AreEqual(startAmmo, weaponComp.RifleAmmo);

    // Act 2 - 停止移动
    moveComp.JoystickDir = float3.zero;
    await TimerComponent.Instance.WaitAsync(100);
    btComp.Update();

    // Assert 2 - 开始射击
    Assert.IsTrue(weaponComp.RifleAmmo < startAmmo);
}
```

---

## 2. 多人战斗测试

### 测试用例 4a.3: 2v2战斗

**测试目的**: 验证多人战斗时阵营、目标选择、射击都正常工作

**前置条件**:
- 2个友方Unit
- 2个敌方Unit

**测试步骤**:
1. 创建2v2场景
2. 模拟战斗
3. 检查各系统协作

**预期结果**:
- 友方不互相攻击
- 自动选择敌方目标
- 正常造成伤害

**测试代码**:
```csharp
[Test]
public async ETTask Test_2v2Combat()
{
    // Arrange - 友方阵营
    Unit ally1 = UnitFactory.Create(scene, 1, UnitType.Player);
    ally1.Position = new float3(0, 0, 0);
    ally1.AddComponent<CampComponent, int>(1);
    SetupCombatUnit(ally1, 1001, 1002);

    Unit ally2 = UnitFactory.Create(scene, 2, UnitType.Player);
    ally2.Position = new float3(2, 0, 0);
    ally2.AddComponent<CampComponent, int>(1);
    SetupCombatUnit(ally2, 1001, 1002);

    // Arrange - 敌方阵营
    Unit enemy1 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy1.Position = new float3(10, 0, 0);
    enemy1.AddComponent<CampComponent, int>(2);
    SetupCombatUnit(enemy1, 1001, 1002);

    Unit enemy2 = UnitFactory.Create(scene, 4, UnitType.Player);
    enemy2.Position = new float3(12, 0, 0);
    enemy2.AddComponent<CampComponent, int>(2);
    SetupCombatUnit(enemy2, 1001, 1002);

    // 设置AOI（所有人都能看到所有人）
    Unit[] allUnits = { ally1, ally2, enemy1, enemy2 };
    foreach (Unit unit in allUnits)
    {
        MockAOI(unit, allUnits.Where(u => u.Id != unit.Id).ToArray());
    }

    // Act - 模拟3秒战斗
    for (int i = 0; i < 30; i++)
    {
        foreach (Unit unit in allUnits)
        {
            BTComponent btComp = unit.GetChild<BTComponent>();
            btComp?.Update();
        }
        await TimerComponent.Instance.WaitAsync(100);
    }

    // Assert
    // 1. 友方没有互相攻击
    NumericComponent ally1Numeric = ally1.GetComponent<NumericComponent>();
    NumericComponent ally2Numeric = ally2.GetComponent<NumericComponent>();
    Assert.AreEqual(100f, ally1Numeric.GetAsFloat(NumericType.Hp));
    Assert.AreEqual(100f, ally2Numeric.GetAsFloat(NumericType.Hp));

    // 2. 敌方受到伤害
    NumericComponent enemy1Numeric = enemy1.GetComponent<NumericComponent>();
    NumericComponent enemy2Numeric = enemy2.GetComponent<NumericComponent>();
    Assert.IsTrue(enemy1Numeric.GetAsFloat(NumericType.Hp) < 100f ||
                  enemy2Numeric.GetAsFloat(NumericType.Hp) < 100f);
}

private void SetupCombatUnit(Unit unit, int rifleId, int smgId)
{
    unit.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);
    unit.AddComponent<WeaponComponent, int, int>(rifleId, smgId);
    unit.AddComponent<TargetSelectorComponent>().MaxRange = 15f;
    unit.InitWeaponBT(rifleId, smgId);
}
```

---

### 测试用例 4a.4: 目标死亡后自动切换

**测试目的**: 验证目标死亡后能自动切换到下一个目标

**前置条件**:
- 1个友方
- 2个敌方

**测试步骤**:
1. 射击第一个敌人直到死亡
2. 检查是否自动切换到第二个敌人

**预期结果**:
- 第一个敌人死亡
- 自动选择第二个敌人
- 继续射击

**测试代码**:
```csharp
[Test]
public async ETTask Test_AutoSwitchTargetOnDeath()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);
    SetupCombatUnit(myUnit, 1001, 1002);

    // 敌人1（血量低，会先死）
    Unit enemy1 = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy1.Position = new float3(5, 0, 0);
    enemy1.AddComponent<CampComponent, int>(2);
    NumericComponent enemy1Numeric = enemy1.AddComponent<NumericComponent>();
    enemy1Numeric.Set(NumericType.Hp, 10); // 低血量
    enemy1Numeric.Set(NumericType.MaxHp, 100);

    // 敌人2
    Unit enemy2 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy2.Position = new float3(7, 0, 0);
    enemy2.AddComponent<CampComponent, int>(2);
    NumericComponent enemy2Numeric = enemy2.AddComponent<NumericComponent>();
    enemy2Numeric.Set(NumericType.Hp, 100);
    enemy2Numeric.Set(NumericType.MaxHp, 100);

    MockAOI(myUnit, new[] { enemy1, enemy2 });

    TargetSelectorComponent selector = myUnit.GetComponent<TargetSelectorComponent>();

    // Act - 持续战斗直到enemy1死亡
    for (int i = 0; i < 50; i++)
    {
        BTComponent btComp = myUnit.GetChild<BTComponent>();
        btComp.Update();

        await TimerComponent.Instance.WaitAsync(100);

        if (enemy1Numeric.GetAsFloat(NumericType.Hp) <= 0)
            break;
    }

    // Assert 1 - enemy1已死
    Assert.IsTrue(enemy1Numeric.GetAsFloat(NumericType.Hp) <= 0);

    // Act - 继续战斗
    await TimerComponent.Instance.WaitAsync(200);
    Unit currentTarget = selector.GetCurrentTarget();

    // Assert 2 - 自动切换到enemy2
    Assert.IsNotNull(currentTarget);
    Assert.AreEqual(enemy2.Id, currentTarget.Id);

    // Assert 3 - enemy2开始受伤
    Assert.IsTrue(enemy2Numeric.GetAsFloat(NumericType.Hp) < 100);
}
```

---

## 3. 网络同步测试

### 测试用例 4a.5: 客户端预测与服务端同步

**测试目的**: 验证客户端预测和服务端同步协作正常

**前置条件**:
- 客户端和服务端场景

**测试步骤**:
1. 客户端发送移动输入
2. 客户端预测移动
3. 服务端处理并广播
4. 客户端回正

**预期结果**:
- 客户端预测流畅
- 服务端位置权威
- 误差在可接受范围

**测试代码**:
```csharp
[Test]
public async ETTask Test_ClientPredictionAndServerSync()
{
    // Arrange - 服务端场景
    Scene serverScene = TestHelper.CreateTestScene();
    Unit serverUnit = UnitFactory.Create(serverScene, 1, UnitType.Player);
    serverUnit.Position = new float3(0, 0, 0);
    MoveComponent serverMove = serverUnit.AddComponent<MoveComponent>();
    serverUnit.AddComponent<NumericComponent>().Set(NumericType.Speed, 5f);

    // Arrange - 客户端场景
    Scene clientScene = TestHelper.CreateClientScene();
    Unit clientUnit = UnitFactory.Create(clientScene, 1, UnitType.Player);
    clientUnit.Position = new float3(0, 0, 0);
    MoveComponent clientMove = clientUnit.AddComponent<MoveComponent>();
    clientUnit.AddComponent<NumericComponent>().Set(NumericType.Speed, 5f);

    InputSystemComponent inputComp = clientScene.AddComponent<InputSystemComponent>();

    float3 moveDir = new float3(1, 0, 0);

    // Act - 客户端发送输入并预测
    inputComp.SendJoystickInput(moveDir);
    inputComp.IsMoving = true;

    // 客户端预测移动（5帧）
    for (int i = 0; i < 5; i++)
    {
        inputComp.Update();
        await TimerComponent.Instance.WaitFrameAsync();
    }

    float3 clientPredictedPos = clientUnit.Position;

    // 服务端处理输入
    C2M_JoystickInput input = C2M_JoystickInput.Create();
    input.MoveDir = moveDir;
    input.Seq = 1;
    await MessageHelper.CallActor(serverUnit, input);

    serverMove.StartJoystickMove();
    serverMove.JoystickDir = moveDir;

    // 服务端Tick移动（5次）
    for (int i = 0; i < 5; i++)
    {
        serverMove.TickJoystickMove();
        await TimerComponent.Instance.WaitAsync(33);
    }

    float3 serverPos = serverUnit.Position;

    // 服务端广播状态
    M2C_JoystickState msg = M2C_JoystickState.Create();
    msg.UnitId = clientUnit.Id;
    msg.Position = serverPos;
    msg.MoveDir = moveDir;
    msg.Seq = 1;

    clientUnit.OnJoystickState(msg);

    // Assert
    // 1. 客户端和服务端位置接近
    float distance = math.distance(clientUnit.Position, serverPos);
    Assert.IsTrue(distance < 0.5f, $"Position error: {distance}m");

    // 2. 客户端进行了回正
    Assert.AreNotEqual(clientPredictedPos, clientUnit.Position);
}
```

---

### 测试用例 4a.6: 断线重连恢复

**测试目的**: 验证断线重连后状态正确恢复

**前置条件**:
- Unit正在移动和战斗

**测试步骤**:
1. Unit正常移动射击
2. 模拟断线
3. 重连
4. 检查状态

**预期结果**:
- 移动停止
- 状态同步
- 可以继续游戏

**测试代码**:
```csharp
[Test]
public async ETTask Test_ReconnectRecovery()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    unit.Position = new float3(5, 0, 5);

    MoveComponent moveComp = unit.AddComponent<MoveComponent>();
    moveComp.StartJoystickMove();
    moveComp.JoystickDir = new float3(1, 0, 0);

    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    // 移动一段时间
    for (int i = 0; i < 5; i++)
    {
        moveComp.TickJoystickMove();
        await TimerComponent.Instance.WaitAsync(33);
    }

    float3 posBeforeDisconnect = unit.Position;

    // Act - 模拟断线重连
    unit.OnReconnect();

    // Assert
    // 1. 移动已停止
    Assert.AreEqual(float3.zero, moveComp.JoystickDir);
    Assert.AreEqual(0, moveComp.LastSeq);

    // 2. 位置保持
    Assert.AreEqual(posBeforeDisconnect, unit.Position);

    // 3. 武器状态保持
    Assert.IsNotNull(weaponComp);
}
```

---

## 4. 测试辅助方法

### 4.1 SetupCombatUnit

```csharp
private void SetupCombatUnit(Unit unit, int rifleId, int smgId)
{
    NumericComponent numeric = unit.AddComponent<NumericComponent>();
    numeric.Set(NumericType.Hp, 100);
    numeric.Set(NumericType.MaxHp, 100);
    numeric.Set(NumericType.Speed, 5f);

    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(rifleId, smgId);
    weaponComp.CurrentWeapon = WeaponType.SMG;

    TargetSelectorComponent selector = unit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 15f;

    unit.InitWeaponBT(rifleId, smgId);
}
```

---

## 5. TDD实施步骤

### 第二周第三天：集成测试

**Step 1: 先确保单元测试全部通过**
```bash
dotnet test --filter "Test_*" | grep "Passed"
```

**Step 2: 编写第一个集成测试**
```bash
dotnet test --filter Test_MoveAndShoot_SMG
```

**Step 3: 修复集成问题**
- 检查组件依赖
- 检查初始化顺序
- 检查Timer生命周期

**Step 4: 逐个完成集成测试**

---

**文档版本**: v1.0
**测试用例数**: 6
**预计时间**: 4小时
**状态**: 待实施
