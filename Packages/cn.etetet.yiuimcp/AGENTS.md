# cn.etetet.yiuimcp

## 概述

`cn.etetet.yiuimcp` 是项目里的 Unity Editor 自动化基础设施包，分为三层：

- `Editor/UnityMCP`：Unity Editor 进程内的 HTTP JSON-RPC 服务
- `UTO`：Node 侧代理与编排层
- `Config/*.ps1`：PowerShell 调用入口

## 修改前检查

- 先判断修改落点属于 Unity 侧、UTO 侧还是脚本侧，不要跨层混改
- 先保证 Unity Editor 侧脚本能正常编译，再验证 UTO 和 `Config/*.ps1`
- `UTO/.port` 是 Unity 与 UTO 的端口同步文件；改动启停链路、端口配置或健康检查时，必须验证它会自动写回
- `UTO/node_modules` 当前导入较重；没有明确需求时，不要顺手调整目录结构

## 开发约束

- Unity 服务入口优先看 `Editor/UnityMCP/Core/YIUIMCPServer.cs`、`YIUIMCPServerHelper.cs`
- UTO 逻辑改 `UTO/src`；如果改了运行时逻辑，需要同步检查 `UTO/build`
- `Config` 目录下脚本统一使用 PowerShell
- 不要用手改构建产物代替源文件修复

## 最小验证顺序

1. `GET /health`
2. `UTO/.port`
3. `Config/get_console_log.ps1` 或 `Config/invoke-uto-tool.ps1`
