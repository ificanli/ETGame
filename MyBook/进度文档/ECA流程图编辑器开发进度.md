# ECA 流程图编辑器开发进度

> 说明：此文档用于持续记录开发进度与待办，开发过程中会实时更新。

## 0. 状态概览
- 当前阶段：最小可执行已落地（事件+条件+动作），节点执行已迁移到 ECANode，撤离点已支持流程图优先，待参数细化
- 方案：方案A（流程图嵌入点位配置）
- 客户端/服务端边界：客户端做范围与表现；服务端权威逻辑与倒计时通知

## 1. 已确认结论
- 点位编辑器 = Scene 导出位置/范围
- 流程图编辑器 = 类似 BT 的逻辑图
- 绑定关系：点位直接引用流程图资产（FlowGraphAsset）
- 方案A：导出时流程图嵌入点位配置
- 最小节点集合：以当前需求清单为基准（见开发计划文档）
- 参数细节：架子搭好后再细化

## 2. 已完成
- 新增 FlowGraphData 数据结构（Model/Share）
- 新增 FlowGraphAsset 资产类型（ModelView/Share）
- 扩展 ECAConfig：新增 FlowGraph 字段（方案A嵌入）
- ECAPointMarker 支持 FlowGraph 绑定并在导出时嵌入
- GraphView 编辑器骨架（窗口/GraphView/基础节点/Key字段/Params面板/保存加载）
- Key 选择支持下拉（Event/Condition/Action）
- Key 变更时自动填充 Params 模板
- 扩展 Action Key 常量列表（与参数方案对齐）
- 扩展 Params 模板（事件/条件/动作）
- GraphView 保存时增加基础校验（空Key/无出口/Condition分支缺失）
- 参数方案草案已补充至开发计划文档
- 运行时流程骨架：ECAPoint 接入 FlowGraph + 事件入口 + GraphRunner
- 新增流程图事件常量（ECAFlowEventType）与节点类型常量（ECAFlowNodeType）
- 新增最小可执行条件/动作：
  - Condition: PlayerInRange, PointStateEquals
  - Action: SetPointState
- 新增条件/动作：
  - Condition: PointTypeEquals
  - Action: SetPointActive
- 点位支持启用/禁用开关（IsActive）
- 新增流程图计时器（OnTimerElapsed 支持按 timer_id 匹配）
- 动作骨架扩展：
  - StartSearchTimer / StartEvacCountdown
  - SpawnItemsToGround / SpawnMonsters（记录请求）
  - TransferToLobby / AllowEvacPlayers
- 最小执行逻辑已迁移至 ECANode InvokeHandler
- 撤离点逻辑已改为“FlowGraph 优先，旧逻辑兜底”
- 新增 ECANode 业务节点骨架：Container/SpawnPoint 组件与系统
  - 容器/刷怪点请求记录字段与对接
- 新增测试用例：
  - Ecanode_ContainerFlow_Timer_Test
  - Ecanode_SpawnPoint_Request_Test

## 3. 进行中
- 业务动作与参数细化

## 4. 待办清单
### 4.1 GraphView 编辑器完善
- 节点类型细化（进行中）
- 图校验（已完成：空Key/无出口/Condition分支）

### 4.2 运行时框架完善
- 条件/动作扩展（容器、刷怪、撤离已做骨架，任务待做）
- 客户端表现事件接口

### 4.3 ECANode 业务节点
- Container / SpawnPoint 与流程图动作对接（记录请求）

### 4.4 测试与验证
- Ecanode_ContainerFlow_Timer_Test（已完成）
- Ecanode_SpawnPoint_Request_Test（已完成）

## 5. 风险与注意事项
- 撤离点无 FlowGraph 时走旧逻辑兜底
- 禁止触碰 `equipment/item/spell/匹配与战局/摇杆输入`
- 严格 ECS/EntityRef 规范
- 禁止 hard code，参数走配置

## 6. 编译记录
- `dotnet build ET.sln`：失败（Unity Test Framework 过时 API，路径：`Library/PackageCache/com.unity.test-framework.../SwitchPlatformSetup.cs`）

---
**最后更新**：2026-02-27
