# PickupHintPanel 开发日志

**功能**：局内地面物品拾取提示（PickupHintPanel）
**关联设计文档**：[PickupHintPanel-YIUI创建指南.md](./PickupHintPanel-YIUI创建指南.md)
**关联任务**：M0.2-W3 #35
**开始时间**：2026-04-16
**最后更新**：2026-04-20
**开发者**：AI

## 开发进度

- [x] 步骤1：回写周计划、版本计划与设计文档，明确任务 35 退回开发中
- [x] 步骤2：把 `PickupHintPanel` 从独立 `Panel` 改成可重复实例化的 `Common`
- [x] 步骤3：在 `MainPanel` 中实现多个地面掉落提示的创建、跟随与错位共存
- [x] 步骤4：执行 `dotnet build ET.sln` 验证并回写文档

## 决策记录

### 2026-04-16 - 本轮不扩 `M2C_ECAInteractHint`
- **背景**：现有客户端交互 hint 链路只提供 `PointId/ButtonTextId/CanInteract`，没有 `ItemConfigId`。
- **方案**：先保留通用“拾取”文案，不扩协议。
- **原因**：这轮核心问题是 UI 类型与重叠策略，不是掉落物文案来源；扩协议会把改动面扩大到 `proto/map/client`。
- **替代方案**：直接给 `M2C_ECAInteractHint` 补 `ItemConfigId`；当前放弃。

### 2026-04-20 - 放弃独立 Panel，改成 MainPanel 内多实例 Common
- **背景**：用户明确指出独立 `Panel` 会遮住别的东西，而且多个近距离掉落点需要同时显示。
- **方案**：保留 `PickupHintPanel` 资源名，但运行时类型改为 `Common`，由 `MainPanel` 统一管理多个实例。
- **原因**：`Panel` 天然属于顶层 UI，`View` 又默认更适合单实例；可重复 `Common` 更符合“同宿主内多项共存”的需求。
- **替代方案**：继续使用单个 `Panel` 或改成单个 `View`；都不能自然解决“错位共存”。

### 2026-04-20 - 位置来源改为运行时虚拟 Unit，而不是静态 ECA 本地配置
- **背景**：`ground_drop_*` 点位由服务端运行时生成，不在客户端静态 `LocalInteractPoints` 配置里。
- **方案**：从 `pointId=ground_drop_{playerId}_{pointUnitId}` 解析 `pointUnitId`，再从 `UnitComponent` 获取虚拟单位位置。
- **原因**：这是当前工程里最直接、最稳定、且不需要扩协议的位置来源。
- **替代方案**：扩交互 hint 协议直接下发屏幕/世界坐标；当前放弃。

### 2026-04-20 - Common 直接显隐复用，不再走独立 OpenPanelAsync
- **背景**：`PickupHintPanel` 改成 `Common` 后，如果继续沿用 `OpenPanelAsync/ClosePanel`，运行时语义会和资源类型不一致。
- **方案**：实例首次创建时直接 `YIUIFactory.Instantiate`，默认隐藏；后续只通过 `Show/Hide/SetAnchoredPosition` 复用。
- **原因**：`Common` 本质是宿主内局部组件，直接显隐更贴合这一层级，也避免再走旧顶层 panel 管理链路。
- **替代方案**：保留 `IYIUIOpen` 并强制每次 `YIUIEventSystem.Open`；当前仅保留接口兼容旧编译链路，不作为主显示入口。

### 2026-04-20 - 清理 MainPanel 中废弃的单实例拾取提示残留
- **背景**：多实例 `Common` 方案已经稳定接管显示，但 `MainPanel` 内还保留着旧 `OpenPanelAsync/ClosePanel` 单实例入口和对应状态字段。
- **方案**：删除 `RefreshPickupHintPanel/EnsurePickupHintPanelOpenAsync/HidePickupHintPanel` 以及 `IsPickupHintPanelOpening/LastPickupHintVisible/LastPickupHintPointId`。
- **原因**：这些代码已经不再参与主链路，继续保留只会增加误用风险，也会让后续维护者误判当前实现仍依赖顶层 `Panel`。
- **替代方案**：保留旧方法作为“兼容备用入口”；当前放弃，因为没有真实调用点，保留收益小于维护成本。

