# 并行开发约束 - 工程4：起装和撤离

## 基础规则
1. 请读取 ./AGENTS.md
2. 所有命令必须使用 PowerShell
3. 编译命令: `dotnet build ET.sln`
4. 绝对禁止 hard code

## 当前工程任务
- **工程编号**: 4（起装和撤离）
- **分支**: MySpawn（与其他3个工程共用分支，各自独立开发，最后手动合并代码）
- **负责模块**: 局外装备选择 → 携带进局 → 撤离带出 → 结算写回

## 可修改的包
- cn.etetet.equipment — 局外装备选择、进局携带
- cn.etetet.item — 局内背包、拾取物品、带出物品
- cn.etetet.ecanode — 撤离完成后的物品结算逻辑（仅 case 1）
- cn.etetet.proto — 仅限新增 LoadoutMessage.proto 文件
- 可能修改 cn.etetet.login — 角色选择流程加装备选择

## 禁止修改的文件和包
- cn.etetet.eca 的 Editor 代码（GraphView 编辑器）
- cn.etetet.spell 的核心逻辑
- cn.etetet.match / cn.etetet.battle（匹配和战局）
- cn.etetet.statesync 中的 InputSystem / OperaComponent
- cn.etetet.ecanode 中 ECAPointComponentSystem 的 case 2/3（工程 3 负责）
- MainPackage.txt 和 g.props

## 核心任务清单
1. 装备选择界面（枪械、英雄选择）
2. 装备数据随玩家传入战局
3. 局内背包系统（拾取搜索到的物品）
4. 撤离时计算带出财富和物品
5. 结算写回（物品存档、财富增加）

## Proto 约束
- 使用独立文件 LoadoutMessage.proto
- 不修改现有 proto 文件

## ECAPointComponentSystem.cs 冲突协调
- 只修改 case 1 (EvacuationPoint) 的奖励逻辑
- 不添加新 case（case 2/3 由工程 3 负责）
