# PickupHintPanel — YIUI 创建与运行时设计

**创建时间**：2026-04-16
**最后更新**：2026-04-20
**状态**：已完成
**关联任务**：M0.2-W3 #35
**涉及包**：cn.etetet.statesync, cn.etetet.map
**关联实现**：[丢弃与拾取武器实现记录.md](./丢弃与拾取武器实现记录.md)

## 1. 需求概述

当前 `PickupHintPanel` 已有 prefab、Builder 和 `MainPanel` 联动，但实现路线仍是“独立 Panel + 单焦点点位”。这条路线有两个问题：

- 作为独立 `Panel` 会参与顶层 UI 层级，容易遮住别的交互元素。
- 多个 `ground_drop_*` 地面掉落点靠得很近时，当前只会保留一个 `FocusPointId`，没有真实的重叠处理。

用户最新确认的目标是：

- `PickupHintPanel` 不再做成独立 `Panel`
- 需要支持多个近距离地面掉落点同时存在
- 多提示重叠时采用“错位共存”，而不是只保留一个

## 2. 本轮边界

- 保留资源名与组件名 `PickupHintPanel`，避免扩大资源重命名范围
- 运行时类型从独立 `Panel` 收口为 `MainPanel` 内部可重复实例化的 `Common`
- `MainPanel` 负责管理多个拾取提示实例
- 每个提示实例只负责一个 `ground_drop_*` 点位
- 当前 **不扩 `M2C_ECAInteractHint` 协议**
- 当前 **不解析真实掉落物图标**
- 当前 **物品名默认继续显示通用“拾取”**，接口保留 `ItemConfigId` 以兼容后续协议扩展

## 3. 技术方案

### 3.1 资源与生成层

`PickupHintPanel.prefab` 继续保留在：

- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab`

但其 YIUI 类型改为 `Common`：

- `PickupHintPanelComponentGen.cs` 从 `Panel` 改为 `Common`
- `PickupHintPanelComponentSystemGen.cs` 不再绑定 `YIUIWindowComponent / YIUIPanelComponent`
- prefab 根节点不再拉满全屏，而是改成一张可局部挂载的小卡片
- `PickupHintPanelYiuiBuilder.cs` 也同步把 `UICodeType` 改成 `EUICodeType.Common`

### 3.2 MainPanel 运行时管理

`MainPanel` 不再只根据单个 `FocusPointId` 决定是否显示拾取提示，而是每帧收集当前所有满足以下条件的点位：

- 点位在 `runtime.InRangePointIds` 中
- `pointId.StartsWith("ground_drop_")`
- `runtime.PointCanInteract[pointId] == true`
- 当前没有打开搜索容器 `OpenContainerPointId`

然后为每个点位维护一个 `PickupHintPanelComponent` 实例：

- 首次出现时：`YIUIFactory.Instantiate<PickupHintPanelComponent>(...)`
- 实例创建后默认隐藏，后续通过 `Show/Hide/SetAnchoredPosition` 直接复用
- 点位失效时：隐藏实例；场景关闭或 `MainPanel` 销毁时统一清理

补充说明：

- 当前 `ET.HotfixView.csproj` 使用显式 `Compile Include`
- 因此本轮没有保留额外的 `MainPanelComponentSystem_PickupHints.cs` 文件
- 多实例拾取提示方法最终合并在 `MainPanelComponentSystem.cs` 中实现
- 旧的单实例 `OpenPanelAsync/ClosePanel` 入口与对应状态字段已在收口阶段删除，避免和当前 `Common` 方案并存

### 3.3 点位位置解析

`ground_drop_*` 的点位不是静态 ECA 配置点，不能从 `LocalInteractPoints` 直接拿位置。本轮使用运行时点位单位的位置：

- 服务端命名规则：`ground_drop_{playerId}_{pointUnitId}`
- 客户端从 `pointId` 解析出 `pointUnitId`
- 通过 `root.CurrentScene()?.GetComponent<UnitComponent>()?.Get(pointUnitId)` 拿到虚拟 `Unit`
- 用 `Unit.Position -> Camera.main.WorldToScreenPoint -> RectTransformUtility.ScreenPointToLocalPointInRectangle`
  计算提示卡片在 `MainPanel` 上的锚点位置

### 3.4 错位共存策略

多掉落点很近时，不做互斥隐藏，而是做局部错位：

1. 先把当前 `FocusPointId` 对应点位放在最前，其余点位按 `pointId` 字典序稳定排序
2. 逐个放置提示卡片
3. 若新卡片与已放置卡片在阈值范围内重叠，则按 `(+28,+52)`、`(-28,+52)`、逐层递增的方式错位
4. 最终位置做边界 clamp，避免提示跑出屏幕

本轮目标是先稳定做到“看得见、点得到、不互相完全盖住”，不追求复杂避让算法。

### 3.5 交互策略

每个提示项自己的 `PickupButton` 直接绑定对应 `pointId`：

- 点击后继续复用 `GroundItemPickupClientHelper.RequestPickupGroundItem`
- 不再依赖先把该点位设为 `FocusPointId` 再拾取

普通 `SearchButton` 仍只服务非 `ground_drop_*` 点位。

## 4. 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 任务 35 退回开发中并更新备注 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 增加本轮方案变更记录 |
| `Book/02-战斗系统/技术方案/PickupHintPanel-YIUI创建指南.md` | 修改 | 将独立 Panel 方案更新为 Common 多实例方案 |
| `Book/02-战斗系统/技术方案/PickupHintPanel开发日志.md` | 修改 | 记录本轮方案回退与实现过程 |
| `Packages/cn.etetet.statesync/Editor/PickupHintPanelYiuiBuilder.cs` | 修改 | Builder 改为生成 Common 小卡片 |
| `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab` | 修改 | prefab 根与 CDE 类型改为 Common |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Main/PickupHintPanelComponentGen.cs` | 修改 | Gen 类型从 Panel 改为 Common |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Main/PickupHintPanelComponentSystemGen.cs` | 修改 | 移除 Panel 专属绑定 |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/PickupHintPanelComponent.cs` | 修改 | 保留 `IYIUIOpen` 兼容旧编译链路，并承载点位/物品名数据 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/PickupHintPanelComponentSystem.cs` | 修改 | 增加显示/位置刷新接口 |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 新增多实例拾取提示缓存字段 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 切换 `LateUpdate` 入口到多实例逻辑，并合并创建、刷新、错位与清理实现 |

## 5. 实现步骤

1. 回写计划与设计文档，明确任务 35 当前处于“开发中”
2. 将 `PickupHintPanel` 的 Builder / prefab / Gen 静态层从 `Panel` 改为 `Common`
3. 在 `MainPanel` 中实现地面掉落提示实例池、屏幕跟随与错位共存
4. 执行 `dotnet build ET.sln`
5. 回写开发日志与实现追踪，任务状态推进到“未回归”

## 6. 验收标准

- [ ] 靠近多个地面掉落点时，可同时看到多个拾取提示
- [x] 提示不再作为独立顶层 `Panel` 遮挡其他 UI
- [ ] 多个近距离提示不会完全重叠，能做到错位共存
- [x] 点击任意提示项自己的按钮可直接复用现有拾取链路
- [ ] 点位失效、离开范围或进入容器搜索时，相关提示不会残留
- [x] `dotnet build ET.sln` 通过

## 7. 实现追踪

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 方案回退与文档收口 | 2026-04-20 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md`、`Book/02-战斗系统/技术方案/PickupHintPanel-YIUI创建指南.md` | 用户确认不再使用独立 Panel，统一改为 `MainPanel` 内多实例 Common |
| 资源与运行时改造 | 2026-04-20 | `Packages/cn.etetet.statesync/Editor/PickupHintPanelYiuiBuilder.cs`、`Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab`、`Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Main/PickupHintPanelComponentGen.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Main/PickupHintPanelComponentSystemGen.cs`、`Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/PickupHintPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/PickupHintPanelComponentSystem.cs`、`Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 已完成；多实例逻辑最终合并进 `MainPanelComponentSystem.cs`，未保留独立 partial 文件 |
| 遗留代码收口 | 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 删除旧单实例 `Panel` 打开/关闭逻辑与状态字段，避免后续误用 |
| 编译验证 | 2026-04-20 | `ET.sln` | `dotnet build ET.sln` 通过 |
