# 地图POI显示与追踪设计文档

**创建时间**：2026-04-16
**最后更新**：2026-04-16（正式 MapIcon 资源已接入）
**状态**：已完成
**关联任务**：M0.2-W3 #32
**涉及包**：cn.etetet.statesync, cn.etetet.map, cn.etetet.eca, cn.etetet.excel

## 需求概述

在现有小地图/大地图能力上增加一套通用地图 `POI` 系统，首期接入两类点位：

1. 我方可用撤离点
2. 高级容器

本次需求约束如下：

- 小地图和大地图都要显示 POI。
- 小地图不可点击，只负责显示和追踪目标边缘指引。
- 大地图可点击图标；点击后弹 tips，并把该点设为当前唯一追踪目标。
- 撤离点只显示“我方出生侧可用”的点。
- 高级容器全图可见。
- tips 文案不能硬编码，走通用文本表。
- 后续还会继续接入 `Boss刷新点`，因此本次要做成通用 POI 框架，而不是只为两类点位写死逻辑。

## 技术方案

### 整体思路

不把撤离点/容器硬塞进当前 `MinimapRuntimeComponent.Markers` 单位标记体系，而是新增一套与单位 marker 并行的地图 `POI` 运行时。

原因：

1. 当前小地图/大地图 marker 运行时是按 `unitId` 组织的，数据来源也只来自单位同步，不适合表达静态场景点位。
2. 大地图现有 marker 默认不可点击，后续继续堆“撤离点/高级容器/Boss刷新点/任务点”会让语义越来越混乱。
3. 这次需求已经包含“点位筛选、点击 tips、唯一追踪、小地图边缘引导”，本质上是一套独立 POI 机制。

因此本次按以下分层实现：

1. `ECAConfig + Params` 继续作为地图 POI 的场景配置源。
2. `statesync` 新增 `MapPoiRuntimeComponent`，在客户端从 `Packages/cn.etetet.map/Bundles/ECA/{mapName}.txt` 加载并缓存 POI。
3. `MainPanel` 新增 `MinimapPoiLayer`，负责小地图静态 POI 和追踪目标边缘指引。
4. `MapWorldPanel` 新增 `PoiLayer`，负责大地图静态 POI 显示与点击。
5. 点击大地图 POI 后，直接复用 `TipsHelper.OpenSync<TipsTextViewComponent>` 弹文本 tips，并把该点记为当前唯一追踪目标。

### 出生侧/可见侧语义

玩家侧不复用 `CampComponent.CampId` 作为 side 语义，而是新增独立 `SideComponent.SideId`。

本轮进一步收口为“出生点显式配置 side”：

- `SpawnPoint` 继续使用 `team_id` 表达出生组/队伍组。
- 同时允许 `SpawnPoint` 额外配置 `side_id`，表示该出生组所属的显示侧。
- 玩家出生时，直接把命中出生点的 `side_id` 写入玩家 `SideComponent`，并通过 `UnitInfo.SideId` 同步到客户端。
- `CampId` 继续保留给战斗阵营、索敌、友伤判定等既有语义，和 `side_id` 不建立隐式映射关系。

这样就能支持“3 队共用 1 个 side”这类需求，同时不污染现有战斗阵营语义。

首期规则：

- `EvacuationPoint`：只有 `side_id == mySideId` 时显示；`side_id <= 0` 视为全侧可见。
- `Container` 且 `container_profile == Epic`：全图可见，不做侧别过滤。

### 配置方式

继续复用 `ECAPointMarker`，不新建独立 Excel 摆点表。

#### 撤离点

- `Type = EvacuationPoint`
- 推荐参数：
  - `side_id`
  - `map_poi_visible`
  - `map_poi_show_minimap`
  - `map_poi_show_worldmap`
  - `map_poi_icon`
  - `map_poi_tip_text_id`

#### 高级容器

- `Type = Container`
- 参数：
  - `container_profile = Epic`
  - `map_poi_visible`
  - `map_poi_show_minimap`
  - `map_poi_show_worldmap`
  - `map_poi_icon`
  - `map_poi_tip_text_id`

#### 后续 Boss 刷新点

- `Type = MonsterSpawnPoint`
- 参数：
  - `spawn_profile = Boss`
  - 复用同一组 `map_poi_*`

#### 出生点

- `Type = SpawnPoint`
- 参数：
  - `team_id`
  - `side_id`

其中：

- `team_id` 决定出生组。
- `side_id` 决定该出生组最终写入玩家 `SideComponent.SideId` 的值。
- 因此可以实现“多个队伍共享同一个 side”，例如 6 队映射到 2 个或 3 个 side。

