# 并行开发约束 - 工程1：匹配模块

## 基础规则
1. 请读取 ./AGENTS.md
2. 所有命令必须使用 PowerShell
3. 编译命令: `dotnet build ET.sln`
4. 绝对禁止 hard code

## 当前工程任务
- **工程编号**: 1（匹配模块）
- **分支**: MySpawn（与其他3个工程共用分支，各自独立开发，最后手动合并代码）
- **负责模块**: 匹配队列 + 战局系统

## 可修改的包
- cn.etetet.match（新建，匹配队列、模式管理）
- cn.etetet.battle（新建，战局会话、状态机、结算）
- cn.etetet.map — 仅限 FiberInit_Map.cs 中战局初始化相关
- cn.etetet.proto — 仅限新增 MatchMessage.proto 文件

## 禁止修改的文件和包
- cn.etetet.eca / cn.etetet.ecanode（ECA 框架）
- cn.etetet.equipment / cn.etetet.item（装备和物品）
- cn.etetet.spell 的核心逻辑
- cn.etetet.statesync 中的 InputSystem / OperaComponent
- cn.etetet.map 中的 SceneChangeHelper / TransferHelper（除非战局流程必需）
- MainPackage.txt 和 g.props（已在基础设施提交中处理）

## 核心任务清单
1. 匹配队列管理（按模式分队列、超时处理）
2. 匹配成功后创建战局（分配地图、通知玩家）
3. 战局状态机（准备 → 战斗 → 结算）
4. 死亡判定 → 战局结束条件
5. 结算数据收集（击杀数、存活状态、财富）
6. 结算完成返回大厅

## Proto 约束
- 使用独立文件 MatchMessage.proto
- 不修改现有 proto 文件

## 测试要求
- Match_BasicFlow_Test — 基础匹配流程
- Match_Timeout_Test — 匹配超时
- Battle_StateFlow_Test — 战局状态流转
- Battle_Settlement_Test — 结算数据验证
