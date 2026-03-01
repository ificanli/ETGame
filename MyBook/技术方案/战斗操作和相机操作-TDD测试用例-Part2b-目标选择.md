# TDD测试用例 - Part 2b: 目标选择系统

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 目标选择系统
- **优先级**: P0
- **测试用例数**: 6

---

## 1. 自动目标选择测试

### 测试用例 2b.1: 选择范围内最近的敌人

**测试目的**: 验证TargetSelector能自动选择范围内最近的敌人

**前置条件**:
- Unit有TargetSelectorComponent
- 范围内有多个敌人

**测试步骤**:
1. 创建自己的Unit（位置0,0,0）
2. 创建3个敌人（距离分别为5m, 8m, 3m）
3. 调用SelectTarget

**预期结果**:
- 选择距离3m的敌人

**测试代码**:
```csharp
[Test]
public void Test_SelectNearestEnemy()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1); // 阵营1

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    // 创建3个敌人
    Unit enemy1 = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy1.Position = new float3(5, 0, 0); // 距离5m
    enemy1.AddComponent<CampComponent, int>(2);
    enemy1.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    Unit enemy2 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy2.Position = new float3(8, 0, 0); // 距离8m
    enemy2.AddComponent<CampComponent, int>(2);
    enemy2.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    Unit enemy3 = UnitFactory.Create(scene, 4, UnitType.Player);
    enemy3.Position = new float3(3, 0, 0); // 距离3m
    enemy3.AddComponent<CampComponent, int>(2);
    enemy3.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    // 添加到AOI
    MockAOI(myUnit, new[] { enemy1, enemy2, enemy3 });

    // Act
    Unit target = selector.SelectTarget();

    // Assert
    Assert.AreEqual(enemy3.Id, target.Id); // 选择最近的
}
```

---

### 测试用例 2b.2: 优先选择血量低的敌人

**测试目的**: 验证在距离相近时优先选择血量低的敌人

**前置条件**:
- 两个敌人距离相近（差距<1m）
- 血量不同

**测试步骤**:
1. 创建两个距离相近的敌人
2. 设置不同血量
3. 调用SelectTarget

**预期结果**:
- 选择血量更低的敌人

**测试代码**:
```csharp
[Test]
public void Test_SelectLowHpEnemy()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    // 两个距离相近的敌人
    Unit enemy1 = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy1.Position = new float3(5, 0, 0);
    enemy1.AddComponent<CampComponent, int>(2);
    NumericComponent numeric1 = enemy1.AddComponent<NumericComponent>();
    numeric1.Set(NumericType.Hp, 100);
    numeric1.Set(NumericType.MaxHp, 100);

    Unit enemy2 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy2.Position = new float3(5.5f, 0, 0); // 距离相近
    enemy2.AddComponent<CampComponent, int>(2);
    NumericComponent numeric2 = enemy2.AddComponent<NumericComponent>();
    numeric2.Set(NumericType.Hp, 30); // 血量更低
    numeric2.Set(NumericType.MaxHp, 100);

    MockAOI(myUnit, new[] { enemy1, enemy2 });

    // Act
    Unit target = selector.SelectTarget();

    // Assert
    Assert.AreEqual(enemy2.Id, target.Id); // 选择血量低的
}
```

---

### 测试用例 2b.3: 优先选择正在攻击自己的敌人

**测试目的**: 验证优先选择正在攻击自己的敌人

**前置条件**:
- 多个敌人
- 其中一个正在攻击自己

**测试步骤**:
1. 创建多个敌人
2. 设置其中一个的目标为自己
3. 调用SelectTarget

**预期结果**:
- 选择正在攻击自己的敌人

**测试代码**:
```csharp
[Test]
public void Test_SelectAttackingEnemy()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    // 敌人1：距离近但未攻击
    Unit enemy1 = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy1.Position = new float3(3, 0, 0);
    enemy1.AddComponent<CampComponent, int>(2);
    enemy1.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    // 敌人2：距离远但正在攻击自己
    Unit enemy2 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy2.Position = new float3(7, 0, 0);
    enemy2.AddComponent<CampComponent, int>(2);
    enemy2.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    TargetSelectorComponent enemy2Selector = enemy2.AddComponent<TargetSelectorComponent>();
    enemy2Selector.CurrentTargetId = myUnit.Id; // 正在攻击自己

    MockAOI(myUnit, new[] { enemy1, enemy2 });

    // Act
    Unit target = selector.SelectTarget();

    // Assert
    Assert.AreEqual(enemy2.Id, target.Id); // 优先选择攻击自己的
}
```