### 文本表方案

项目里已有正式 `Text.xlsx -> TextConfig -> TextConstDefine` 链路，因此本次不新起第二套语言表结构，直接在 `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx` 增加通用 POI 文案：

- 我方撤离点
- 高级容器
- 后续 Boss 刷新点预留

如果全量导表仍被外部 Excel 阻塞，本次允许采用：

1. 先回写 `Text.xlsx`
2. 再手工同步运行时 `TextConfigCategory.json`

以保证本功能即时可用；待外部阻塞解除后再补正式导表闭环。

### 图标方案

POI 图标不写死到代码，统一通过 `MinimapConstConfig` 配置 sprite 名：

- 先扩一组 `Minimap.PoiIcon.*` 常量 key
- 运行时按 `map_poi_icon` 优先取显式配置
- 未配时按 `PoiType` 使用默认 key

这样后续新增 Boss 点/空投点时无需修改 UI 逻辑。

本轮已接正式 `MapIcon.png` 切片，并统一使用独立语义名，避免和旧 `export (4).png` 的 `green/gold/red/blue` 占位 sprite 重名：

- `Minimap.PoiIcon.Evacuation = poi_evacuation`
- `Minimap.PoiIcon.HighContainer = poi_high_container`
- `Minimap.PoiIcon.BossSpawn = poi_boss_spawn`
- `Minimap.PoiIcon.Tracked = poi_tracked`

同时在 `SDCMap.txt` 的真实 POI 点位上显式写入同名 `map_poi_icon`，减少场景资源和全局默认配置漂移。

### 小地图显示与追踪

小地图分两层：

1. 普通 `POI` 图标
2. 当前追踪目标的边缘引导图标

规则：

- 普通 POI 使用真实坐标投影到当前小地图视野。
- 若 POI 在当前小地图视野外，则普通图标不显示。
- 若该 POI 是当前追踪目标，则无论是否在视野外，都要显示边缘引导图标。
- 边缘引导图标不代表真实位置，只表示目标方向。

追踪目标为全局唯一：

- 大地图点击新目标时覆盖旧目标。
- 关闭大地图后仍保留追踪。
- 目标从配置里移除或当前地图不再存在时，自动清空追踪。

### 大地图交互

大地图 `POI` 图标改为可点击：

1. 点击后弹 `TipsTextViewComponent`
2. 同时把该 `POI` 设为当前唯一追踪目标

首期不做大地图路径线、不做额外确认弹窗、不做小地图点击。

