# cn.etetet.test

## 概述
- 通用测试用例包，基于 `ConsoleMode.Test` 运行，按 `[Test]` 特性自动发现并分发到对应处理器执行。
- 支持按包名与用例名的正则过滤批量执行，适合快速验证模块逻辑与环境初始化流程。

## 快速运行
- 启动测试场景：
  - `pwsh -Command "dotnet ./Bin/ET.App.dll --Console=1 --SceneName=Test"`
- 交互式执行示例：
  - `Test`
  - `Test --Name=CreateRobot`
  - `Test --Name=CreateRobot2`（无匹配时提示 not found test）
- 注意测试的进程不会退出，ai在跑测试的时候，不要一直等待进程退出

## 命令参数
- `--Name`：处理器类名正则，默认 `.*`，匹配所有测试用例。
- 解析与分发：控制台命令由 `TestConsoleHandler` 解析，按正则从 `TestDispatcher` 获取匹配的处理器并逐一运行。

## 核心类
- `Scripts/Hotfix/Test/TestConsoleHandler.cs:12`：声明控制台处理器并进入测试模式。
- `Scripts/Hotfix/Test/TestConsoleHandler.cs:25`：按 `Package/Name` 正则获取用例并执行，`ret == 0` 视为成功。
- `Scripts/Model/Test/TestDispatcher.cs:48`：按包名与处理器名正则筛选并返回匹配的 `ITestHandler` 列表。
- `Scripts/Model/Test/TestArgs.cs:7`：命令行参数定义，`Package/Name` 默认 `.*`。
- `Scripts/Model/Test/ITestHandler.cs:5`：测试处理器接口约定。
- `Scripts/Hotfix/Test/ATestHandler.cs:11`：统一处理流程，自动创建/移除 `TestCase` 子纤程并调用 `Run(...)`。
- `Scripts/Hotfix/Test/FiberInit_TestCase.cs:8`：`TestCase` 场景初始化，每个用例均为全新服务器环境。

## 测试用例命名规范（强制）
继承 `ATestHandler` 的测试用例类必须遵守以下命名规范，由分析器 `TestCaseNamingAnalyzer`（ET0036）在编译时强制检查：

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
- `Test case class 'InvalidTest' must follow naming pattern '{PackageType}_{TestName}_Test' and be placed in 'Scripts/Hotfix/Test' directory: Class name must end with '_Test'`
- `Test case class 'Robot_Example_Test' must follow naming pattern '{PackageType}_{TestName}_Test' and be placed in 'Scripts/Hotfix/Test' directory: PackageType should be 'Test' for package 'cn.etetet.test', but got 'Robot'`

## 编写新测试
1. 新建类继承 `ATestHandler`（注意：父类已有 `[Test]` 特性，子类无需重复添加）。
2. **严格遵守上述命名规范**，类名格式为 `Test_{描述}_Test`。
3. 文件必须放在 `Scripts/Hotfix/Test/` 目录下。
4. 实现 `protected override ETTask<int> Run(Fiber fiber, TestArgs args)` 并返回约定值（`0` 成功，非 `0` 失败）。
5. 示例：
```csharp
using ET.Test;
using ET.Server;

namespace ET.Test
{
    public class Test_MyCase_Test : ATestHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, TestArgs args)
        {
            await ETTask.CompletedTask;
            return ErrorCode.ERR_Success;
        }
    }
}
```

## 返回值与日志
- 成功：返回 `0`（如 `ErrorCode.ERR_Success`），控制台输出 `success`。
- 失败：返回非零或抛出异常，控制台输出 `fail` 与错误信息。

## 示例输出
- `dotnet ./Bin/ET.App.dll --Console=1 --SceneName=Test`
- `> Test` → `Test.Test_CreateRobot start` → `Test.Test_CreateRobot success`
- `> Test --Name=CreateRobot` → `Test.Test_CreateRobot start` → `Test.Test_CreateRobot success`
- `> Test --Name=CreateRobot2` → `not found test! package: .* name: CreateRobot2`

## 常见问题
- `not found test`：无匹配用例。检查类名与正则是否匹配，是否继承了 `ATestHandler`，热更/编译是否完成。
- 正则匹配范围过大或过小：调整 `--Name` 参数，建议先用默认值确认整体列表再收敛过滤。