# AGENTS.md

读完这个文件，请输出确认是否做到了以下步骤:
1.是否了解Skills目录，是否读取  
2.是否了解Skills的使用场景，并且在相应的场景加载对应的Skills  

## 项目概述

基于Unity + .NET的开源游戏框架ET10（代号"昭君"），采用模块化Package架构，专为大型多人在线游戏开发而设计。框架支持客户端服务端双端C#开发、热更新、分布式架构和高性能网络通信。

### 核心特性

- **统一开发语言**：客户端、服务端均使用C#
- **热更新支持**：代码、资源、配置三位一体热更新
- **模块化架构**：Package管理，松耦合设计
- **ECS框架**：Entity-Component-System架构
- **分布式支持**：多进程、多服务器部署

### 重要提醒

1. **管理员权限**：运行服务器需要管理员权限
2. **PowerShell必需**：ET工具链完全基于PowerShell

### 文件结构

```
WOW/
├── Assets/           # Unity项目资源
├── Packages/         # ET模块化包
├── Bin/             # 编译输出目录
├── Scripts/         # 游戏逻辑代码
├── Book/            # 开发文档和教程
├── Luban/           # Excel配置
├── Proto/           # 消息的proto定义
└── Logs/            # 运行日志
```

## 重要: Skill使用指南

1. 重要: 请阅读一下./Agents/skills目录，所有skills都放在这里
2. 重要: 在遇到下面描写的使用场景的时候，请自动加载对应的skills


### et-excel - Excel 操作专家

**使用场景**：
- 读取/写入 Excel 文件（xlsx）
- 单元格操作、批量数据导入导出
- 设置样式、公式、图表
- 工作表管理、合并单元格
- Luban 配置表操作

### et-arch - 架构和规范守护者

**使用场景**：
- 创建新的Entity或Component
- 编写System类
- 检查代码是否符合ECS规范
- EntityRef使用问题
- await后Entity访问问题
- Module分析器相关问题

### et-build - 编译构建专家

**使用场景**：
- 编译项目（dotnet build ET.sln）
- 导出Excel配置
- 导出Proto文件
- 启动服务器
- 发布版本
- 资源打包

### et-test-run - 测试执行专家（高频）

**使用场景**：
- 执行测试（全部/指定）
- 查看测试日志
- 调试测试失败

### et-test-write - 测试编写专家（低频）

**使用场景**：
- 编写或修改测试用例

### et-tdd - 测试驱动规范

**使用场景**：
- 使用tdd测试驱动编写代码


## 包的依赖规范

### 依赖管理原则

1. 依赖的配置在包的package.json中
2. 包之间不能相互依赖，只能单向依赖
3. 包中只能访问自己包或者依赖包的符号
4. 请注意要递归依赖，修改依赖时要把依赖的依赖全部递归加上去
5. 通常只能高层包依赖低层包，但如果A包的packagegit.json中加了"AllowSameLevelAccess": true，那么允许没有被A包依赖的同层包访问
6. 假如A包依赖了B包，那么B包永远不能访问A包，这样可以强制处理逻辑相互依赖问题

### 包层级关系

#### 第5层

```
├── cn.etetet.wow           (游戏入口)          AllowSameLevelAccess
├── cn.etetet.btnode        (btnode)           AllowSameLevelAccess
├── cn.etetet.test          (测试系统)          AllowSameLevelAccess
├── cn.etetet.robot         (机器人系统)        AllowSameLevelAccess 依赖console
├── cn.etetet.login         (登录系统)          AllowSameLevelAccess
├── cn.etetet.map           (地图系统)          AllowSameLevelAccess 依赖actorlocation
```

#### 第4层

```
├── cn.etetet.actorlocation (location消息系统) 依赖netinner
├── cn.etetet.aoi           (数值系统) 依赖unit，numeric
```

#### 第3层

```
├── cn.etetet.numeric       (数值系统) 依赖unit
├── cn.etetet.move          (移动系统) 依赖unit
├── cn.etetet.nav2d         (寻路系统) 依赖unit
├── cn.etetet.netinner      (内网消息系统) 依赖startconfig
├── cn.etetet.router        (软路由系统) 依赖startconfig,http
├── cn.etetet.item          (道具背包系统) 依赖unit
```

#### 第2层

```
├── cn.etetet.unit          (单位系统)
├── cn.etetet.behaviortree  (行为树系统)
├── cn.etetet.http          (http系统)
├── cn.etetet.startconfig   (服务器配置系统)
├── cn.etetet.console       (控制台系统)
├── cn.etetet.yooassets     (资源加载系统)
├── cn.etetet.yiuiframework (yiuiframework)
├── cn.etetet.yiuiinvoke    (yiuiinvoke)
├── cn.etetet.yiuigm        (yiuigm)
├── cn.etetet.yiuiloopscrollrectasync (yiuiloopscrollrectasync)
├── cn.etetet.yiuiyooassets (yiuiyooassets)
├── cn.etetet.yiui          (yiui)
├── cn.etetet.yiuireddot    (yiuireddot)
├── cn.etetet.yiuitips      (yiuitips)
├── cn.etetet.yiuidamagetips(yiuidamagetips)
├── cn.etetet.yiui3ddisplay (yiui3ddisplay)
├── cn.etetet.yiuieffect    (yiuieffect)
```

#### 第1层

```
├── cn.etetet.core          (核心框架)
├── cn.etetet.excel         (excel)
├── cn.etetet.proto         (协议定义)
├── cn.etetet.loader        (加载器)
```

## 包结构规范

### 基本要求

- **所有代码文件**必须创建在 `Packages/cn.etetet.*` 目录下
- **包命名规范**：`cn.etetet.{功能模块名}`
- 每个package中都有packagegit.json文件，每个packagegit.json中的Id是项目唯一的
- 每个package中都有Scripts/Model/Share/PackageType.cs文件，里面的编号就是packagegit.json中的Id
- 每个包都有AGENTS.md文件，如果没有请创建，修改前可以读取理解包的规范，可以参考cn.etetet.test包

## AI 使用规范

### AI行为规范

**重要: 请使用全中文跟我沟通（代码除外）**
**重要: 每次执行任何操作前请先向我说明要做什么，以及为什么要这么做**

### 命令执行规范

**重要：在此项目中执行的所有命令都必须使用PowerShell**

- 原因：ET框架工具链完全基于PowerShell
- 兼容性：确保与项目构建脚本和工具链一致

### 其他规范

- 分析器编译要使用ET.sln
- Singleton类（如RobotCaseDispatcher）可以包含方法，不需要创建System类


## 核心开发原则

### 绝对禁止事项

- **绝对禁止hard code**
- **项目只有一个编译**: `dotnet build ET.sln` 无论什么都用这个编译
- **每次做出决定前先检查是否违反规定，执行完任务后再次检查是否违反规定**

### 质量保证要求

严格遵循以上规范，确保：

1. **架构清晰**：Entity负责数据，System负责逻辑
2. **模块化**：功能按包组织，程序集合理分离
3. **可维护**：代码规范统一，注释详细
4. **可扩展**：遵循框架设计原则，易于扩展
5. **高质量**：充分的错误处理和性能优化
6. **代码一致性**：字段命名统一、Entity完整性、无重复定义
7. **EntityRef安全**：正确管理Entity引用，遵循async/await规范
8. **Handler规范**：`MessageLocationHandler` / `MessageHandler<T>` / `MessageSessionHandler` 的 `Run` 入口无需额外判空实体；如有 `await`，必须在 `await` 后通过 `EntityRef` 重新获取再使用

这些规范是ET框架高效开发的基础，请严格遵循执行。
