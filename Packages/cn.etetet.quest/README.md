# ET.Quest

> 当前实现状态说明（2026-03-27）。本文描述当前仓库已落地的任务系统能力，不等同于未来完整任务系统设计。

## 当前定位

- **包名**：`cn.etetet.quest`
- **当前判断**：服务端任务骨架和接取/提交链路已落地；客户端同步与 UI 主链路仍未闭环

## 当前数据模型

### 服务端

- `Unit -> QuestComponent -> Quest -> QuestObjective`
- `QuestComponent` 当前维护：
  - `FinishedQuests`
  - `QuestObjectives`（按目标类型聚合）

### 客户端

- `Scene -> QuestComponent -> Quest -> QuestObjective`
- 客户端当前并没有 `ClientQuestComponent` / `ClientQuestData` / `ClientQuestObjectiveData` 这套命名
- 当前客户端实际使用的类名同样是：
  - `QuestComponent`
  - `Quest`
  - `QuestObjective`

## 当前已实现链路

### 服务端

- `C2M_AcceptQuestHandler`
  - 校验 NPC、距离、前置任务和重复接取条件
  - 通过 `QuestHelper.AddQuest` 创建任务实例
- `C2M_SubmitQuestHandler`
  - 校验提交 NPC、距离和任务目标完成状态
  - 通过 `QuestHelper.SubmitQuest` 完成提交流程
- `C2M_SyncQuestDataHandler`
  - 返回当前任务树快照
- `C2M_QueryAvailableQuestsHandler`
  - 已有入口，但返回内容仍不完整
- `QuestHelper.UpdateObjectiveCount`
  - 目标进度变化时发送 `M2C_UpdateQuestObjective`

### 客户端

- `AfterCreateCurrentScene_AddQuestComponent`
  - 场景创建时挂载 `QuestComponent`
- `M2C_SyncQuestDataHandler`
  - 可根据服务端快照重建客户端任务树
- `M2C_UpdateQuestHandler`
  - 可更新任务状态
- `QuestHelper`
  - 已提供 `AcceptQuest`
  - 已提供 `SubmitQuest`
  - 已提供 `SyncQuestData`
  - 已提供 `AbandonQuest`
  - 已提供 `QueryAvailableQuests`
  - 已提供 `GetQuestDetail`

## 当前未闭环部分

- `M2C_UpdateQuestObjectiveHandler` 仍是 TODO，没有真正写入客户端目标进度
- 登录后自动同步任务数据的事件链路仍是注释/TODO 状态
- `C2M_QueryAvailableQuestsHandler` 目前还不能视为完整可接任务查询实现
- NPC 对话、任务面板、可接任务列表等 UI 没有形成完整客户端闭环

## 当前使用示例

```csharp
bool accepted = await QuestHelper.AcceptQuest(scene, questId);
bool submitted = await QuestHelper.SubmitQuest(scene, questId);
AvailableQuestInfo[] available = await QuestHelper.QueryAvailableQuests(scene);
Quest quest = scene.GetComponent<QuestComponent>()?.GetQuest(questId);
```

## 参考文档

- `Book/10-项目架构/任务系统现状.md`
