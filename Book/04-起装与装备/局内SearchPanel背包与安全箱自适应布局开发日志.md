# 局内SearchPanel背包与安全箱自适应布局开发日志

**功能**：局内 SearchPanel 背包与安全箱自适应布局  
**关联设计文档**：[局内SearchPanel背包与安全箱自适应布局设计文档](./局内SearchPanel背包与安全箱自适应布局设计文档.md)  
**关联任务**：M0.2-W3 #33  
**开始时间**：2026-04-16  
**开发者**：AI

## 开发进度

- [x] 步骤1：更新计划并建立设计文档、开发日志
- [x] 步骤2：分析 SearchPanel 现有背包/安全箱层级并确定布局方案
- [x] 步骤3：实现 SearchPanel 背包/安全箱自适应布局
- [x] 步骤4：执行编译验证并回写文档状态

## 决策记录

### 2026-04-16 - SearchPanel 改为“运行时 BagRoot + 块级缓存刷新”方案

- **背景**：局外 `LobbyPanel` 的块级布局缓存已经验证可行，但局内 `SearchPanel` 的现有 `BagBoardRoot` 不是完整块 root，直接手改 prefab 容器会带来较大的 YAML 维护成本。
- **方案**：运行时为背包区补一个 `BagRoot`，把 `BagBoardRoot` 重新挂到 `BagRoot` 下；安全箱继续沿用现有 `SecureBagRoot`；复用局外同款 `Bg / Title / Hint / BoardRoot` 缓存与尺寸重排算法。
- **原因**：这样可以保住现有 YIUI 绑定和拖拽/点击语义入口，同时把“背景贴格子、安全箱跟随背包下移”收口到同一套布局逻辑里。
- **替代方案**：手改 `SearchPanel.prefab` 增加共同父容器。放弃原因是当前只为一处局内界面补布局，运行时容器的改动面更小、回滚成本也更低。

### 2026-04-16 - 打开面板时必须先恢复布局基线

- **背景**：`SearchPanel` 存在面板实例复用的可能；如果上一把是在 `BackpackInspect` 模式打开，直接沿用上一次运行时布局，会污染下一次 `ContainerSearch / CorpseLoot` 的初始基线。
- **方案**：在 `YIUIOpen` 前置执行 `ResetOwnedAreaLayout`，恢复 `BagBoardRoot / SecureBagRoot` 的原始锚点与尺寸，再按当前 `OpenMode` 建立新的布局缓存。
- **结果**：三种模式都走同一套布局刷新逻辑，但不会互相串布局。

### 2026-04-16 - 双栏模式不能沿用单栏堆叠坐标

- **背景**：用户在 `CorpseLoot` 回归里发现安全箱和容器区域叠在一起；根因是当前把“背包在上、安全箱在下”的纵向堆叠策略应用到了所有模式。
- **方案**：新增 `SecureAreaTopLeft` 缓存；`BackpackInspect` 继续按背包块高度向下堆叠安全箱，`ContainerSearch / CorpseLoot` 改为保留 `SecureBagRoot` 的 prefab 原始区域基线，只刷新块内尺寸。
- **原因**：双栏模式下 prefab 已经给右侧拥有者区域分好了位置，再做跨区域纵向重排只会把安全箱挤进左侧容器列。
- **替代方案**：给 `SearchPanel` 额外 hard code 一组双栏坐标。放弃原因是模式特化坐标维护成本更高，也更容易和 prefab 后续调整脱节。

## 问题日志

### 2026-04-16 - 尸体搜索时安全箱与容器区域重叠

- **现象**：`CorpseLoot` / `ContainerSearch` 打开后，安全箱块会压进左侧容器区域。
- **原因**：布局缓存只记了背包块原始左上角，安全箱位置始终按“背包下方 + 间距”计算，双栏模式没有保留 `SecureBagRoot` 的原始区域基线。
- **解决**：在 `SearchPanelComponent` 中缓存 `SecureAreaTopLeft`，双栏模式按 prefab 原始区域放置安全箱，单栏模式仍沿用纵向堆叠。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增 W3 任务33「局内 SearchPanel 背包与安全箱自适应布局」 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录 |
| 2026-04-16 | `Book/04-起装与装备/局内SearchPanel背包与安全箱自适应布局设计文档.md` | 新增 | 建立专项设计文档 |
| 2026-04-16 | `Book/04-起装与装备/局内SearchPanel背包与安全箱自适应布局开发日志.md` | 新增 | 建立专项开发日志 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/SearchPanelComponent.cs` | 修改 | 新增 SearchPanel 拥有者区布局缓存字段与原始锚点缓存 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/SearchPanelComponentSystem.cs` | 修改 | 补运行时 `BagRoot`、块级布局刷新、打开时恢复逻辑，并在三模式渲染后统一重排背包/安全箱 |
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 任务33备注补充双栏模式安全箱区域基线修复与回归点 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加任务33双栏模式安全箱重叠修复记录 |
| 2026-04-16 | `Book/04-起装与装备/局内SearchPanel背包与安全箱自适应布局设计文档.md` | 修改 | 补充单栏堆叠 / 双栏基线的差异化布局策略与验收点 |
| 2026-04-16 | `Book/04-起装与装备/局内SearchPanel背包与安全箱自适应布局开发日志.md` | 修改 | 记录双栏模式安全箱与容器区重叠问题及修复决策 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/SearchPanelComponent.cs` | 修改 | 新增 `SecureAreaTopLeft` 缓存，保留安全箱原始区域基线 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/SearchPanelComponentSystem.cs` | 修改 | `BackpackInspect` 继续纵向堆叠，双栏模式改为按安全箱原始区域刷新，避免压到容器区 |

## 开发总结

- **实际完成**：`SearchPanel` 已在 `BackpackInspect / ContainerSearch / CorpseLoot` 三模式下接入同一套背包/安全箱块级布局刷新；背包区运行时补 `BagRoot`，安全箱继续复用 `SecureBagRoot`；其中 `BackpackInspect` 保持安全箱跟随背包向下堆叠，`ContainerSearch / CorpseLoot` 改为保留安全箱原始所属区域，避免和左侧容器区重叠；`dotnet build ET.sln` 已通过。
- **未完成**：尚未做 Unity / 游戏内人工回归，不正式处理分辨率断点适配。
- **与设计的偏差**：最终没有手改 `SearchPanel.prefab`，而是改成“运行时 `BagRoot` + 打开时恢复布局基线”的更小改动方案。
- **后续待办**：进 Unity 实机确认三模式下的视觉贴合、拖拽热区和点击交互是否都符合预期。
