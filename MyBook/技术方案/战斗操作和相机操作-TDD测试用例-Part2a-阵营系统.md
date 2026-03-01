# TDD测试用例 - Part 2a: 阵营系统

## 文档信息

- **所属**: 战斗操作和相机操作TDD测试用例
- **模块**: 阵营系统
- **优先级**: P0
- **测试用例数**: 4

---

## 1. 阵营关系判断测试

### 测试用例 2a.1: 判断敌对关系

**测试目的**: 验证CampHelper能正确判断两个Unit是否敌对

**前置条件**:
- 两个Unit分别属于不同阵营
- 阵营类型都不是Neutral

**测试步骤**:
1. 创建阵营1的Unit
2. 创建阵营2的Unit
3. 调用CampHelper.IsEnemy判断

**预期结果**:
- 不同阵营返回true
- 相同阵营返回false
- 中立阵营返回false

**测试代码**:
```csharp
[Test]
public void Test_IsEnemy()
{
    // Arrange
    Unit unit1 = UnitFactory.Create(scene, 1, UnitType.Player);
    Unit unit2 = UnitFactory.Create(scene, 2, UnitType.Player);
    Unit unit3 = UnitFactory.Create(scene, 3, UnitType.Player);

    CampComponent camp1 = unit1.AddComponent<CampComponent, int>(1); // 阵营1
    CampComponent camp2 = unit2.AddComponent<CampComponent, int>(2); // 阵营2
    CampComponent camp3 = unit3.AddComponent<CampComponent, int>(1); // 阵营1

    // Act & Assert
    Assert.IsTrue(CampHelper.IsEnemy(unit1, unit2));   // 不同阵营，敌对
    Assert.IsFalse(CampHelper.IsEnemy(unit1, unit3));  // 相同阵营，不敌对
    Assert.IsFalse(CampHelper.IsEnemy(unit2, unit3));  // 不同阵营，但会检查
}
```

---

### 测试用例 2a.2: 判断友方关系

**测试目的**: 验证CampHelper能正确判断两个Unit是否友方

**前置条件**:
- 两个Unit已创建并分配阵营

**测试步骤**:
1. 创建相同阵营的两个Unit
2. 调用CampHelper.IsFriendly判断

**预期结果**:
- 相同阵营返回true
- 不同阵营返回false

**测试代码**:
```csharp
[Test]
public void Test_IsFriendly()
{
    // Arrange
    Unit unit1 = UnitFactory.Create(scene, 1, UnitType.Player);
    Unit unit2 = UnitFactory.Create(scene, 2, UnitType.Player);
    Unit unit3 = UnitFactory.Create(scene, 3, UnitType.Player);

    unit1.AddComponent<CampComponent, int>(1); // 阵营1
    unit2.AddComponent<CampComponent, int>(1); // 阵营1
    unit3.AddComponent<CampComponent, int>(2); // 阵营2

    // Act & Assert
    Assert.IsTrue(CampHelper.IsFriendly(unit1, unit2));   // 相同阵营
    Assert.IsFalse(CampHelper.IsFriendly(unit1, unit3));  // 不同阵营
}
```

---

### 测试用例 2a.3: 中立阵营不与任何人敌对

**测试目的**: 验证中立阵营不与任何阵营敌对

**前置条件**:
- 存在中立阵营的Unit

**测试步骤**:
1. 创建中立阵营Unit
2. 创建其他阵营Unit
3. 判断敌对关系

**预期结果**:
- 中立阵营与任何阵营都不敌对

**测试代码**:
```csharp
[Test]
public void Test_NeutralCamp()
{
    // Arrange
    Unit neutralUnit = UnitFactory.Create(scene, 1, UnitType.Monster);
    Unit playerUnit = UnitFactory.Create(scene, 2, UnitType.Player);

    CampComponent neutralCamp = neutralUnit.AddComponent<CampComponent, int>(3); // 阵营3=中立
    neutralCamp.CampType = CampType.Neutral;

    playerUnit.AddComponent<CampComponent, int>(1); // 阵营1

    // Act & Assert
    Assert.IsFalse(CampHelper.IsEnemy(neutralUnit, playerUnit));
    Assert.IsFalse(CampHelper.IsEnemy(playerUnit, neutralUnit));
    Assert.IsTrue(CampHelper.IsNeutral(neutralUnit));
}
```

---

