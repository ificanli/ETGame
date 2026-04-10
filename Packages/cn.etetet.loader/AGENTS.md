# cn.etetet.loader

## 概述

`cn.etetet.loader` 是 ET 工程的加载与启动相关基础包，负责编辑器构建入口、程序集切换辅助、运行时加载器与部分启动期工具逻辑。

## 修改约束

- 优先在 `Editor/` 下承载构建、批处理、编辑器辅助逻辑
- 运行时逻辑与编辑器逻辑分层，避免把 `UnityEditor` 相关代码混入运行时代码
- 涉及 `CodeMode` 的改动，必须同时考虑资产值与 `ET.CodeMode.dll` 同步
- 涉及构建入口的改动，要优先复用现有 `BuildHelper`、`ProcessHelper`、`AssemblyTool` 等能力，避免重复造轮子
- 任何路径、包名、场景名的选择都要先核对当前工程实际，不允许保留失效的旧路径

## 相关文件

| 文件 | 说明 |
|------|------|
| `Editor/BuildHelper.cs` | 客户端构建辅助方法 |
| `Editor/BuildEditor.cs` | 编辑器内构建窗口 |
| `Editor/AssemblyTool.cs` | 程序集与代码模式辅助 |
| `Editor/ServerCommandLineEditor.cs` | 命令行启动辅助 |