## 问题日志

### 2026-04-20 - 现有多掉落点逻辑实际上只有单焦点
- **现象**：多个掉落点靠近时，`MainPanel` 只会显示一个提示。
- **原因**：`M2C_ECAInteractHintHandler` 在 `InRange=true` 时直接覆写 `runtime.FocusPointId`，离开时也只是从 `InRangePointIds` 中挑第一个顶上；`MainPanel` 也只按单个焦点刷新。
- **解决**：本轮把拾取提示逻辑从“焦点驱动”切到“扫描全部 `ground_drop_*` 点位驱动”。

### 2026-04-20 - `ET.HotfixView.csproj` 为显式 Compile Include
- **现象**：新增 `MainPanelComponentSystem_PickupHints.cs` 后，`dotnet build ET.sln` 提示 `ReleaseAllPickupHintCommons/RefreshPickupHintCommons` 不存在。
- **原因**：当前 `ET.HotfixView.csproj` 不是通配符收集源码，而是显式 `Compile Include`；新文件不会自动进入编译。
- **解决**：撤回临时 partial 文件，把多实例拾取提示方法合并回已纳入编译的 `MainPanelComponentSystem.cs`，避免留下脆弱的 csproj 补丁。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-20 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 任务 35 状态退回开发中，并改为 `MainPanel` 内多实例 `Common` 方案 |
| 2026-04-20 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 增加任务 35 本轮方案回退与重定记录 |
| 2026-04-20 | `Book/02-战斗系统/技术方案/PickupHintPanel-YIUI创建指南.md` | 修改 | 设计口径从独立 `Panel` 更新为多实例 `Common` |
| 2026-04-20 | `Packages/cn.etetet.statesync/Editor/PickupHintPanelYiuiBuilder.cs` | 修改 | Builder 改为生成固定尺寸 `Common` 卡片 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab` | 修改 | prefab 根节点改为小卡片，关闭背景射线，切换 `UICodeType=Common` |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Main/PickupHintPanelComponentGen.cs` | 修改 | Gen 类型从 `Panel` 调整为 `Common` |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Main/PickupHintPanelComponentSystemGen.cs` | 修改 | 移除 `UIWindow/UIPanel` 绑定 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/PickupHintPanelComponent.cs` | 修改 | 保留 `IYIUIOpen` 兼容旧编译链路，并作为 `Common` 数据组件承载点位 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/PickupHintPanelComponentSystem.cs` | 修改 | 增加 `Show/Hide/SetAnchoredPosition`，默认隐藏并关闭非按钮射线 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 新增 `PickupHintRoot` 与多实例缓存字典 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 切换到多实例拾取提示刷新、位置解析、错位布局与统一清理 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 删除旧单实例拾取提示状态字段，避免和多实例 `Common` 主链路并存 |
| 2026-04-20 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 删除废弃 `OpenPanelAsync/ClosePanel` 单实例拾取提示方法，收口到当前多实例实现 |

## 开发总结

- **实际完成**：已完成 `PickupHintPanel -> Common` 资源切换、`MainPanel` 多实例拾取提示、`ground_drop_*` 屏幕跟随与错位共存，并收掉旧单实例残留代码；当前实现已通过 `dotnet build ET.sln`。
- **未完成**：尚未做 Unity / 局内人工回归，尤其需要确认多个近距离掉落点、与其他交互点混合出现、以及进入容器搜索时的提示收口。
- **与设计的偏差**：设计阶段曾计划单独新增 `MainPanelComponentSystem_PickupHints.cs` partial，但由于当前 `ET.HotfixView.csproj` 采用显式 `Compile Include`，最终把方法合并回 `MainPanelComponentSystem.cs`。
- **后续待办**：进 Unity 验证多掉落点“错位共存”、按钮点击拾取、离开范围清理与不遮挡其他 UI 的实际表现。
