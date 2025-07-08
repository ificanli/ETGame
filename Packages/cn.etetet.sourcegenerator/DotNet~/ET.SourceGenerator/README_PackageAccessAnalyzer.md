# Package Access Analyzer

## 概述

`PackageAccessAnalyzer` 是一个Roslyn分析器，用于强制执行ET框架中Package之间的访问控制规则。它确保：

1. **成员访问控制**：类的私有和内部成员只能被自己的Package访问
2. **Package依赖管理**：Package之间的访问必须在依赖配置中明确声明
3. **循环依赖检测**：防止Package之间的双向依赖

## 功能特性

### 1. 成员访问控制 (ET0120)
- 检查跨Package的成员访问
- 确保私有和内部成员不被其他Package访问
- 允许公共成员的正常访问

### 2. 循环依赖检测 (ET0121)
- 检测Package之间的双向依赖
- 防止包依赖循环
- 基于配置的依赖关系验证

### 3. 未授权访问检测 (ET0122)
- 检查Package之间的访问是否在依赖配置中声明
- 强制执行明确的依赖关系
- 提供清晰的访问控制

## 配置说明

### Package依赖配置
依赖关系在 `PackageDependencyConfig.cs` 中定义：

```csharp
public static readonly ImmutableDictionary<int, ImmutableHashSet<int>> AllowedDependencies = 
    new Dictionary<int, ImmutableHashSet<int>>
    {
        // Core包 (ID: 1) - 基础包，不依赖其他包
        [1] = ImmutableHashSet<int>.Empty,
        
        // Login包 (ID: 9) - 可以访问Core包
        [9] = ImmutableHashSet.Create(1),
        
        // Quest包 (ID: 10) - 可以访问Core包和Login包
        [10] = ImmutableHashSet.Create(1, 9),
        
        // 更多配置...
    }.ToImmutableDictionary();
```

### Package ID映射
每个Package都有一个唯一的ID，定义在：
- `packagegit.json` 文件中的 `Id` 字段
- `PackageType.cs` 文件中的常量

## 使用示例

### 正确的访问模式
```csharp
// Quest包访问Core包（已在配置中声明）
namespace ET
{
    public class QuestComponent
    {
        public void UseCore()
        {
            var core = new CoreComponent(); // ✅ 允许访问
            var publicValue = core.PublicField; // ✅ 访问公共成员
        }
    }
}
```

### 错误的访问模式
```csharp
// Quest包访问Core包的内部成员
namespace ET
{
    public class QuestComponent
    {
        public void UseCore()
        {
            var core = new CoreComponent();
            var internalValue = core.InternalField; // ❌ ET0120: 不能访问内部成员
        }
    }
}
```

```csharp
// 未授权的Package访问
namespace ET
{
    public class UnauthorizedComponent
    {
        public void UseQuest()
        {
            var quest = new QuestComponent(); // ❌ ET0122: 未授权访问
        }
    }
}
```

```csharp
// 循环依赖
// 如果Core包访问Quest包，而Quest包也访问Core包
namespace ET
{
    public class CoreComponent
    {
        public void UseQuest()
        {
            var quest = new QuestComponent(); // ❌ ET0121: 循环依赖
        }
    }
}
```

## 配置管理

### 添加新的Package依赖
1. 在 `PackageDependencyConfig.cs` 中添加新的依赖关系
2. 确保依赖关系是单向的，避免循环依赖
3. 遵循最小权限原则，只给必要的访问权限

### Package ID管理
1. 每个新Package都需要一个唯一的ID
2. 在 `packagegit.json` 中定义ID
3. 在 `PackageType.cs` 中定义对应的常量
4. 在 `PackageDependencyConfig.cs` 中添加依赖配置

## 最佳实践

1. **明确依赖关系**：只在配置中声明真正需要的依赖
2. **避免循环依赖**：设计时考虑包的层次结构
3. **最小权限原则**：只暴露必要的公共接口
4. **定期审查**：定期检查和清理不必要的依赖关系

## 故障排除

### 常见错误和解决方案

1. **ET0120: Package成员访问违规**
   - 检查是否访问了其他Package的私有或内部成员
   - 将需要访问的成员改为公共的
   - 或者重新设计，避免跨Package访问私有成员

2. **ET0121: Package循环依赖**
   - 检查Package之间的依赖关系
   - 重新设计包结构，消除循环依赖
   - 考虑提取共同依赖到新的基础包

3. **ET0122: Package未授权访问**
   - 在 `PackageDependencyConfig.cs` 中添加必要的依赖关系
   - 确保依赖关系是合理的，不会造成循环依赖

## 技术细节

- 基于Roslyn Analyzer框架
- 支持实时代码分析
- 与Visual Studio和VS Code集成
- 支持CI/CD管道中的代码检查