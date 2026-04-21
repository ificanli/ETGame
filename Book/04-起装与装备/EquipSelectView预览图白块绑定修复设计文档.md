# EquipSelectView预览图白块绑定修复设计文档

**创建时间**：2026-04-21
**最后更新**：2026-04-21
**状态**：已完成
**关联任务**：M0.2-W3 #47
**涉及包**：cn.etetet.statesync, cn.etetet.yiuimcp

## 需求概述

当前 `EquipSelectView` 右侧武器预览区域中，名称与描述正常，但图标区域显示为白块。

本轮目标明确为：

1. 修正 `EquipSelectView` 右侧预览图标显示。
2. 优先通过 `YIUIMCP` 驱动 Unity Editor 修改绑定，不直接手改 prefab YAML。
3. 不扩大到起装页其余布局或文案逻辑。

## 技术方案

### 整体思路

先复核现有运行时链路，再对 prefab 里的 YIUI 绑定做最小修复。

当前代码在 `EquipSelectViewComponentSystem.cs` 里通过：

1. `uiBase.ComponentTable.FindComponent<Image>("SelectImage")`
2. 然后把武器 icon sprite 赋给 `SelectImage`

但 `EquipSelectView.prefab` 里 `SelectImage` 这条绑定，当前实际指向的是右侧 `icon` 节点的 `RectTransform`，不是它的 `Image` 组件。

结果是：

1. 运行时 `FindComponent<Image>("SelectImage")` 拿不到正确组件。
2. 右侧真实 `Image` 没被赋 sprite。
3. `icon` 节点默认 `m_Sprite: {fileID: 0}`，因此显示成白块。

本轮修复收口为：

1. 新增一个最小 Unity Editor 菜单，仅修 `EquipSelectView.prefab` 的 `SelectImage` 绑定目标。
2. 通过 `YIUIMCP ExecuteMenu` 执行该菜单，让绑定从 `RectTransform` 改为 `Image`。
3. 允许 `UIBindComponentTable.AutoCheck()` 把组件 key 规范成 `u_ComSelectImage`。
4. 在运行时补一层 `u_ComSelectImage / SelectImage` 双 key 兼容，继续复用现有 `EquipSelectViewPreviewHelper`。

### 为什么这轮不直接重生成 YIUI 代码

当前 `EquipSelectViewComponent.cs` 手写层已经额外声明了：

1. `Image SelectImage`
2. `UIDataValueString GunDescData`

它们是为了兼容当前生成层未覆盖到这两条绑定的临时承载。

如果这轮直接把 `SelectImage` 纳入 YIUI 生成链，会生成同名字段 `SelectImage`，与手写 partial 冲突。

因此本轮只修 prefab 绑定，不触碰 `Gen` 文件，避免把一个显示问题扩大成“绑定修复 + 生成链重构”。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 补录 W3 #47 任务 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 补录版本变更记录 |
| `Book/04-起装与装备/EquipSelectView预览图白块绑定修复设计文档.md` | 新增 | 建立本轮设计文档 |
| `Book/04-起装与装备/EquipSelectView预览图白块绑定修复开发日志.md` | 新增 | 建立本轮开发日志 |
| `Packages/cn.etetet.statesync/Editor/EquipSelectViewBindingFixEditor.cs` | 新增 | 提供可被 YIUIMCP `ExecuteMenu` 驱动的 Unity Editor 修复入口 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/EquipSelectViewComponentSystem.cs` | 修改 | 兼容 `u_ComSelectImage / SelectImage` 双 key 查找 |
| `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectView.prefab` | 修改 | 由 Unity Editor 菜单落地修正 `SelectImage` 绑定目标 |

## Entity/Component 设计

本轮不新增 Entity，不改协议。

运行时业务逻辑只增加一处绑定兼容：

1. 优先查找 `u_ComSelectImage`
2. 兼容回退旧 key `SelectImage`

## 接口设计

不新增网络接口。

仅新增一个 Unity Editor 菜单入口，供 `YIUIMCP ExecuteMenu` 调用。

## 实现步骤

1. 补计划、设计文档、开发日志门禁。
2. 新增 Editor 菜单，并通过 YIUIMCP 执行绑定修复。
3. 补运行时双 key 兼容，复核 prefab 绑定结果并执行 `dotnet build ET.sln`。

## 验收标准

- [x] `EquipSelectView` 右侧预览图不再显示白块
- [x] `SelectImage` 绑定改为右侧 `icon` 节点上的 `Image`
- [x] 修复过程通过 YIUIMCP 驱动，不直接手改 prefab YAML
- [x] `dotnet build ET.sln` 通过

## 关联文档

- [起装武器预览属性显示设计文档.md](./起装武器预览属性显示设计文档.md)
- [起装武器预览属性显示开发日志.md](./起装武器预览属性显示开发日志.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-21 | `Book/08-版本计划/M0.2-W3周计划.md`, `Book/08-版本计划/M0.2版本计划.md`, `Book/04-起装与装备/EquipSelectView预览图白块绑定修复设计文档.md`, `Book/04-起装与装备/EquipSelectView预览图白块绑定修复开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-21 | `Packages/cn.etetet.statesync/Editor/EquipSelectViewBindingFixEditor.cs`, `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectView.prefab` | 绑定经 Unity Editor 修正后，被 YIUI 自动规范成 `u_ComSelectImage`，属于编辑器侧预期行为 |
| 步骤3 | 2026-04-21 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/EquipSelectViewComponentSystem.cs`, `ET.sln` | 为兼容 YIUI 自动规范后的 key，额外补了一层 `u_ComSelectImage / SelectImage` 双 key 查找；`dotnet build ET.sln` 通过 |
