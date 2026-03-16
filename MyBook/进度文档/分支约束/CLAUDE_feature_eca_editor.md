# 并行开发约束 - 工程3：ECA 编辑器和地图设计

## 基础规则
1. 请读取 ./AGENTS.md
2. 所有命令必须使用 PowerShell
3. 编译命令: `dotnet build ET.sln`
4. 绝对禁止 hard code

## 当前工程任务
- **工程编号**: 3（ECA 编辑器和地图设计）
- **分支**: MySpawn（与其他3个工程共用分支，各自独立开发，最后手动合并代码）
- **负责模块**: GraphView 可视化编辑器 + 地图场景设计 + 新 ECA 节点类型

## 可修改的包
- cn.etetet.eca — Editor 目录：GraphView 编辑器
- cn.etetet.ecanode — 新增 ECA 节点类型（容器、刷怪点）
- cn.etetet.map — 场景资源（Bundles/Scenes）、ECA 配置文件

## 禁止修改的文件和包
- cn.etetet.equipment / cn.etetet.item（装备和物品）
- cn.etetet.spell 的核心逻辑
- cn.etetet.match / cn.etetet.battle（匹配和战局）
- cn.etetet.statesync 中的 InputSystem / OperaComponent
- cn.etetet.ecanode 中 PlayerEvacuationComponentSystem 的 case 1 撤离逻辑（工程 4 负责）
- MainPackage.txt 和 g.props

## 核心任务清单
1. ECA GraphView 编辑器（节点创建、连接、拖拽、保存/加载）
2. 新增 ECA 节点类型：
   - 搜索容器（Container）— PointType=3
   - 刷怪点（SpawnPoint）— PointType=2
3. 容器交互逻辑（走近 → 开箱 → 拾取物品）
4. 地图场景设计（布置撤离点、容器、怪物刷新点）
5. 导出完整的 ECA 配置

## Proto 约束
- 不新增 proto 文件（Editor 工具，无网络消息）

## ECAPointComponentSystem.cs 冲突协调
- 只添加新 case（case 2 SpawnPoint, case 3 Container）
- 不修改 case 1 (EvacuationPoint) 的逻辑
