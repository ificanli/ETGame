# 局内SearchPanel背包与安全箱自适应布局设计文档

**创建时间**：2026-04-16  
**最后更新**：2026-04-16  
**状态**：未回归  
**关联任务**：M0.2-W3 #33  
**涉及包**：cn.etetet.statesync

## 需求概述

当前局内背包界面复用 `SearchPanel`，并通过 `BackpackInspect / ContainerSearch / CorpseLoot` 三种打开模式在同一个 prefab 内切换表现。现状里格子和物品本身会按照运行时格子数刷新，但背包块和安全箱块外层 UI 仍保留旧的绝对定位与拉伸关系，导致：

- 背包背景没有稳定贴合当前格子区域
- 安全箱背景没有稳定贴合当前安全箱格子区域
- 安全箱块没有稳定跟在背包块下方
- 切换不同背包尺寸后，`SearchPanel` 的背包区视觉会失真
- 当前 `BackpackInspect` 虽然会改单栏布局，但主要仍靠运行时代码调整 `BagBoardRoot` 锚点，没有把块级布局收口到更易维护的结构

本期目标：

- `SearchPanel` 三模式都补齐背包/安全箱块自适应布局
- 背包背景贴合格子区域
- 安全箱背景贴合格子区域
- 安全箱自动排在背包下方
- 尺寸跟随运行时背包/安全箱格子数变化
- 本轮不正式做分辨率适配，但结构上要为后续断点适配预留空间

## 技术方案

### 整体思路

延续局外 `LobbyPanel` 的块级布局缓存思路，但结合 `SearchPanel` 的现有 YIUI 绑定和已有 prefab 结构做最小收口：

- 保留 `SearchPanel` 当前格子渲染、拖拽和模式切换逻辑
- 不新建第二套背包业务逻辑，也不复制三模式渲染链路
- 不手改 `SearchPanel.prefab` 的大段 YAML，也不引入第二套 YIUI 绑定
- 在运行时为背包区补一个轻量 `BagRoot` 容器，把 `BagBoardRoot` 从“板子节点”提升成“块内板子”
- 安全箱继续复用现有 `SecureBagRoot`
- 运行时缓存 `Bg / Title / Hint / BoardRoot` 相对关系，并按最新格子尺寸刷新
- `YIUIOpen` 时先恢复上次运行时改动，再按当前打开模式重新建立布局基线，避免 `BackpackInspect / ContainerSearch / CorpseLoot` 互相污染

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/SearchPanelComponent.cs` | 修改 | 新增局内背包区布局缓存字段 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/SearchPanelComponentSystem.cs` | 修改 | 接入背包/安全箱块自适应布局刷新 |
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增 W3 任务33 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 记录版本变更 |
| `Book/04-起装与装备/局内SearchPanel背包与安全箱自适应布局开发日志.md` | 新增 | 记录开发过程 |

### 布局设计

#### 1. 三模式统一复用同一套背包/安全箱块布局能力

`SearchPanel` 的三模式差异主要在：

- 左侧容器区是否显示
- 顶部标题和快捷操作区是否显示
- 背包区处于双栏还是单栏布局上下文

但右侧“背包块 + 安全箱块”本质上是同一类内容，因此：

- 三种模式共用同一套块内 `Bg / Title / Hint / BoardRoot` 缓存与尺寸刷新逻辑
- `BackpackInspect` 采用单栏纵向堆叠：安全箱跟随背包块高度自动下移
- `ContainerSearch / CorpseLoot` 采用双栏基线布局：安全箱保留 `SecureBagRoot` 的 prefab 原始所属区域，只做块内尺寸自适应
- 模式层负责决定容器区显隐，以及选择“单栏堆叠”还是“双栏基线”这套区块定位策略

#### 2. 运行时补背包块容器，安全箱沿用现有 root

当前 `SearchPanel.prefab` 中：

- `BagBoardRoot` 是背包板子，不是完整背包块 root
- `SecureBagRoot` 已经是完整安全箱块 root

实际实现没有继续硬改 prefab，而是在运行时补一层轻量容器：

- 每次 `SearchPanel` 打开时，先把上次创建的 `BagRoot` 还原并销毁
- 按当前模式的初始位置重新创建运行时 `BagRoot`
- 把 `BagBoardRoot` 重新挂到 `BagRoot` 下
- `SecureBagRoot` 继续作为完整安全箱块 root 使用
- 后续布局刷新只更新 `BagRoot / BagBoardRoot / SecureBagRoot / SecureBoardRoot`

这样可以在不破坏现有 YIUI 绑定的前提下，把“背包板子”和“背包块”拆开。

