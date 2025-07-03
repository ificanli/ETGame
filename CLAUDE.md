# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于Unity + .NET的开源游戏框架ET9.0（代号"西施"），采用模块化Package架构，专为大型多人在线游戏开发而设计。框架支持客户端服务端双端C#开发、热更新、分布式架构和高性能网络通信。

## 开发环境和工具

### 必需工具版本
- **Unity版本**: Unity 6000.0.25（严格要求此版本）
- **.NET版本**: .NET 9（必须）
- **PowerShell**: 必须安装，ET工具链基于PowerShell

### 常用开发命令

**重要提醒：所有命令行操作都使用PowerShell执行**

#### 编译相关
```powershell
# 编译整个解决方案（需翻墙下载NuGet包）
dotnet build ET.sln

# 发布Linux版本
pwsh -ExecutionPolicy Bypass -File Scripts/Publish.ps1

# Unity内编译（快捷键F6）
# 注意：Model和Hotfix程序集不能用IDE编译，必须用Unity编译
```

#### 热更新相关
```powershell
# Unity菜单操作（无直接命令行）
# ET -> Reload 或按F7进行热重载
# HybridCLR -> Generate -> All 生成热更新相关文件
# ET -> HybridCLR -> CopyAotDlls 复制AOT DLL
```

#### 资源和配置
```powershell
# 导出Excel配置（PowerShell脚本）
# Unity菜单: ET -> Excel -> ExcelExport

# 导出ScriptableObject配置
# Unity菜单: ET -> WOW -> ExportScriptableObject

# YooAsset资源打包
# 通过YooAsset -> AssetBundle Builder窗口操作
```

#### 服务器相关
```powershell
# 独立启动服务器（需管理员权限）
# Unity菜单: ET -> Server Tools -> Start Server(Single Process)

# 手动启动服务器
dotnet Bin/ET.App.dll --Console=1

# 注意：运行目录是Unity根目录，不是Bin目录
```

### 重要开发工具
- **Excel导出工具**: `dotnet Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll`
  - 功能：导出Excel配置为Luban格式
  - 处理内容：Luban配置、游戏数据表、启动配置等
  - 特性：支持实时输出和Ctrl+C正常终止

- **Proto导出工具**: `dotnet Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll`
  - 功能：导出proto文件为C#文件
  - 处理内容：Protocol Buffers消息定义转换为C#类
  - 用途：网络通信协议、数据序列化结构

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


### 开发工作流
```
1. 修改代码 -> 2. dotnet build ET.sln -> 3. 重启进程 -> 4. 测试
```

### 文件结构说明
- `Assets/`: Unity项目资源
- `Packages/`: ET模块化包
- `Bin/`: 编译输出目录
- `Scripts/`: 游戏逻辑代码
- `Book/`: 开发文档和教程
- `Luban/`: Excel配置
- `Proto/`: 消息的proto定义

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

## Claude AI 使用说明

### 命令执行规范
**重要：Claude在此项目中执行的所有命令都必须使用PowerShell**
- 原因：ET框架工具链完全基于PowerShell
- 方式：使用 `pwsh -Command "具体命令"` 格式
- 兼容性：确保与项目构建脚本和工具链的一致性

### 信息记录规则
当用户说"请记住"时，将信息记录在此CLAUDE.md文件中。

**日志输出规范**：打印的日志请使用英文，这是项目的统一要求。

**Singleton类方法规范**：Singleton类（如RobotCaseDispatcher）可以包含方法，不需要创建System类。这类似于其他单例类如HttpDispatcher的设计模式。

## ET框架开发规范

### Entity-Component-System (ECS) 架构
- **Entity**：只包含数据，不包含方法
- **System**：只包含逻辑，不包含数据  
- **Component**：Entity的组成部分，遵循组合优于继承
- **分离原则**：严格分离数据定义和业务逻辑

### 包结构规范
- **所有代码文件**必须创建在 `Packages/cn.etetet.*` 目录下
- **包命名规范**：`cn.etetet.{功能模块名}`
  - 核心包：`cn.etetet.core`
  - 功能包：`cn.etetet.mmo`、`cn.etetet.bag`、`cn.etetet.skill` 等
  - UI包：`cn.etetet.yiui*` 系列

### 程序集分类规范
每个包必须支持以下四个程序集分类：

#### 1. Model 程序集 (`Scripts/Model/`)
- **用途**：服务器和客户端共享的模型层
- **内容**：Entity定义、配置数据、共享逻辑
- **特点**：不可热更新，稳定性高

#### 2. ModelView 程序集 (`Scripts/ModelView/`)  
- **用途**：客户端专用的视图模型层
- **内容**：UI相关Entity、客户端专用组件
- **特点**：不可热更新，UI底层支持