## 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 登记任务 32 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录 |
| `Book/03-地图与视野/地图POI显示与追踪开发日志.md` | 新增 | 开发日志 |
| `Assets/GameRes/YIUI/Common/Sprites/Atlas1/MapIcon.png.meta` | 修改 | 正式地图图标切片命名为 `poi_*` |
| `Packages/cn.etetet.map/Bundles/ECA/SDCMap.txt` | 修改 | 给实际撤离点/高级容器/Boss POI 显式写入 `map_poi_icon` |
| `Packages/cn.etetet.eca/Scripts/Model/Share/ECAPointParamKey.cs` | 修改 | 增加 `side_id` 与 `map_poi_*` 参数 key |
| `Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs` | 修改 | 扩展参数模板与编辑器默认项 |
| `Packages/cn.etetet.statesync/Scripts/Model/Client/MapPoiRuntimeComponent.cs` | 新增 | 地图 POI 运行时数据 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/MapPoiRuntimeComponentSystem.cs` | 新增 | 客户端 POI 配置加载与筛选逻辑 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/MinimapPoiIconHelper.cs` | 新增 | POI 图标解析 |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 小地图 POI 图层、追踪图标缓存 |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MapWorldPanelComponent.cs` | 修改 | 大地图 POI 图层与点击缓存 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 小地图 POI 绘制与追踪边缘指引 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | 修改 | 大地图 POI 绘制、点击与追踪设置 |
| `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx` | 修改 | 新增通用文本 |
| `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx` | 修改 | 新增 POI 图标常量 |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/TextConfigCategory.json` | 可能修改 | 若导表阻塞则手工同步运行时文本 |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/et_minimapconstconfigcategory.json` | 可能修改 | 若导表阻塞则手工同步运行时图标常量 |

## Entity/Component 设计

### MapPoiRuntimeComponent

挂在客户端当前地图 `Scene` 上。

字段规划：

- `MapName`
- `LoadedMapName`
- `Dictionary<string, MapPoiRuntimeData> Pois`
- `string SelectedPoiId`

### MapPoiRuntimeData

实际字段：

- `PoiId`
- `PoiType`
- `PointType`
- `Position`
- `SideId`
- `ShowMinimap`
- `ShowWorldmap`
- `TipTextId`
- `IconName`
- `Visible`

### PoiType

首期枚举：

- `Evacuation`
- `HighContainer`
- `BossSpawn`

## 数据结构

### 新增 ECA 参数 key

- `side_id`
- `map_poi_visible`
- `map_poi_show_minimap`
- `map_poi_show_worldmap`
- `map_poi_icon`
- `map_poi_tip_text_id`
- `container_profile`
- `spawn_profile`

### 文本表

首期实际新增：

- `MapPoi_Evacuation_Self`
- `MapPoi_HighContainer`
- `MapPoi_BossSpawn`

### MinimapConst

首期至少新增：

- `Minimap.PoiIcon.Evacuation`
- `Minimap.PoiIcon.HighContainer`
- `Minimap.PoiIcon.BossSpawn`
- `Minimap.PoiIcon.Tracked`

## 实现步骤

1. 补计划文档、设计文档和开发日志，登记任务口径。
2. 扩展 `ECAPointParamKey` / `ECAPointMarker`，定义 POI 与 `side_id` 参数。
3. 实现客户端 `MapPoiRuntimeComponent`，从 ECA 导出文件读取 POI 并按玩家 `SideId` 过滤我方撤离点。
4. 实现大地图 POI 图层、点击 tips 和唯一追踪目标切换。
5. 实现小地图 POI 图层与追踪目标边缘指引。
6. 补 `Text.xlsx`、`MinimapConstConfig.xlsx` 与必要的运行时配置同步。
7. 执行本任务范围验证并回写文档状态。

## 验收标准

- [ ] 小地图能显示我方可用撤离点和高级容器。
- [ ] 大地图能显示我方可用撤离点和高级容器。
- [ ] 大地图点击撤离点会弹出“我方撤离点”tips。
- [ ] 大地图点击高级容器会弹出“高级容器”tips。
- [ ] 大地图点击某个 POI 后，会将其设为当前唯一追踪目标。
- [ ] 小地图在追踪目标超出视野时，仍能显示边缘指引。
- [ ] 关闭大地图后，追踪目标不会丢失。
- [ ] 当前实现不会影响现有单位 marker 的显示和颜色逻辑。
- [ ] 后续 `Boss刷新点` 能复用同一套 `POI` 结构扩展。

## 关联文档

- [M0.2-W3周计划.md](../08-版本计划/M0.2-W3周计划.md)
- [地图系统说明.md](./9.6地图系统说明.md)
- [ECA框架使用指南-场景配置.md](../07-ECA框架/ECA2/ECA框架使用指南-场景配置.md)
- [地图POI显示与追踪开发日志.md](./地图POI显示与追踪开发日志.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md`, `Book/08-版本计划/M0.2版本计划.md`, `Book/03-地图与视野/地图POI显示与追踪设计文档.md`, `Book/03-地图与视野/地图POI显示与追踪开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-16 | `Packages/cn.etetet.eca/Scripts/Model/Share/ECAPointParamKey.cs`, `Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs` | 无偏差 |
| 步骤3 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Client/MapPoiRuntimeComponent.cs`, `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/MapPoiRuntimeComponentSystem.cs`, `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/Scene/AfterCreateCurrentScene_AddMapPoiRuntime.cs`, `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/MinimapPoiIconHelper.cs` | 无偏差 |
| 步骤4 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MapWorldPanelComponent.cs`, `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | 无偏差 |
| 步骤5 | 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs`, `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 无偏差 |
| 步骤6 | 2026-04-16 | `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx`, `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx`, `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/TextConfigCategory.json`, `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/et_minimapconstconfigcategory.json` | 先完成 POI 文案与默认 icon key 接线，后续再接正式图标资源 |
| 步骤7 | 2026-04-16 | `Book/03-地图与视野/地图POI显示与追踪设计文档.md`, `Book/03-地图与视野/地图POI显示与追踪开发日志.md`, `Book/08-版本计划/M0.2-W3周计划.md` | 使用 `dotnet build ET.sln` 完成编译验证，通过 |
| 步骤8 | 2026-04-16 | `Assets/GameRes/YIUI/Common/Sprites/Atlas1/MapIcon.png.meta`, `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset`, `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx`, `Packages/cn.etetet.excel/Bundles/Luban/Config/*/Json/et_minimapconstconfigcategory.json`, `Packages/cn.etetet.map/Bundles/ECA/SDCMap.txt` | 正式 MapIcon 资源到位后，切换默认 key 到 `poi_*` 语义名，并把 `SDCMap` 真实点位显式写成同名 icon 配置 |