---

### 测试用例 2b.4: 超出范围的敌人不被选择

**测试目的**: 验证超出MaxRange的敌人不会被选择

**前置条件**:
- MaxRange = 10m
- 敌人距离 > 10m

**测试步骤**:
1. 设置MaxRange为10m
2. 创建距离15m的敌人
3. 调用SelectTarget

**预期结果**:
- 返回null

**测试代码**:
```csharp
[Test]
public void Test_OutOfRangeEnemy()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    Unit enemy = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy.Position = new float3(15, 0, 0); // 超出范围
    enemy.AddComponent<CampComponent, int>(2);
    enemy.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    MockAOI(myUnit, new[] { enemy });

    // Act
    Unit target = selector.SelectTarget();

    // Assert
    Assert.IsNull(target);
}
```

---

## 2. 手动目标切换测试

### 测试用例 2b.5: 手动指定目标

**测试目的**: 验证能手动指定目标并优先使用

**前置条件**:
- 有多个可选目标

**测试步骤**:
1. 创建多个敌人
2. 手动指定其中一个
3. 调用SelectTarget

**预期结果**:
- 返回手动指定的目标

**测试代码**:
```csharp
[Test]
public void Test_ManualTargetSelection()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    Unit enemy1 = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy1.Position = new float3(3, 0, 0); // 更近
    enemy1.AddComponent<CampComponent, int>(2);
    enemy1.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    Unit enemy2 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy2.Position = new float3(7, 0, 0); // 更远
    enemy2.AddComponent<CampComponent, int>(2);
    enemy2.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    MockAOI(myUnit, new[] { enemy1, enemy2 });

    // Act - 手动指定enemy2
    selector.SetManualTarget(enemy2.Id);
    Unit target = selector.SelectTarget();

    // Assert
    Assert.AreEqual(enemy2.Id, target.Id); // 返回手动指定的
}
```

---

### 测试用例 2b.6: 手动目标无效时自动切换

**测试目的**: 验证手动目标死亡或超出范围时自动切换到其他目标

**前置条件**:
- 已手动指定目标
- 手动目标变为无效

**测试步骤**:
1. 手动指定目标
2. 目标死亡（Hp=0）
3. 调用SelectTarget

**预期结果**:
- 自动选择其他有效目标
- ManualTargetId被清零

**测试代码**:
```csharp
[Test]
public void Test_ManualTargetInvalidAutoSwitch()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.Position = new float3(0, 0, 0);
    myUnit.AddComponent<CampComponent, int>(1);

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.MaxRange = 10f;

    Unit enemy1 = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy1.Position = new float3(3, 0, 0);
    enemy1.AddComponent<CampComponent, int>(2);
    enemy1.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    Unit enemy2 = UnitFactory.Create(scene, 3, UnitType.Player);
    enemy2.Position = new float3(5, 0, 0);
    enemy2.AddComponent<CampComponent, int>(2);
    NumericComponent numeric2 = enemy2.AddComponent<NumericComponent>();
    numeric2.Set(NumericType.Hp, 100);

    MockAOI(myUnit, new[] { enemy1, enemy2 });

    // 手动指定enemy2
    selector.SetManualTarget(enemy2.Id);
    Assert.AreEqual(enemy2.Id, selector.SelectTarget().Id);

    // Act - enemy2死亡
    numeric2.Set(NumericType.Hp, 0);
    Unit newTarget = selector.SelectTarget();

    // Assert
    Assert.AreEqual(enemy1.Id, newTarget.Id); // 自动切换到enemy1
    Assert.AreEqual(0, selector.ManualTargetId); // 手动目标被清除
}
```

---

## 3. 选择频率测试

### 测试用例 2b.7: 选择间隔限制