### 测试用例 2a.4: 获取阵营关系枚举

**测试目的**: 验证GetRelation能返回正确的阵营关系枚举

**前置条件**:
- 多个不同阵营的Unit

**测试步骤**:
1. 创建不同阵营的Unit
2. 调用GetRelation获取关系

**预期结果**:
- 返回正确的CampRelation枚举值

**测试代码**:
```csharp
[Test]
public void Test_GetRelation()
{
    // Arrange
    Unit unit1 = UnitFactory.Create(scene, 1, UnitType.Player);
    Unit unit2 = UnitFactory.Create(scene, 2, UnitType.Player);
    Unit unit3 = UnitFactory.Create(scene, 3, UnitType.Monster);

    unit1.AddComponent<CampComponent, int>(1); // 阵营1
    unit2.AddComponent<CampComponent, int>(1); // 阵营1

    CampComponent camp3 = unit3.AddComponent<CampComponent, int>(3);
    camp3.CampType = CampType.Neutral; // 中立

    // Act & Assert
    Assert.AreEqual(CampRelation.Friendly, CampHelper.GetRelation(unit1, unit2));
    Assert.AreEqual(CampRelation.Neutral, CampHelper.GetRelation(unit1, unit3));
}
```

---

## 2. 阵营配置测试

### 测试用例 2a.5: 从配置表读取阵营类型

**测试目的**: 验证CampComponent能从配置表正确读取阵营类型

**前置条件**:
- 配置表已加载

**测试步骤**:
1. Mock配置表数据
2. 创建CampComponent
3. 检查阵营类型

**预期结果**:
- 阵营类型与配置表一致

**测试代码**:
```csharp
[Test]
public void Test_LoadCampFromConfig()
{
    // Arrange
    MockConfigCategory<CampConfig> mockConfig = new MockConfigCategory<CampConfig>();
    mockConfig.Add(1, new CampConfig
    {
        Id = 1,
        Name = "友方",
        Type = CampType.Friendly
    });
    mockConfig.Add(2, new CampConfig
    {
        Id = 2,
        Name = "敌方",
        Type = CampType.Enemy
    });

    CampConfigCategory.Instance = mockConfig;

    Unit unit1 = UnitFactory.Create(scene, 1, UnitType.Player);
    Unit unit2 = UnitFactory.Create(scene, 2, UnitType.Player);

    // Act
    CampComponent camp1 = unit1.AddComponent<CampComponent, int>(1);
    CampComponent camp2 = unit2.AddComponent<CampComponent, int>(2);

    // Assert
    Assert.AreEqual(CampType.Friendly, camp1.CampType);
    Assert.AreEqual(CampType.Enemy, camp2.CampType);
}
```

---

## 3. 测试辅助类

### 3.1 CampConfig Mock

```csharp
public class CampConfig
{
    public int Id;
    public string Name;
    public CampType Type;
    public int[] EnemyCamps;
}

public class MockCampConfigCategory
{
    private Dictionary<int, CampConfig> configs = new Dictionary<int, CampConfig>();

    public void Add(int id, CampConfig config)
    {
        configs[id] = config;
    }

    public CampConfig Get(int id)
    {
        return configs.TryGetValue(id, out var config) ? config : null;
    }
}
```

---

## 4. TDD实施步骤

### 第一天上午：阵营系统

**Step 1: Red - 编写测试用例 2a.1**
```bash
# 运行测试，预期失败
dotnet test --filter Test_IsEnemy
# 结果：CampHelper类不存在
```

**Step 2: Green - 实现CampHelper.IsEnemy**
```csharp
public static class CampHelper
{
    public static bool IsEnemy(Unit unit1, Unit unit2)
    {
        if (unit1 == null || unit2 == null)
            return false;

        CampComponent camp1 = unit1.GetComponent<CampComponent>();
        CampComponent camp2 = unit2.GetComponent<CampComponent>();

        if (camp1 == null || camp2 == null)
            return false;

        if (camp1.CampType == CampType.Neutral || camp2.CampType == CampType.Neutral)
            return false;

        return camp1.CampId != camp2.CampId;
    }
}
```

**Step 3: Refactor - 优化代码**
- 提取空值检查为私有方法
- 添加注释

**Step 4: 重复步骤1-3完成其他测试用例**

---

**文档版本**: v1.0
**测试用例数**: 4
**预计时间**: 2小时
**状态**: 待实施