#### 3. Hotfix 程序集 (`Scripts/Hotfix/`)
- **用途**：服务器和客户端共享的热更新逻辑层
- **内容**：System类、业务逻辑实现
- **特点**：可热更新，核心业务逻辑

#### 4. HotfixView 程序集 (`Scripts/HotfixView/`)
- **用途**：客户端专用的热更新视图层
- **内容**：UI System类、客户端显示逻辑
- **特点**：可热更新，UI业务逻辑

### Entity 开发规范

#### Entity 类定义规范
```csharp
// 位置：Packages/cn.etetet.{包名}/Scripts/Model/ 或 Scripts/ModelView/
namespace ET  // 或 ET.Client, ET.Server
{
    /// <summary>
    /// 详细的中文描述
    /// </summary>
    [ComponentOf(typeof(ParentEntityType))]  // 指定父实体类型（如适用）
    public class ExampleComponent : Entity, IAwake, IDestroy
    {
        // 只包含数据字段，不包含方法
        public int SomeValue;
        public string SomeName;
        public List<int> SomeList;
    }
}
```

#### Entity 类要求
- **必须**继承 `Entity` 基类
- **必须**实现 `IAwake` 接口（生命周期接口）
- **根据需要**实现其他接口：`IDestroy`、`IUpdate`、`ISerialize` 等
- **严禁**在Entity类中定义任何方法
- **必须**添加 `[ComponentOf]` 或 `[ChildOf]` 特性指定父级约束


#### System 类定义规范
```csharp
// 位置：Packages/cn.etetet.{包名}/Scripts/Hotfix/ 或 Scripts/HotfixView/
namespace ET  // 或 ET.Client, ET.Server
{
    /// <summary>
    /// 详细的中文描述
    /// </summary>
    [EntitySystemOf(typeof(ExampleComponent))]     // 指定对应的Entity类型
    public static partial class ExampleComponentSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this ExampleComponent self)
        {
            // 初始化逻辑
        }

        [EntitySystem]
        private static void Destroy(this ExampleComponent self)
        {
            // 销毁清理逻辑
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 业务方法的中文描述
        /// </summary>
        public static void DoSomething(this ExampleComponent self, int param)
        {
            // 业务逻辑实现
        }

        #endregion
    }
}
```

#### System 类要求
- **必须**是静态类（`static`）
- **必须**包含 `partial` 关键字
- **必须**添加 `[EntitySystemOf(typeof(对应Entity类))]` 特性
- **必须**实现对应Entity的 `Awake` 生命周期函数
- **所有方法**必须是静态扩展方法
- **生命周期方法**必须添加 `[EntitySystem]` 特性并声明为 `private static`

### EntityRef使用规范

#### 基本使用方法
```csharp
// Entity字段中使用EntityRef
public Dictionary<int, EntityRef<ProcessInfo>> ProcessDict { get; set; }

// 创建EntityRef引用
EntityRef<ProcessInfo> processRef = processInfo;

// 正确的Entity对象访问和检查方式
ProcessInfo entity = processRef;  // 直接赋值，不用.Entity
if (entity != null)
{
    // 安全使用Entity
}

// 错误方式
// var entity = processRef.Entity;  // 错误：不要用.Entity
// if (processRef.Entity != null) { /* 使用 */ }  // 错误：多次访问
```

#### EntityRef在async/await环境下的使用规范（重要！）
**这是ET分析器的严格限制，必须遵循：**

```csharp
// ✅ 正确：await后使用Entity需要通过EntityRef重新获取
public static async ETTask ProcessUpdate(this UpdateCoordinatorComponent self, UpdateTask task)
{
    // 1. 在await前创建EntityRef引用
    EntityRef<UpdateCoordinatorComponent> selfRef = self;
    EntityRef<UpdateTask> taskRef = task;
    
    foreach (int processId in task.TargetProcessIds)
    {
        // 2. 在每次使用前通过EntityRef重新获取Entity
        task = taskRef;
        task.UpdateProgress(processId, "开始处理");
        
        // 3. await后需要重新获取所有Entity
        await SomeAsyncOperation();
        
        // 4. await后必须重新获取才能安全使用
        self = selfRef;
        task = taskRef;
        
        // 现在可以安全使用Entity
        task.UpdateProgress(processId, "处理完成");
    }
}
```

**关键要点：**
- await后，可能Entity已经失效
- 必须在await前创建EntityRef
- await后必须通过EntityRef重新获取Entity才能使用
- 这是ET分析器的硬性限制，违反会导致编译错误

### 命名空间规范
- `ET`：通用命名空间，用于共享代码
- `ET.Client`：客户端专用命名空间
- `ET.Server`：服务器专用命名空间

### 代码质量规范

