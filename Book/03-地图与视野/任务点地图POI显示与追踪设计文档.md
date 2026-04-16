# 任务点地图POI显示与追踪设计文档

**创建时间**：2026-04-16
**最后更新**：2026-04-16（正式任务点图标已接入）
**状态**：已完成
**关联任务**：M0.2-W3 #38
**涉及包**：cn.etetet.statesync, cn.etetet.eca, cn.etetet.excel

## 需求概述

在现有地图 `POI` 体系基础上，把局内任务点也接进小地图/大地图显示与追踪。

本次需求明确约束如下：

- 任务点进图后直接显示，不需要先靠近，也不需要先接任务。
- 行为与现有 `Boss刷新点`、`撤离点` 一样，复用当前 POI 的显示、点击 tips 和唯一追踪逻辑。
- 任务点需要单独图标和单独文案。
- 当前图标先用默认占位，后续可由美术替换成正式资源。
- 不额外新建任务点 Excel/摆点体系，继续沿用现有任务点 ECA 配置。

## 技术方案

### 整体思路

不新做第二套“任务点地图标记”逻辑，而是直接扩展现有 `MapPoiRuntimeComponent` 与 `MapPoiType`：

1. 新增 `MapPoiType.MissionTask`。
2. 客户端读取地图 ECA 导出时，按任务点已有的三个必填参数识别任务点：
   - `mission_task_monster_unit_config_id`
   - `mission_task_monster_count`
   - `mission_task_reward_gold`
3. 命中识别后，任务点直接进入现有地图 POI 运行时，复用：
   - 小地图显示
   - 大地图显示
   - 大地图点击 tips
   - 当前唯一追踪目标
   - 小地图追踪边缘指引
4. 默认图标与默认文案继续走 `MinimapConstConfig` 和 `TextConfig`，不把“任务点”文本或 sprite 名硬编码到逻辑里。

这样改动面最小，也能保持任务点和现有 POI 共用同一套维护口径。

### 任务点识别口径

任务点本身已经有成熟配置方案，本次不再依赖额外点位类型或专用 FlowGraph 扫描，只按现有参数识别。

识别条件：

1. `ECAPointType == RangeTrigger`
2. `Params` 同时存在以下 3 个参数且值大于 0：
   - `mission_task_monster_unit_config_id`
   - `mission_task_monster_count`
   - `mission_task_reward_gold`

原因：

- 这 3 个参数已经是任务点运行时闭环的必填口径。
- 不依赖 `FlowGraph` 扫描可以降低客户端对导出结构的耦合。
- 不需要在 `cn.etetet.eca` 或地图资源层新增任务点专用类型。

### 图标与文案

新增一组任务点默认常量：

- `Minimap.PoiIcon.MissionTask`
- `Minimap.PoiTipText.MissionTask`

同时增加任务点占位颜色常量：

- `Minimap.MarkerColor.MissionTask`

本轮默认口径：

- 任务点图标 key 独立存在，当前正式 sprite 名为 `poi_mission_task`。
- 若后续还要继续换美术，只需要改配置或场景 `map_poi_icon`，不需要改逻辑。
- 任务点 tips 文案单独走 `TextConfig`，默认显示“任务点”。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 登记任务 38 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录 |
| `Book/03-地图与视野/任务点地图POI显示与追踪设计文档.md` | 新增 | 创建设计文档 |
| `Book/03-地图与视野/任务点地图POI显示与追踪开发日志.md` | 新增 | 创建开发日志 |
| `Packages/cn.etetet.statesync/Scripts/Model/Client/MapPoiRuntimeComponent.cs` | 修改 | 增加 `MissionTask` 枚举值 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/MapPoiRuntimeComponentSystem.cs` | 修改 | 增加任务点识别与默认图标/文案分支 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/MinimapPoiIconHelper.cs` | 修改 | 增加任务点默认 icon key |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 小地图任务点颜色分支 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | 修改 | 大地图任务点颜色分支 |
| `Packages/cn.etetet.statesync/Scripts/Model/Share/MinimapConstKey.cs` | 修改 | 增加任务点图标/文案/颜色 key |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/et_minimapconstconfigcategory.json` | 修改 | 同步任务点常量配置 |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/TextConfigCategory.json` | 修改 | 同步任务点默认文案 |
| `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx` | 修改 | 回写任务点默认图标/颜色/文案 key |
| `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx` | 修改 | 回写任务点默认文案 |

## 实现步骤

1. 补计划文档、设计文档和开发日志，登记任务口径。
2. 扩展 `MapPoiType` 与任务点识别逻辑。
3. 扩展小地图/大地图任务点图标与颜色分支。
4. 补任务点默认常量与文本配置。
5. 执行 `dotnet build ET.sln` 验证并回写文档状态。

## 验收标准

- [ ] 任务点进图后会直接显示在小地图。
- [ ] 任务点进图后会直接显示在大地图。
- [ ] 大地图点击任务点会弹出单独的任务点 tips 文案。
- [ ] 大地图点击任务点后，会将其设为当前唯一追踪目标。
- [ ] 小地图在任务点超出视野时，仍能显示追踪边缘指引。
- [ ] 当前改动不会影响现有撤离点、高级容器、Boss 刷新点 POI 行为。
- [ ] 任务点默认图标和文案均可通过配置替换，不依赖代码硬编码。

## 关联文档

- [地图POI显示与追踪设计文档.md](./地图POI显示与追踪设计文档.md)
- [局内任务点刷怪奖励设计文档.md](../05-肉鸽玩法/局内任务点刷怪奖励设计文档.md)
- [任务点地图POI显示与追踪开发日志.md](./任务点地图POI显示与追踪开发日志.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md`, `Book/08-版本计划/M0.2版本计划.md`, `Book/03-地图与视野/任务点地图POI显示与追踪设计文档.md`, `Book/03-地图与视野/任务点地图POI显示与追踪开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Client/MapPoiRuntimeComponent.cs`, `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/MapPoiRuntimeComponentSystem.cs` | 任务点识别最终按 3 个必填参数收口，没有额外扫描 FlowGraph 导出结构 |
| 步骤3 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/MinimapPoiIconHelper.cs`, `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs`, `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | UI 层完全复用现有 POI 图层与追踪逻辑，只补任务点分支 |
| 步骤4 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Share/MinimapConstKey.cs`, `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx`, `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx`, `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/TextConfigCategory.json`, `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/et_minimapconstconfigcategory.json` | 首版先补任务点默认 key；正式图标到位后已切到 `poi_mission_task` |
| 步骤5 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs`, `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs`, 本设计文档、开发日志 | 编译过程中顺手补齐既有 `RunTimeLimit` UI 模型/调用缺口后，`dotnet build ET.sln` 通过 |
| 步骤6 | 2026-04-16 | `Assets/GameRes/YIUI/Common/Sprites/Atlas1/MapIcon.png.meta`, `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset`, `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx`, `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/et_minimapconstconfigcategory.json`, `Packages/cn.etetet.map/Bundles/ECA/SDCMap.txt` | 正式任务点图标资源到位后，`Minimap.PoiIcon.MissionTask` 改为 `poi_mission_task`，并在 `SDCMap` 的任务点实例上显式写入同名 `map_poi_icon` |
