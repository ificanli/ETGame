# cn.etetet.core

## 概述

核心框架包，承载通用基础设施，包括命令行参数、日志、Fiber 调度、Timer、ETTask、网络基础能力等。

## 修改注意

- 命令行参数统一收敛在 `Scripts/Core/Share/World/Options/Options.cs`
- Fiber、Timer、ETTask 相关改动优先保持通用性，不写业务特例
- 涉及全局启动流程时，先确认对非 Test 场景没有副作用

## 关键文件

| 文件 | 说明 |
|------|------|
| `Scripts/Core/Share/World/Options/Options.cs` | 全局启动参数定义 |
| `Scripts/Core/Share/World/Fiber/Fiber.cs` | Fiber 调度与帧结束等待 |
| `Scripts/Core/Share/Timer/TimerComponentSystem.cs` | Timer/WaitAsync 能力 |
| `Scripts/Core/Share/World/Log/Log.cs` | Console/文件日志输出 |
