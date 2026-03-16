# TDD测试用例 - Part 2c: 武器系统

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 武器系统
- **优先级**: P0
- **测试用例数**: 8

---

## 1. 弹药管理测试

### 测试用例 2c.1: 消耗弹药

**测试目的**: 验证射击时能正确消耗弹药

**前置条件**:
- WeaponComponent已初始化
- 步枪弹药30发

**测试步骤**:
1. 创建WeaponComponent
2. 消耗1发弹药
3. 检查弹药数量

**预期结果**:
- 弹药减少1发
- 弹药不会小于0

**测试代码**:
```csharp
[Test]
public void Test_ConsumeAmmo()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    int initialAmmo = weaponComp.RifleAmmo;
    Assert.AreEqual(30, initialAmmo); // 步枪初始30发

    // Act
    weaponComp.ConsumeAmmo(1001, 1);

    // Assert
    Assert.AreEqual(29, weaponComp.RifleAmmo);

    // Act - 消耗到0
    weaponComp.ConsumeAmmo(1001, 29);
    Assert.AreEqual(0, weaponComp.RifleAmmo);

    // Act - 尝试消耗负数（不应该小于0）
    weaponComp.ConsumeAmmo(1001, 1);
    Assert.AreEqual(0, weaponComp.RifleAmmo);
}
```

---

### 测试用例 2c.2: 补充弹药

**测试目的**: 验证换弹后能补充满弹药

**前置条件**:
- 弹药已消耗部分

**测试步骤**:
1. 消耗部分弹药
2. 调用RefillAmmo
3. 检查弹药是否补满

**预期结果**:
- 弹药恢复到弹匣容量

**测试代码**:
```csharp
[Test]
public void Test_RefillAmmo()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    // 消耗弹药
    weaponComp.ConsumeAmmo(1001, 20);
    Assert.AreEqual(10, weaponComp.RifleAmmo);

    // Act - 补充弹药
    weaponComp.RefillAmmo(1001);

    // Assert
    Assert.AreEqual(30, weaponComp.RifleAmmo); // 恢复到弹匣容量
}
```

---

### 测试用例 2c.3: 双武器独立弹药管理

**测试目的**: 验证步枪和冲锋枪的弹药独立管理

**前置条件**:
- 同时装备步枪和冲锋枪

**测试步骤**:
1. 消耗步枪弹药
2. 检查冲锋枪弹药不受影响

**预期结果**:
- 两种武器弹药互不影响

**测试代码**:
```csharp
[Test]
public void Test_DualWeaponAmmo()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    int initialRifleAmmo = weaponComp.RifleAmmo;
    int initialSMGAmmo = weaponComp.SMGAmmo;

    // Act - 消耗步枪弹药
    weaponComp.ConsumeAmmo(1001, 10);

    // Assert
    Assert.AreEqual(initialRifleAmmo - 10, weaponComp.RifleAmmo);
    Assert.AreEqual(initialSMGAmmo, weaponComp.SMGAmmo); // 冲锋枪不受影响
}
```

---

## 2. 射击间隔测试

### 测试用例 2c.4: 射击间隔限制

**测试目的**: 验证武器射击受攻击间隔限制

**前置条件**:
- 步枪攻击间隔0.5秒

**测试步骤**:
1. 第一次射击
2. 立即尝试第二次射击
3. 等待0.5秒后再射击

**预期结果**:
- 间隔不足时CanFire返回false
- 间隔足够时CanFire返回true

**测试代码**:
```csharp
[Test]
public async ETTask Test_FireInterval()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    // Mock配置：攻击间隔0.5秒
    MockWeaponConfig(1001, new WeaponConfig
    {
        Id = 1001,
        AttackInterval = 0.5f
    });

    // Act 1 - 第一次射击
    Assert.IsTrue(weaponComp.CanFire(1001));
    weaponComp.RecordFireTime(1001);

    // Act 2 - 立即尝试第二次射击
    Assert.IsFalse(weaponComp.CanFire(1001)); // 间隔不足

    // Act 3 - 等待0.5秒
    await TimerComponent.Instance.WaitAsync(500);

    // Assert
    Assert.IsTrue(weaponComp.CanFire(1001)); // 可以射击了
}
```

---

### 测试用例 2c.5: 不同武器独立射击间隔

**测试目的**: 验证步枪和冲锋枪的射击间隔独立计算

**前置条件**:
- 步枪间隔0.5秒，冲锋枪间隔0.1秒

**测试步骤**:
1. 步枪射击
2. 立即切换冲锋枪射击

**预期结果**:
- 冲锋枪不受步枪射击时间影响

