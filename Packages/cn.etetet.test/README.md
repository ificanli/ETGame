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

## 命令参数
- `--Package`：包名正则，默认 `.*`，匹配所有包。
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

## 编写新测试
1. 新建类继承 `ATestHandler`，添加特性 `[Test(PackageType.Test)]`。
2. 实现 `protected override ETTask<int> Run(Fiber fiber, TestArgs args)` 并返回约定值（`0` 成功，非 `0` 失败）。
3. 示例：
```csharp
using ET.Test;
using ET.Server;

namespace ET.Test
{
    [Test(PackageType.Test)]
    public class Test_MyCase : ATestHandler
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
- `not found test`：无匹配用例。检查是否添加 `[Test(PackageType.Test)]`，类名与正则是否匹配，热更/编译是否完成。
- 正则匹配范围过大或过小：调整 `--Package` 与 `--Name`，建议先用默认值确认整体列表再收敛过滤。