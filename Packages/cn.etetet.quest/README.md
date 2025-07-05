# ET.Quest

ET框架任务系统模块，提供完整的任务管理、目标追踪和进度监控功能。

## 功能特性

- **完整的任务流程**：支持任务接取、进行、提交、放弃等完整流程
- **目标管理**：支持多目标任务，实时进度追踪
- **前后端同步**：客户端服务端数据实时同步
- **事件驱动**：基于ET事件系统的任务状态变化通知
- **模块化设计**：符合ET框架ECS架构规范
- **可扩展**：支持自定义任务类型和目标处理器

## 核心组件

### 服务端
- `QuestComponent` - 服务端任务管理组件
- `Quest` - 任务实体
- `QuestObjective` - 任务目标实体
- `QuestHelper` - 任务操作帮助类
- `IQuestObjectiveHandler` - 任务目标处理器接口

### 客户端
- `ClientQuestComponent` - 客户端任务组件
- `ClientQuestData` - 客户端任务数据实体
- `ClientQuestObjectiveData` - 客户端任务目标数据实体
- `ClientQuestHelper` - 客户端任务操作帮助类

### 共享
- `QuestStatus` - 任务状态枚举
- `QuestEvents` - 任务相关事件定义

## 使用示例

```csharp
// 接取任务
bool success = await ClientQuestHelper.AcceptQuest(scene, questId, npcId);

// 提交任务
bool success = await ClientQuestHelper.SubmitQuest(scene, questId, npcId);

// 获取任务进度
ClientQuestData questData = questComponent.GetQuestData(questId);
```

## 依赖

- cn.etetet.core
- cn.etetet.sourcegenerator

## 版本历史

### 1.0.0
- 初始版本
- 支持基础任务流程
- 前后端数据同步
- 事件系统集成