**测试代码**:
```csharp
[Test]
public void Test_IndependentFireInterval()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    MockWeaponConfig(1001, new WeaponConfig { Id = 1001, AttackInterval = 0.5f });
    MockWeaponConfig(1002, new WeaponConfig { Id = 1002, AttackInterval = 0.1f });

    // Act - 步枪射击
    weaponComp.RecordFireTime(1001);
    Assert.IsFalse(weaponComp.CanFire(1001)); // 步枪CD中

    // Assert - 冲锋枪不受影响
    Assert.IsTrue(weaponComp.CanFire(1002)); // 冲锋枪可以射击
}
```

---

## 3. 换弹状态测试

### 测试用例 2c.6: 换弹期间不能射击

**测试目的**: 验证换弹期间CanFire返回false

**前置条件**:
- 武器正在换弹

**测试步骤**:
1. 设置换弹状态
2. 检查CanFire

**预期结果**:
- 换弹期间不能射击

**测试代码**:
```csharp
[Test]
public void Test_CannotFireWhileReloading()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    MockWeaponConfig(1001, new WeaponConfig
    {
        Id = 1001,
        AttackInterval = 0.5f,
        ReloadTime = 2.0f
    });

    // Act - 开始换弹
    weaponComp.SetReloading(1001, true);

    // Assert
    Assert.IsFalse(weaponComp.CanFire(1001)); // 换弹期间不能射击
    Assert.IsTrue(weaponComp.IsReloading(1001));

    // Act - 换弹完成
    weaponComp.SetReloading(1001, false);

    // Assert
    Assert.IsTrue(weaponComp.CanFire(1001)); // 可以射击了
}
```

---

### 测试用例 2c.7: 弹药为0不能射击

**测试目的**: 验证弹药为0时CanFire返回false

**前置条件**:
- 弹药已耗尽

**测试步骤**:
1. 消耗所有弹药
2. 检查CanFire

**预期结果**:
- 弹药为0不能射击

**测试代码**:
```csharp
[Test]
public void Test_CannotFireWithoutAmmo()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    MockWeaponConfig(1001, new WeaponConfig
    {
        Id = 1001,
        AttackInterval = 0.5f
    });

    // Act - 消耗所有弹药
    weaponComp.ConsumeAmmo(1001, 30);

    // Assert
    Assert.AreEqual(0, weaponComp.GetAmmo(1001));
    Assert.IsFalse(weaponComp.CanFire(1001)); // 没弹药不能射击
}
```

---

## 4. 武器切换测试

### 测试用例 2c.8: 切换武器

**测试目的**: 验证能正确切换当前装备的武器

**前置条件**:
- 同时装备步枪和冲锋枪

**测试步骤**:
1. 初始装备步枪
2. 切换到冲锋枪
3. 检查当前武器

**预期结果**:
- CurrentWeapon正确更新

**测试代码**:
```csharp
[Test]
public void Test_SwitchWeapon()
{
    // Arrange
    Unit unit = UnitFactory.Create(scene, 1, UnitType.Player);
    WeaponComponent weaponComp = unit.AddComponent<WeaponComponent, int, int>(1001, 1002);

    // 初始装备步枪
    Assert.AreEqual(WeaponType.Rifle, weaponComp.CurrentWeapon);

    // Act - 切换到冲锋枪
    unit.SwitchWeapon(WeaponType.SMG);

    // Assert
    Assert.AreEqual(WeaponType.SMG, weaponComp.CurrentWeapon);

    // 检查BT也切换了
    BTComponent btComp = unit.GetChild<BTComponent>();
    Assert.IsNotNull(btComp);
    Assert.AreEqual(1002, btComp.ConfigId); // 冲锋枪的BT
}
```

---

## 5. 子弹创建测试

### 测试用例 2c.9: 创建子弹

**测试目的**: 验证能正确创建子弹实体

**前置条件**:
- 有射击者和目标

**测试步骤**:
1. 创建射击者和目标
2. 调用CreateBullet
3. 检查子弹属性

**预期结果**:
- 子弹正确创建
- 子弹属性正确设置

**测试代码**:
```csharp
[Test]
public void Test_CreateBullet()
{
    // Arrange
    Unit owner = UnitFactory.Create(scene, 1, UnitType.Player);
    owner.Position = new float3(0, 0, 0);

    Unit target = UnitFactory.Create(scene, 2, UnitType.Player);
    target.Position = new float3(10, 0, 0);

    WeaponConfig weaponConfig = new WeaponConfig
    {
        Id = 1001,
        Damage = 50f,
        ProjectilePrefab = "Bullet_Rifle"
    };

    // Act
    Unit bullet = UnitFactory.CreateBullet(scene, owner, target, weaponConfig);

    // Assert
    Assert.IsNotNull(bullet);
    Assert.AreEqual(owner.Position, bullet.Position);

    BulletComponent bulletComp = bullet.GetComponent<BulletComponent>();
    Assert.IsNotNull(bulletComp);
    Assert.AreEqual(owner.Id, bulletComp.OwnerId);
    Assert.AreEqual(target.Id, bulletComp.TargetId);
    Assert.AreEqual(50f, bulletComp.Damage);
}
```

