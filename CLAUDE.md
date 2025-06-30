# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于Unity + .NET的开源游戏框架ET9.0（代号"西施"），采用模块化Package架构，专为大型多人在线游戏开发而设计。框架支持客户端服务端双端C#开发、热更新、分布式架构和高性能网络通信。

## 开发环境和工具

### 必需工具版本
- **Unity版本**: Unity 6000.0.25（严格要求此版本）
- **IDE**: JetBrains Rider 2024.3（推荐）或Visual Studio（不推荐新手使用）
- **.NET版本**: .NET 8（必须）
- **PowerShell**: 必须安装，ET工具链基于PowerShell

### 常用开发命令

#### 编译相关
```bash
# 编译整个解决方案（需翻墙下载NuGet包）
dotnet build ET.sln

# 发布Linux版本
powershell -ExecutionPolicy Bypass -File Scripts/Publish.ps1

# Unity内编译（快捷键F6）
# 注意：Model和Hotfix程序集不能用IDE编译，必须用Unity编译
```

#### 热更新相关
```bash
# Unity菜单操作（无直接命令行）
# ET -> Reload 或按F7进行热重载
# HybridCLR -> Generate -> All 生成热更新相关文件
# ET -> HybridCLR -> CopyAotDlls 复制AOT DLL
```

#### 资源和配置
```bash
# 导出Excel配置（PowerShell脚本）
# Unity菜单: ET -> Excel -> ExcelExport

# 导出ScriptableObject配置
# Unity菜单: ET -> WOW -> ExportScriptableObject

# YooAsset资源打包
# 通过YooAsset -> AssetBundle Builder窗口操作
```

#### 服务器相关
```bash
# 独立启动服务器（需管理员权限）
# Unity菜单: ET -> Server Tools -> Start Server(Single Process)

# 手动启动服务器
dotnet Bin/ET.App.dll --Console=1

# 注意：运行目录是Unity根目录，不是Bin目录
```

## 核心架构设计

### 程序集分层结构
```
ET.HotfixView (UI热更新层)
    ↓
ET.Hotfix (逻辑热更新层)
    ↓
ET.ModelView (UI模型层)
    ↓
ET.Model (游戏逻辑模型层)
    ↓
ET.Loader (加载器和启动层)
    ↓
ET.Core (框架核心层)
```

### ECS架构核心
- **Entity**: 所有游戏对象的基类，支持对象池优化
- **Component**: 数据组件，纯数据无逻辑
- **System**: 逻辑系统，通过静态扩展方法实现热更新
- **EventSystem**: 事件发布订阅系统，支持异步事件处理

### 模块化Package设计
项目采用Unity Package Manager管理模块：
- `cn.etetet.core`: 框架核心功能
- `cn.etetet.loader`: 代码加载和启动
- `cn.etetet.hybridclr`: HybridCLR热更新集成
- `cn.etetet.yiuiframework`: YIUI界面框架
- `cn.etetet.yooassets`: YooAsset资源管理集成

### 热更新机制
- **代码热更**: 基于HybridCLR，支持C#代码运行时更新
- **资源热更**: 基于YooAsset，支持资源增量更新
- **配置热更**: 基于Luban配置系统，支持配置实时更新

### 网络架构特色
- **Fiber并发模型**: 轻量级协程，充分利用多核
- **KCP协议**: 高性能UDP可靠传输，支持弱网环境
- **Actor模型**: 分布式实体通信，位置透明
- **软路由**: 防DDoS攻击，动态切换网络节点

## 关键注意事项

### 编译规则
1. **Model和Hotfix程序集只能在Unity中编译**（F6），不能用IDE编译
2. 编译前需要翻墙，确保NuGet包能正常下载
3. 修改代码后用F7进行热重载，无需重启

### 开发工作流
```
1. 修改代码 -> 2. Unity中F6编译 -> 3. F7热重载 -> 4. 测试
```

### 常见问题解决
- **10037错误**: 检查服务器是否以管理员权限启动
- **MemoryPack报错**: 确保翻墙并更新NuGet包
- **编译失败**: 确保使用指定的Unity版本6000.0.25
- **Token问题**: 配置GitHub Token访问ET-Packages

### 文件结构说明
- `Assets/`: Unity项目资源
- `Packages/`: ET模块化包
- `Bin/`: 编译输出目录
- `Scripts/`: 构建和发布脚本
- `Book/`: 开发文档和教程

### 调试技巧
- 开启`ENABLE_VIEW`宏可在Unity Hierarchy中查看所有Entity
- 使用Unity Profiler监控性能
- 查看`Logs/`目录获取详细日志信息
- 服务端支持REPL模式进行动态调试

## 特别提醒

1. **必须翻墙**: 整个开发过程需要全局翻墙访问GitHub Package和NuGet
2. **管理员权限**: 运行服务器需要管理员权限（HTTP服务需要）
3. **版本严格**: 新手必须使用指定的Unity 6000.0.25版本
4. **Clone新工程**: 必须clone全新工程，不要使用已有工程
5. **PowerShell必需**: ET工具链完全基于PowerShell，必须安装