#### 注释规范
```csharp
/// <summary>
/// 类的详细中文描述
/// 说明功能、用途和注意事项
/// </summary>
public class ExampleComponent : Entity, IAwake
{
    /// <summary>
    /// 字段的中文描述
    /// </summary>
    public int Value;
}

/// <summary>
/// 方法的详细中文描述
/// </summary>
/// <param name="self">当前组件实例</param>
/// <param name="value">参数的中文描述</param>
/// <returns>返回值的中文描述</returns>
public static bool DoSomething(this ExampleComponent self, int value)
{
    // 重要逻辑的中文注释
    return true;
}
```

#### 编码风格
```csharp
// 命名规范
public class PlayerComponent        // 类名：PascalCase
public static void GetItem()       // 方法名：PascalCase  
public string playerName;          // 字段名：camelCase
private string internalField;     // 私有字段：camelCase
public const int MAX_PLAYERS = 100; // 常量：UPPER_SNAKE_CASE

// 代码格式
if (condition) 
{   // 大括号不能省
    // tab缩进
}
```

## 重要分析器规范

### Entity Await 安全规范
- 在 async/await 环境下，任何 Entity 及其子类对象，await 之后**禁止直接访问** await 前的 Entity 变量。
- 必须在 await 前创建 EntityRef，await 后通过 EntityRef 重新获取 Entity。
- 只要存在执行路径可能在 await 后访问 Entity，均视为违规。
- 支持 `[SkipAwaitEntityCheck]` 特性标记的方法或类可跳过此检查，但是应该避免使用`[SkipAwaitEntityCheck]`。

### Entity 成员引用规范
- 任何类/结构体**禁止直接声明 Entity 或其子类类型的字段或属性**，包括集合类型（如 List<Entity>、Dictionary<int, Entity>）。
- 允许声明 `EntityRef<T>` 类型字段或属性。

## 常见错误避免

1. ❌ Entity中定义方法
2. ❌ System忘记加特性
3. ❌ 生命周期方法不是private static
4. ❌ 忘记实现IAwake接口
5. ❌ 消息类字段使用camelCase（应该用PascalCase）
6. ❌ Entity字段不完整（缺少System中使用的字段）
7. ❌ 重复定义相同的类（检查是否已存在）
8. ❌ HTTP消息类字段名与使用处不匹配
9. ❌ 直接存储Entity引用（应该用EntityRef）
10. ❌ 使用EntityRef.Entity属性访问（应该直接赋值）
11. ❌ 不检查Entity的IsDisposed状态
12. ❌ 将EntityRef当作Entity直接使用
13. ❌ await后直接使用Entity（违反ET分析器规则）
14. ❌ [StaticField], 静态字段容易导致多线程问题，应该尽量避免使用，如果要使用，必须需要我手动确认

## 总结

严格遵循以上规范，确保：
1. **架构清晰**：Entity负责数据，System负责逻辑
2. **模块化**：功能按包组织，程序集合理分离
3. **可维护**：代码规范统一，注释详细
4. **可扩展**：遵循框架设计原则，易于扩展
5. **高质量**：充分的错误处理和性能优化
6. **代码一致性**：字段命名统一、Entity完整性、无重复定义
7. **EntityRef安全**：正确管理Entity引用，遵循async/await规范

这些规范是ET框架高效开发的基础，请严格遵循执行。


#### 机器人测试流程
1. 启动测试进程: dotnet ./Bin/ET.App.dll --Process=1 --SceneName=WOW --StartConfig=Localhost --Console=1
2. 进程会输出>等待你的输入
3. 测试所有用例: 在测试进程控制台输入Case --Id=0 //--Id=0指执行所有用例
   输出case run success: 0，表示所有测试用例执行完成
4. 测试指定用例X: 在测试进程控制台输入Case --Id=X   X是RobotCaseType的成员变量
   输出"case run success: X" 表示测试用例X执行完成

#### 完整的命令行执行方式

**单个测试用例执行：**
```bash
printf "Case --Id=0\n" | pwsh -Command "dotnet ./Bin/ET.App.dll --Process=1 --SceneName=RobotCase --StartConfig=Localhost --Console=1"
```

**单进程多用例连续执行（推荐）：**
```bash
printf "Case --Id=1\nCase --Id=2\n" | pwsh -Command "dotnet ./Bin/ET.App.dll --Process=1 --SceneName=RobotCase --StartConfig=Localhost --Console=1"
```

**执行流程说明：**
- 启动测试进程，显示 `>` 等待输入
- 依次执行多个测试用例，无需重启进程
- 成功输出：`case run success: X` (X为用例ID)
- 失败输出：`case run failed: X` 加详细错误信息
- 检测到EOF时程序正常退出

**优势：**
- 一次启动多次测试，提高效率
- 保持进程状态，减少启动开销
- 支持连续测试不同用例
- 实时查看每个用例的执行结果