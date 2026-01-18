# cn.etetet.test

## 概述

通用测试用例包，基于 `ConsoleMode.Test` 运行，按 `[Test]` 特性自动发现并分发到对应处理器执行。支持按包名与用例名的正则过滤批量执行。

## 详细文档

- **编写测试用例**：请查看 `/et-test-write` skill
- **执行测试**：请查看 `/et-test-run` skill

## 核心类

| 文件 | 说明 |
|------|------|
| `TestConsoleHandler.cs` | 控制台处理器，解析命令并进入测试模式 |
| `TestDispatcher.cs` | 按包名与处理器名正则筛选匹配的测试用例 |
| `ATestHandler.cs` | 测试基类，约定 `Handle(...)` 接口 |
| `FiberInit_TestCase.cs` | `TestCase` 场景初始化，每个用例均为全新服务器环境 |
| `TestArgs.cs` | 命令行参数定义 |
| `ITestHandler.cs` | 测试处理器接口 |
