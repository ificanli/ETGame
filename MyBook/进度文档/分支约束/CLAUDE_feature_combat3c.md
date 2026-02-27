# 并行开发约束 - 工程2：战斗 3C

## 基础规则
1. 请读取 ./AGENTS.md
2. 所有命令必须使用 PowerShell
3. 编译命令: `dotnet build ET.sln`
4. 绝对禁止 hard code

## 当前工程任务
- **工程编号**: 2（战斗 3C）
- **分支**: MySpawn（与其他3个工程共用分支，各自独立开发，最后手动合并代码）
- **负责模块**: 摇杆移动 + 自动攻击 + 武器切换 + 技能改造

## 可修改的包
- cn.etetet.map — InputSystemComponent、OperaComponent（摇杆输入）
- cn.etetet.spell — 自动攻击、目标选择、技能释放方式
- cn.etetet.statesync — 移动同步改造（摇杆方向 → 服务端移动）
- 可能新建 cn.etetet.weapon（武器系统）

## 禁止修改的文件和包
- cn.etetet.eca / cn.etetet.ecanode（ECA 框架）
- cn.etetet.equipment / cn.etetet.item（装备和物品）
- cn.etetet.match / cn.etetet.battle（匹配和战局）
- cn.etetet.map 中的 FiberInit_Map.cs（除移动相关外）
- MainPackage.txt 和 g.props

## 核心任务清单
1. 虚拟摇杆实现（参考 MyBook/说明文档/9.3摇杆移动改造实施文档.md）
2. 摇杆方向 → 服务端寻路移动
3. 自动索敌 + 范围自动攻击（参考 MyBook/说明文档/9.5范围自动普攻改造方案.md）
4. 武器格子 UI + 点击切换
5. 武器差异化（冲锋枪可移动攻击，步枪需停止）
6. 滑动释放指向技能、双击大招

## Proto 约束
- 不新增 proto 文件（复用现有移动/技能消息）

## 参考文档
- MyBook/说明文档/9.3摇杆移动改造实施文档.md
- MyBook/说明文档/9.5范围自动普攻改造方案.md
