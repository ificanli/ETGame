# com.etetet.init

## 概述

初始化与 CodeMode 切换包，负责全局启动配置、程序集引用切换工具，以及与构建模式相关的基础资源。

## 修改注意

- `DotNet~/CodeModeChangeHelper.cs` 负责批量维护 `AssemblyReference.asmref`，删除或移动文件时必须同步处理对应 `.meta`
- `Resources/GlobalConfig.asset` 是当前 `CodeMode` 的真实配置来源，修改构建流程时不要依赖历史路径
- `CodeMode` 切换必须同时兼容 `Client`、`Server`、`ClientServer`，避免留下半切换状态

## 关键文件

| 文件 | 说明 |
|------|------|
| `DotNet~/CodeModeChangeHelper.cs` | CodeMode 切换时创建/删除 `AssemblyReference.asmref` |
| `Resources/GlobalConfig.asset` | 当前全局 CodeMode 配置 |
| `Settings/` | 初始化相关配置资源 |