**测试目的**: 验证目标选择受间隔限制（1秒）

**前置条件**:
- SelectIntervalMs = 1000

**测试步骤**:
1. 第一次选择目标
2. 立即再次选择
3. 等待1秒后再选择

**预期结果**:
- 间隔<1秒返回缓存目标
- 间隔>1秒重新选择

**测试代码**:
```csharp
[Test]
public async ETTask Test_SelectInterval()
{
    // Arrange
    Unit myUnit = UnitFactory.Create(scene, 1, UnitType.Player);
    myUnit.AddComponent<CampComponent, int>(1);

    TargetSelectorComponent selector = myUnit.AddComponent<TargetSelectorComponent>();
    selector.SelectInterval = 1000; // 1秒
    selector.MaxRange = 10f;

    Unit enemy = UnitFactory.Create(scene, 2, UnitType.Player);
    enemy.Position = new float3(5, 0, 0);
    enemy.AddComponent<CampComponent, int>(2);
    enemy.AddComponent<NumericComponent>().Set(NumericType.Hp, 100);

    MockAOI(myUnit, new[] { enemy });

    // Act 1 - 第一次选择
    Unit target1 = selector.SelectTarget();
    long firstSelectTime = selector.LastSelectTime;

    // Act 2 - 立即再次选择
    Unit target2 = selector.SelectTarget();

    // Assert 1 - 返回缓存目标，时间未更新
    Assert.AreEqual(target1.Id, target2.Id);
    Assert.AreEqual(firstSelectTime, selector.LastSelectTime);

    // Act 3 - 等待1秒后选择
    await TimerComponent.Instance.WaitAsync(1000);
    Unit target3 = selector.SelectTarget();

    // Assert 2 - 重新选择，时间已更新
    Assert.IsTrue(selector.LastSelectTime > firstSelectTime);
}
```

---

## 4. 测试辅助方法

### 4.1 MockAOI

```csharp
public static void MockAOI(Unit unit, Unit[] visibleUnits)
{
    // Mock AOI系统，让unit能看到visibleUnits
    AOIComponent aoiComp = unit.GetComponent<AOIComponent>();
    if (aoiComp == null)
    {
        aoiComp = unit.AddComponent<AOIComponent>();
    }

    foreach (Unit visibleUnit in visibleUnits)
    {
        AOIEntity aoiEntity = new AOIEntity();
        aoiEntity.Unit = visibleUnit;
        aoiComp.AddSeePlayers(aoiEntity);
    }
}
```

---

## 5. TDD实施步骤

### 第一天下午：目标选择系统

**Step 1: Red - 编写测试用例 2b.1**
```bash
dotnet test --filter Test_SelectNearestEnemy
# 结果：TargetSelectorComponent不存在
```

**Step 2: Green - 实现基础TargetSelectorComponent**
```csharp
[ComponentOf(typeof(Unit))]
public class TargetSelectorComponent: Entity, IAwake
{
    public long CurrentTargetId;
    public long ManualTargetId;
    public long LastSelectTime;
    public int SelectInterval = 1000;
    public float MaxRange = 10.0f;
}
```

**Step 3: Green - 实现SelectTarget方法**
```csharp
public static Unit SelectTarget(this TargetSelectorComponent self)
{
    // 最小实现：选择最近的敌人
    Unit owner = self.GetParent<Unit>();
    List<Unit> enemies = GetEnemiesInRange(owner, self.MaxRange);

    if (enemies.Count == 0)
        return null;

    Unit nearest = enemies[0];
    float minDist = math.distance(owner.Position, nearest.Position);

    foreach (Unit enemy in enemies)
    {
        float dist = math.distance(owner.Position, enemy.Position);
        if (dist < minDist)
        {
            minDist = dist;
            nearest = enemy;
        }
    }

    self.CurrentTargetId = nearest.Id;
    return nearest;
}
```

**Step 4: Refactor - 添加优先级算法**
- 实现距离+血量权重计算
- 实现攻击自己的优先级加成
- 提取配置参数

**Step 5: 重复完成其他测试用例**

---

**文档版本**: v1.0
**测试用例数**: 6
**预计时间**: 3小时
**状态**: 待实施