---

### 测试用例 2c.10: 子弹移动和命中

**测试目的**: 验证子弹能正确移动并命中目标

**前置条件**:
- 子弹已创建

**测试步骤**:
1. 创建子弹
2. 模拟多帧Update
3. 检查是否命中

**预期结果**:
- 子弹向目标移动
- 命中后造成伤害
- 子弹被销毁

**测试代码**:
```csharp
[Test]
public async ETTask Test_BulletHit()
{
    // Arrange
    Unit owner = UnitFactory.Create(scene, 1, UnitType.Player);
    owner.Position = new float3(0, 0, 0);

    Unit target = UnitFactory.Create(scene, 2, UnitType.Player);
    target.Position = new float3(5, 0, 0); // 距离5米

    NumericComponent targetNumeric = target.AddComponent<NumericComponent>();
    targetNumeric.Set(NumericType.Hp, 100);

    WeaponConfig weaponConfig = new WeaponConfig
    {
        Id = 1001,
        Damage = 30f
    };

    // Act - 创建子弹
    Unit bullet = UnitFactory.CreateBullet(scene, owner, target, weaponConfig);
    BulletComponent bulletComp = bullet.GetComponent<BulletComponent>();
    bulletComp.Speed = 20f; // 20m/s

    // 模拟移动（5米 / 20m/s = 0.25秒）
    for (int i = 0; i < 10; i++) // 10帧，每帧0.033秒
    {
        bulletComp.Update();
        await TimerComponent.Instance.WaitFrameAsync();

        if (bullet.IsDisposed)
            break;
    }

    // Assert
    Assert.IsTrue(bullet.IsDisposed); // 子弹已销毁
    Assert.AreEqual(70f, targetNumeric.GetAsFloat(NumericType.Hp)); // 造成30伤害
}
```

---

## 6. 测试辅助方法

### 6.1 MockWeaponConfig

```csharp
private static Dictionary<int, WeaponConfig> mockWeaponConfigs = new Dictionary<int, WeaponConfig>();

public static void MockWeaponConfig(int id, WeaponConfig config)
{
    mockWeaponConfigs[id] = config;
}

public class MockWeaponConfigCategory
{
    public WeaponConfig Get(int id)
    {
        return mockWeaponConfigs.TryGetValue(id, out var config) ? config : null;
    }
}
```

### 6.2 WeaponConfig定义

```csharp
public class WeaponConfig
{
    public int Id;
    public string Name;
    public WeaponType Type;
    public float Damage;
    public float AttackRange;
    public float AttackInterval;
    public int MagazineSize;
    public float ReloadTime;
    public bool CanMoveWhileFire;
    public int BTConfigId;
    public string ProjectilePrefab;
}
```

---

## 7. TDD实施步骤

### 第二天上午：武器系统

**Step 1: Red - 编写测试用例 2c.1**
```bash
dotnet test --filter Test_ConsumeAmmo
# 结果：WeaponComponent不存在
```

**Step 2: Green - 实现WeaponComponent**
```csharp
[ComponentOf(typeof(Unit))]
public class WeaponComponent: Entity, IAwake<int, int>
{
    public int RifleId;
    public int SMGId;
    public WeaponType CurrentWeapon = WeaponType.Rifle;
    public int RifleAmmo;
    public int SMGAmmo;
    public bool RifleReloading;
    public bool SMGReloading;
    public long RifleLastFireTime;
    public long SMGLastFireTime;
}
```

**Step 3: Green - 实现ConsumeAmmo**
```csharp
public static void ConsumeAmmo(this WeaponComponent self, int weaponId, int count)
{
    if (weaponId == self.RifleId)
    {
        self.RifleAmmo = math.max(0, self.RifleAmmo - count);
    }
    else if (weaponId == self.SMGId)
    {
        self.SMGAmmo = math.max(0, self.SMGAmmo - count);
    }
}
```

**Step 4: Refactor - 优化代码结构**
- 提取GetAmmo方法
- 添加配置表读取
- 完善错误处理

**Step 5: 重复完成其他测试用例**

---

**文档版本**: v1.0
**测试用例数**: 8
**预计时间**: 4小时
**状态**: 待实施