#### 3. 运行时缓存块内 chrome 相对关系

对背包块和安全箱块，首次初始化时缓存：

- `Bg`
- `Title`
- `HintText`（仅安全箱）
- `BoardRoot`
- 每个子节点相对 `BoardRoot` 左上角的偏移
- 背景相对 `BoardRoot` 的四边 inset

后续每次格子数变化时：

- 先计算新的 `BoardRoot` 像素尺寸
- 再反推出整个块的包围盒
- 更新 root 尺寸
- 更新 `Bg / Title / Hint / BoardRoot`

#### 4. 不破坏现有交互热区

当前实际拖拽、点击、落点计算依赖：

- `u_ComBagBoardRoot`
- `SecureBoardRoot`
- `u_ComBagItemsLayer`
- `SecureItemsLayer`

因此本轮不改这些语义入口，只调整：

- 它们的尺寸
- 它们在所属块内的位置
- 所属块 root 的尺寸

不把拖拽入口转移到新的代理节点。

#### 5. 三模式切换保护

`SearchPanel` 可能复用同一个面板实例，因此本轮额外补一层打开时恢复逻辑：

- `YIUIOpen` 时先恢复 `BagBoardRoot / SecureBagRoot` 的原始锚点和尺寸基线
- 再根据当前 `OpenMode` 重新建立运行时 `BagRoot` 和布局缓存
- 避免上一次 `BackpackInspect` 的布局基线污染下一次 `ContainerSearch / CorpseLoot`

#### 6. 分辨率处理策略

本轮不正式做断点适配，但要为后续适配预留空间：

- 不继续新增依赖绝对坐标的硬编码布局
- 块内尺寸刷新与打开模式解耦
- 若后续要做断点适配，可继续把 `SearchPanel` 外层拆成头部、容器区、拥有者区三个大区块，而块内尺寸刷新逻辑可以直接复用本轮实现

后续若要补分辨率适配，优先策略为：

- 把 `SearchPanel` 分成头部、左侧容器区、右侧拥有者区、底部操作区
- 在 prefab 层做双栏/窄双栏/单栏断点布局
- 格子区内部继续复用本轮的运行时尺寸刷新逻辑

## 实现步骤

1. 更新周计划、版本计划，建立设计文档与开发日志
2. 盘点 `SearchPanel` 现有背包块/安全箱块层级，确定可复用节点与新增容器
3. 在 `SearchPanelComponent` 中补齐布局缓存字段
4. 在 `SearchPanelComponentSystem` 中补运行时 `BagRoot`、块级缓存刷新与打开时恢复逻辑
5. 验证三模式下背包/安全箱区都走同一套自适应布局能力
6. 执行 `dotnet build ET.sln` 验证
7. 回写设计文档、开发日志与计划状态

## 验收标准

- [ ] `BackpackInspect` 模式下，背包背景贴合格子区域
- [ ] `BackpackInspect` 模式下，安全箱背景贴合格子区域并排在背包下方
- [ ] `ContainerSearch` 模式下，右侧背包/安全箱区仍保持正确贴合关系，安全箱不压到左侧容器区
- [ ] `CorpseLoot` 模式下，右侧背包/安全箱区仍保持正确贴合关系，安全箱不压到左侧容器区
- [ ] 切换不同背包/安全箱尺寸后，块布局随格子数变化
- [ ] 现有拖拽、点击、快速拾取等交互不回归
- [ ] `dotnet build ET.sln` 通过

## 关联文档

- [搜索界面三模式需求与方案说明](./搜索界面三模式需求与方案说明.md)
- [搜索界面三模式当前实现与待办清单](./搜索界面三模式当前实现与待办清单.md)
- [局内安全格拾取与保留设计文档](./局内安全格拾取与保留设计文档.md)
- [起装背包与安全箱自适应布局设计文档](./起装背包与安全箱自适应布局设计文档.md)

## 实现追踪

> 开发完成后回填

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md`、本设计文档、开发日志 | 无偏差 |
| 步骤2~5 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/SearchPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/SearchPanelComponentSystem.cs` | 实际落地改为“运行时 `BagRoot` + 块级缓存刷新”，未继续手改 prefab |
| 步骤6 | 2026-04-16 | `ET.sln` | `dotnet build ET.sln` 通过 |
| 步骤5补充 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/SearchPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/SearchPanelComponentSystem.cs`、本设计文档、开发日志 | 双栏模式补充保留 `SecureBagRoot` 原始区域基线，避免尸体/容器搜索时安全箱与左侧容器区重叠 |
