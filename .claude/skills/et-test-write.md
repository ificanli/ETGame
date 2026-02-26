# et-test-write - 测试编写专家

这个skill专门负责编写新的测试用例。

## 使用场景

- 编写新的测试用例
- 测试数据准备
- 配置准备

## 测试用例命名规范（强制）

继承 `ATestHandler` 的测试用例类必须遵守以下命名规范，由分析器 `TestCaseNamingAnalyzer`（ET0036）在编译时强制检查。

### 规则

1. **命名格式**：`{PackageType}_{TestName}_Test`
   - `PackageType`：必须与所在包的 `PackageType` 常量匹配（如 `Test`、`Robot`、`Map` 等）
   - `TestName`：测试用例的描述性名称，可包含多个下划线分隔
   - 必须以 `_Test` 结尾

2. **文件位置**：必须放在包的 `Scripts/Hotfix/Test/` 目录下

3. **PackageType 匹配规则**：
   - `cn.etetet.test` → `Test`
   - `cn.etetet.robot` → `Robot`
   - `cn.etetet.map` → `Map`
   - YIUI相关包（如 `cn.etetet.yiuiframework`）→ `YIUIFramework`
   - 其他包：首字母大写的包名最后部分

### 示例

```csharp
// ✅ 正确：在 cn.etetet.test 包中
public class Test_CreateRobot_Test : ATestHandler { }
public class Test_Login_Success_Test : ATestHandler { }
public class Test_Data_Validation_Test : ATestHandler { }

// ❌ 错误：缺少 _Test 后缀
public class Test_CreateRobot : ATestHandler { }

// ❌ 错误：PackageType 不匹配（在 test 包中使用了 Robot 前缀）
public class Robot_CreateRobot_Test : ATestHandler { }

// ❌ 错误：文件不在 Scripts/Hotfix/Test/ 目录
```

### 编译错误示例

违反命名规范时，编译器会报错：
- `Test case class 'InvalidTest' must follow naming pattern '{PackageType}_{TestName}_Test'...`
- `PackageType should be 'Test' for package 'cn.etetet.test', but got 'Robot'`

## 核心类说明

- `TestConsoleHandler.cs`：控制台处理器，解析命令并进入测试模式
- `TestDispatcher.cs`：按包名与处理器名正则筛选并返回匹配的 `ITestHandler` 列表
- `TestArgs.cs`：命令行参数定义，`Package/Name` 默认 `.*`
- `ITestHandler.cs`：测试处理器接口约定
- `ATestHandler.cs`：统一处理流程，自动创建/移除 `TestCase` 子纤程并调用 `Run(...)`
- `FiberInit_TestCase.cs`：`TestCase` 场景初始化，每个用例均为全新服务器环境

## 编写新测试步骤

1. 新建类继承 `ATestHandler`（父类已有 `[Test]` 特性，子类无需重复添加）
2. **严格遵守命名规范**，类名格式为 `{PackageType}_{描述}_Test`
3. 文件必须放在 `Scripts/Hotfix/Test/` 目录下
4. 实现 `protected override ETTask<int> Run(Fiber fiber, TestArgs args)` 并返回约定值

### 返回值约定

- 成功：`return ErrorCode.ERR_Success;`（即 `0`）
- 失败：**直接返回数字**（如 `return 1;`, `return 2;` 等），不要定义ErrorCode常量

**重要：测试用例的错误码不要污染正式代码**
- ❌ 错误：在包的ErrorCode.cs中定义测试专用的错误码
- ✅ 正确：测试用例直接返回数字，能看懂即可

```csharp
// ✅ 正确：直接返回数字
if (unit == null)
{
    Log.Error($"unit is null");
    return 1;
}

if (aoiEntity == null)
{
    Log.Error($"aoiEntity is null");
    return 2;
}

// ❌ 错误：定义测试专用的ErrorCode常量
public const int ERR_TestError = ...;  // 不要这样做
```

## 测试数据准备

### 访问服务端数据

```csharp
// 获取地图Fiber
string mapName = robot.Root.CurrentScene().Name;
Fiber map = fiber.GetFiber("MapManager").GetFiber(mapName);

// 获取服务端Unit
Client.PlayerComponent playerComponent = robot.Root.GetComponent<Client.PlayerComponent>();
Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);
```

### 准备配置数据

**不要修改已有的json或excel**，在代码中写json串：

```csharp
string json = 
"""
{
    ""Id"": 1,
    ""Name"": ""TestQuest"",
    ...
}
""";
QuestConfigCategory config = MongoHelper.FromJson<QuestConfigCategory>(json);
```

## 日志输出规范

1. **使用英文**：这是项目统一要求
2. **Log.Debug**：打印普通日志
3. **Log.Console**：**测试错误必须使用Log.Console**，方便查看错误信息
4. **不要用Log.Error**：测试错误不要用Log.Error
5. **不要用Log.Info**：用于重要运营日志

**重要：测试错误必须使用Log.Console**
```csharp
// ✅ 正确：使用Log.Console
if (unit == null)
{
    Log.Console($"unit is null");
    return 1;
}

// ❌ 错误：不要使用Log.Error
if (unit == null)
{
    Log.Error($"unit is null");  // 不要这样做
    return 1;
}
```

## 结果验证

**必须检查数据是否与预期一致**：

**重要：每个测试用例中的错误码必须唯一，不能重复**

```csharp
// ✅ 正确：每个错误返回不同的数字
if (serverUnit == null)
{
    Log.Console($"not found server unit");
    return 1;
}

if (aoiEntity == null)
{
    Log.Console($"aoiEntity is null");
    return 2;  // 不同的错误码
}

if (!condition1)
{
    Log.Console($"condition1 failed");
    return 3;  // 不同的错误码
}

// ❌ 错误：错误码重复
if (serverUnit == null)
{
    Log.Console($"not found server unit");
    return 1;
}

if (aoiEntity == null)
{
    Log.Console($"aoiEntity is null");
    return 1;  // 错误：重复了，无法区分是哪里出错
}
```
}

// ✅ 正确：验证状态
if (quest.State != QuestState.Completed)
{
    Log.Error($"quest state error, expected: Completed, actual: {quest.State}");
    return 2;
}

// ❌ 错误：不验证就返回成功
return ErrorCode.ERR_Success;
```

## 常见坑点

### 1. await后Entity访问

```csharp
// ❌ 错误：await后直接使用Entity
await SomeOperation();
task.DoSomething(); // Entity可能已失效

// ✅ 正确：通过EntityRef重新获取
EntityRef<UpdateTask> taskRef = task;
await SomeOperation();
task = taskRef; // 重新获取
task.DoSomething();
```

### 2. 数据清理

**不需要清理**，每个测试用例都是独立环境

### 3. 测试日志

测试成功后，**必须检查All.log**，确认日志符合预期

### 4. 代码复用

**尽量调用已有代码**，不要重复实现逻辑

## 编写完成后

1. 编译项目：使用 `/et-build`
2. 执行测试：使用 `/et-test-run`
3. 检查日志：`cat Logs/All.log`

