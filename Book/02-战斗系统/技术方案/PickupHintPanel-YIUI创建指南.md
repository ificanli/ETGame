# PickupHintPanel — YIUI Prefab 创建指南

**创建时间**：2026-04-16
**最后更新**：2026-04-16
**状态**：已完成
**关联任务**：M0.2-W3 #35
**涉及包**：cn.etetet.statesync, cn.etetet.map
**关联实现**：[丢弃与拾取武器实现记录.md](./丢弃与拾取武器实现记录.md)

## 1. 需求概述

当玩家靠近 `ground_drop_` 地面掉落点时，现有 `MainPanel` 只会显示通用 `SearchButton`，点击后直接走一键拾取。当前缺口是没有独立的轻量拾取提示面板，导致地面武器/物品交互识别度不够，也无法和普通搜索容器语义分离。

本轮目标是把 `PickupHintPanel` 正式落到 YIUI 资源层，并在 `MainPanel` 中补齐打开、刷新和关闭逻辑。

## 2. 本轮边界

- 正式新增 `PickupHintPanel.prefab`
- 正式补齐 `u_DataItemName` / `u_EventPickup` 绑定
- 在 `MainPanelComponentSystem.LateUpdate` 中根据 `ground_drop_` 焦点打开或关闭面板
- 点击 `PickupHintPanel` 按钮后继续复用现有 `GroundItemPickupClientHelper`
- 当前 **不扩 `M2C_ECAInteractHint` 协议**
- 当前 **不解析真实掉落物名/图标**，面板文案先稳定显示通用“拾取”

## 3. 已就绪的代码文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `PickupHintPanelComponentGen.cs` | `ModelView/Client/YIUIGen/Main/` | Gen 层：Panel 定义、绑定字段声明 |
| `PickupHintPanelComponent.cs` | `ModelView/Client/YIUIComponent/Main/` | 自定义数据：`FocusPointId`, `ItemConfigId` |
| `PickupHintPanelComponentSystemGen.cs` | `HotfixView/Client/YIUIGen/Main/` | Gen System：Awake、YIUIBind |
| `PickupHintPanelComponentSystem.cs` | `HotfixView/Client/YIUISystem/Main/` | 自定义逻辑：YIUIInitialize、YIUIOpen、拾取按钮点击 |

## 4. 资源路径与构建方式

旧文档中的 `Assets/GameRes/YIUI/Packages/Main/` 已过时。当前工程真实资源路径是：

- `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab`

本轮不手改 Gen 文件，统一通过编辑器菜单构建：

- `ET/YIUI/Build PickupHintPanel Resources`

## 5. Prefab 结构

```text
PickupHintPanel (Panel 根节点)
|
└── Content (RectTransform, 锚点底部中央, sizeDelta: 360×134)
    |
    ├── Background (Image, 半透明深色底)
    ├── Accent (Image, 左侧高亮条)
    ├── ItemName (TMP_Text, 左中，默认文案“拾取”)
    ├── SubTitle (TMP_Text, 静态说明文本)
    └── PickupButton (Button, 右侧，112×52)
        └── Text: "拾取"
```

布局原则：

- 面板位于屏幕底部中央，抬高到摇杆上方
- 不再和普通 `SearchButton` 共用同一视觉语义
- 面板只负责“当前焦点是地面掉落物”的轻提示，不承担搜索容器职责
- 静态文本区与按钮区在 prefab 层显式拆开，避免运行时再用代码修布局

## 6. YIUI 绑定

### 6.1 DataTable

| 绑定名 | 绑定类型 | 目标节点 | 说明 |
|--------|---------|---------|------|
| `u_DataItemName` | `UIDataValueString` | `ItemName` (TMP_Text) | 物品名称文本 |

### 6.2 EventTable

| 绑定名 | 绑定类型 | 目标节点 | 说明 |
|--------|---------|---------|------|
| `u_EventPickup` | `UITaskEventP0` | `PickupButton` (Button) | 拾取按钮点击事件 |

### 6.3 绑定组件

- `ItemName` 节点挂 `UIDataBindTextTMP`，绑定 `u_DataItemName`
- `PickupButton` 节点挂 `UITaskEventBindClick`，绑定 `u_EventPickup`

## 7. 运行时联动方案

### 7.1 打开条件

在 `MainPanelComponentSystem.LateUpdate` 中：

- 当前有 `focusPointId`
- `focusPointId.StartsWith("ground_drop_")`
- 当前没有打开容器 `OpenContainerPointId`
- 当前点位仍可交互

满足时打开或刷新 `PickupHintPanel`。

### 7.2 关闭条件

- 焦点为空
- 焦点不再是 `ground_drop_`
- 点位不可交互
- 当前已经进入普通容器搜索态

### 7.3 当前文案策略

当前 `M2C_ECAInteractHint` 不带 `ItemConfigId`，而 `ground_drop_{playerId}_{unitId}` 的 `pointId` 也无法在客户端直接反解出配置。因此本轮 `PickupHintPanel` 的文案策略是：

- 默认显示 `拾取`
- 保留 `SetPickupTarget(pointId, itemConfigId)` 接口
- 后续协议一旦补 `ItemConfigId`，无需重做 prefab，只需补消息和客户端赋值

## 8. 构建与验证

1. 执行 Unity 菜单：`ET/YIUI/Build PickupHintPanel Resources`
2. 确认生成：
   - `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab`
   - `PickupHintPanelComponentGen.cs`
   - `PickupHintPanelComponentSystemGen.cs`
3. 运行：
   - `dotnet build ET.sln`

## 9. 验收标准

- [ ] 靠近地面掉落物时，不再只依赖 `SearchButton`
- [ ] `PickupHintPanel` 能正常打开和关闭
- [ ] 点击 `PickupHintPanel` 的拾取按钮可复用现有一键拾取链路
- [ ] 离开地面掉落点或切入其他交互点时，`PickupHintPanel` 不残留
- [x] `dotnet build ET.sln` 通过

## 10. 实现追踪

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 计划登记与设计收口 | 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/08-版本计划/M0.2版本计划.md`、`Book/02-战斗系统/技术方案/PickupHintPanel-YIUI创建指南.md` | 旧指南资源路径已过时，已按当前工程真实路径修正 |
| Builder 与 prefab 正式落地 | 2026-04-16 | `Packages/cn.etetet.statesync/Editor/PickupHintPanelYiuiBuilder.cs`、`Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab`、`Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Main/PickupHintPanelComponentGen.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Main/PickupHintPanelComponentSystemGen.cs` | 实际资源为增强可读性新增 `SubTitle` 静态文本，并把 `Content` 调整为 `360x134` |
| MainPanel 正式联动 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | `ground_drop_` 焦点不再继续显示旧 `SearchButton`，改为单独打开/刷新 `PickupHintPanel` |
| 构建验证与文档收口 | 2026-04-16 | `Book/02-战斗系统/技术方案/PickupHintPanel开发日志.md`、`Book/08-版本计划/M0.2-W3周计划.md` | 实际执行顺序增加了 `TriggerCompile/GetCompileResult`，用于确保 Unity 菜单运行的是最新 Builder |
