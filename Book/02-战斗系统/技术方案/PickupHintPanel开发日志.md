# PickupHintPanel 开发日志

**功能**：局内地面物品拾取提示 Panel（PickupHintPanel）
**关联设计文档**：[PickupHintPanel-YIUI创建指南.md](./PickupHintPanel-YIUI创建指南.md)
**关联任务**：M0.2-W3 #35
**开始时间**：2026-04-16
**开发者**：AI

## 开发进度

- [x] 步骤1：补周计划与版本计划登记，收口设计文档
- [x] 步骤2：新增 YIUI Builder 菜单并生成 `PickupHintPanel.prefab`
- [x] 步骤3：补 `MainPanel` 对 `PickupHintPanel` 的打开/关闭/刷新联动
- [x] 步骤4：执行 `dotnet build ET.sln` 验证并回写文档

## 决策记录

### 2026-04-16 - 本轮不扩 `M2C_ECAInteractHint`
- **背景**：现有 `PickupHintPanelComponentSystem` 支持 `ItemConfigId`，但客户端交互 hint 链路只提供 `PointId/ButtonTextId/CanInteract`。
- **方案**：本轮先正式落地 prefab 与交互入口，面板文案退回通用“拾取”。
- **原因**：用户当前明确要先落地功能，协议扩展会额外牵涉 `proto/map/client` 多包修改，不是本轮最小可交付闭环。
- **替代方案**：直接扩 `M2C_ECAInteractHint` 增加 `ItemConfigId`；当前先不做，避免扩大变更面。

### 2026-04-16 - 用 Builder 正式产出 prefab，不手搓 YAML
- **背景**：旧指南资源路径已过时，且手改 YIUI prefab 容易造成 `CDE/Data/Event/Gen` 漂移。
- **方案**：新增 `ET/YIUI/Build PickupHintPanel Resources` 菜单，复用项目现有 `Home/BattleRecord` Builder 模式。
- **原因**：这条路径更符合当前工程已有实践，也方便后续继续通过 `ExecuteMenu` 自动构建。
- **替代方案**：直接编辑 prefab YAML；当前放弃，风险更高。

### 2026-04-16 - 先用 `TriggerCompile/GetCompileResult` 再执行 Unity 菜单
- **背景**：`ExecuteMenu` 首次返回成功，但 Unity 编辑器仍可能运行旧域里的 Builder，导致日志里继续命中已修复的旧异常。
- **方案**：先通过 `TriggerCompile` 强制 Unity 域重建，再用 `GetCompileResult` 确认 `Success, No errors!`，最后重新执行 `ET/YIUI/Build PickupHintPanel Resources`。
- **原因**：这样可以确保菜单实际执行的是最新脚本，而不是旧编译产物。
- **替代方案**：直接重复执行菜单等待偶发刷新；当前放弃，结果不稳定。

## 问题日志

### 2026-04-16 - 旧指南资源路径已失效
- **现象**：文档写的是 `Assets/GameRes/YIUI/Packages/Main/`，但当前工程真实 prefab 路径在 `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/`。
- **原因**：项目 YIUI 资源已按 package 内聚迁移，旧文档未同步。
- **解决**：设计文档中已改为当前真实路径，并明确通过编辑器菜单构建。

### 2026-04-16 - `ExecuteMenu` 成功不代表 Builder 一定成功
- **现象**：Unity MCP 返回“菜单命令执行成功”，但本地并没有生成 `PickupHintPanel.prefab`。
- **原因**：UTO 的 `ExecuteMenu` 只保证菜单被触发，不保证菜单内部没有旧编译域异常；实际失败信息需要回看 `Editor.log`。
- **解决**：先查 `Editor.log` 定位真实异常，再修 Builder 反射逻辑与 Unity 编译状态，最后用 `TriggerCompile/GetCompileResult` 收口后重跑菜单。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增 W3#35 PickupHintPanel 落地任务 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 补版本计划变更记录 |
| 2026-04-16 | `Book/02-战斗系统/技术方案/PickupHintPanel-YIUI创建指南.md` | 修改 | 将旧创建指南升级为当前有效设计文档 |
| 2026-04-16 | `Book/02-战斗系统/技术方案/PickupHintPanel开发日志.md` | 新增 | 创建开发日志并记录当前决策 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Editor/PickupHintPanelYiuiBuilder.cs` | 新增 | 新增 `ET/YIUI/Build PickupHintPanel Resources` Builder，统一生成 prefab 与 YIUI 代码 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/PickupHintPanel.prefab` | 新增 | 正式落地 PickupHintPanel prefab 资源 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 补拾取提示面板运行态防抖字段 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 在 `LateUpdate` 中接入地面掉落点的 PickupHintPanel 打开/关闭/刷新逻辑，并对 `ground_drop_` 隐藏旧 SearchButton |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIGen/Main/PickupHintPanelComponentGen.cs` | 新增 | 生成 PickupHintPanel Gen 绑定定义 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Main/PickupHintPanelComponentSystemGen.cs` | 新增 | 生成 PickupHintPanel Gen System 绑定代码 |

## 开发总结

- **实际完成**：已新增 `PickupHintPanel` 正式 Builder、生成 `PickupHintPanel.prefab` 与对应 `YIUIGen` 文件，并在 `MainPanel` 中补齐 `ground_drop_` 焦点的打开/关闭/刷新逻辑；最终 `dotnet build ET.sln` 通过。
- **未完成**：尚未进行局内人工回归；真实物品名/图标仍未接协议，当前继续显示通用“拾取”。
- **与设计的偏差**：正式 prefab 在原方案基础上新增了 `SubTitle` 静态说明文本，并把 `Content` 调整为 `360x134`，用于明确正文区和按钮安全区边界，避免底部 CTA 与文本区过挤。
- **后续待办**：进 Unity / 游戏内验证 `PickupHintPanel` 的打开、关闭与拾取链路；若后续协议补 `ItemConfigId`，只需接消息赋值，无需重做 prefab。
