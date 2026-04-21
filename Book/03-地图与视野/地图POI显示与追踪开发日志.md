# 地图POI显示与追踪开发日志

**功能**：地图 POI 显示与追踪
**关联设计文档**：[地图POI显示与追踪设计文档.md](./地图POI显示与追踪设计文档.md)
**关联任务**：M0.2-W3 #32
**开始时间**：2026-04-16
**开发者**：AI

## 开发进度

- [x] 步骤1：补齐计划文档、设计文档和开发日志
- [x] 步骤2：扩展 ECA 参数与编辑器模板
- [x] 步骤3：实现客户端地图 POI 运行时与侧别过滤
- [x] 步骤4：实现大地图点击、tips 与唯一追踪
- [x] 步骤5：实现小地图 POI 与追踪目标边缘指引
- [x] 步骤6：补文本表、图标配置与运行时同步
- [x] 步骤7：执行验证并回写文档状态

## 决策记录

### 2026-04-16 - 地图 POI 单独建运行时，不混进单位 marker
- **背景**：当前小地图/大地图 marker 体系按 `unitId` 组织，只吃单位同步数据。
- **方案**：新增独立 `MapPoiRuntimeComponent`，和单位 marker 并行渲染。
- **原因**：本次需求包含“静态点位、点击 tips、唯一追踪、小地图边缘引导”，已经不是单位 marker 的职责范围。
- **替代方案**：继续扩展 `MinimapRuntimeComponent.Markers`。放弃原因是语义不对，后续接 `Boss刷新点` 和更多场景点位会持续变脏。

### 2026-04-16 - 点位侧新增 `side_id`，玩家侧使用独立 `SideComponent`
- **背景**：用户要求撤离点按“出生侧”显示，且明确 `CampId` 与 `sideId` 不是同一个概念。
- **方案**：点位配置新增 `side_id` 参数；玩家侧新增独立 `SideComponent`，并由 `SpawnPoint.side_id` 直接写入，再通过 `UnitInfo.SideId` 同步到客户端。
- **原因**：这样可以让多个队伍共享同一个 side，同时不影响现有 `CampId` 在战斗、索敌和友敌关系里的语义。
- **替代方案**：复用 `CampId`。放弃原因是会把“显示侧”和“战斗阵营”强绑在一起，后续 6 队 2/3 side 的配置会继续混乱。

### 2026-04-16 - 文本继续走现有 `Text.xlsx`
- **背景**：用户要求“先用语言表”，但项目里其实已经有正式 `Text.xlsx -> TextConfig` 链路。
- **方案**：直接在 `cn.etetet.statesync/Luban/Config/Datas/Text.xlsx` 增加通用 POI 文本。
- **原因**：避免再造一套文案系统，同时后续 `Boss刷新点`、任务点、空投点都可复用。
- **替代方案**：新建专门的地图文案表。放弃原因是和现有 `TextConfig` 职责重复。

### 2026-04-16 - 正式地图图标改成独立 `poi_*` 命名
- **背景**：用户已补正式 `MapIcon.png`，但原来临时占位用的 `green/gold/red/blue` 其实已经被旧 `export (4).png` 占用。
- **方案**：正式地图图标统一命名为 `poi_evacuation / poi_high_container / poi_boss_spawn / poi_tracked / poi_mission_task`，并同步改 `MinimapConstConfig` 与 `SDCMap.txt`。
- **原因**：这样既能接入正式美术，又不会和旧占位 sprite 重名，场景点位还能显式声明自己用哪张 icon。
- **替代方案**：继续沿用旧 `green/gold/red/blue` 配置名。放弃原因是这些旧名已经不是正式资源语义，后续很难维护。

### 2026-04-16 - 普通单位和 POI 尺寸彻底拆分
- **背景**：用户回归确认 POI 需要至少 `25` 的可视尺寸，但普通单位 marker 再大就会遮挡小地图阅读。
- **方案**：普通单位继续走 `Minimap.MarkerSize`，当前调到 `8`；POI 在小地图和大地图统一改读独立 `Minimap.PoiSize=25`。
- **原因**：两类标记的语义和可读性要求不同，继续共用一个尺寸常量会互相拉扯。
- **替代方案**：只调一个折中尺寸。放弃原因是会同时牺牲“普通单位不挡图”和“POI 可辨识”两边体验。

### 2026-04-17 - 小地图 POI 尺寸继续独立拆分
- **背景**：用户最新回归口径是“小地图的几个 POI 图标再放大 1 倍”，但当前 `Minimap.PoiSize=25` 仍由大小地图共用。
- **方案**：保留大地图 `Minimap.PoiSize=25`，新增小地图专用 `Minimap.CompactPoiSize=50`，只让 `MainPanel` 的小地图图标和追踪边缘指引改读新 key。
- **原因**：这样能精准放大小地图可读性，不会把展开地图上的 POI 一起放大到过满。
- **替代方案**：直接把 `Minimap.PoiSize` 改到 `50`。放弃原因是会同时影响大地图，超出本次回归修改范围。

### 2026-04-16 - 展开地图底图回到 prefab 预绑
- **背景**：用户回归发现展开地图会出现“只有图标层、没有底图背景”的表现。
- **方案**：`MapWorldPanel.prefab` 直接预绑和小地图同一个 `Minimap Render Texture`，同时补静态 `PoiLayer`；运行时只继续负责刷新 uv、迷雾和数据项。
- **原因**：底图和静态承载层都属于稳定 UI 资源，不应该继续依赖“打开时再从 MainPanel 拿引用”或“缺节点就运行时创建”的兜底链。
- **替代方案**：只在 `RefreshWorldMapTexture` 里继续做运行时兜底复制。放弃原因是资源层仍然不完整，用户已经明确 UI 类问题优先改 prefab。

### 2026-04-16 - side 语义下沉到 SpawnPoint 显式配置
- **背景**：用户确认实际玩法是“6 队里 3 队共用 1 个 side”，因此 `sideId` 必须直接由出生点配置传入。
- **方案**：给 `SpawnPoint` 开放 `side_id` 参数；玩家出生时把命中出生点的 `side_id` 写入独立 `SideComponent`，POI 再按 `side_id == SideComponent.SideId` 过滤。
- **原因**：场景配置直接表达“哪个出生组属于哪个 side”，并且和战斗阵营完全解耦。
- **替代方案**：继续沿用“按 team 排序推导 side”或“直接复用 CampId”。放弃原因是都无法稳定表达“多队同 side”的配置语义。

## 问题日志

### 2026-04-16 - 仓库存在大量并行脏改动
- **现象**：`git status` 显示当前工作树已有大量资源、Home、怪物、美术和第三方文件改动。
- **原因**：仓库当前存在并行工作，且用户已明确不要处理其他模块问题。
- **解决**：本次只改计划文档、地图/POI/ECA/文本相关文件，不回退、不整理其他模块变更。

### 2026-04-16 - Excel COM 写入字符串时出现参数类型歧义
- **现象**：通过 PowerShell 包装函数批量写 `xlsx` 时，Excel COM 在字符串赋值阶段报 `Unable to cast object of type 'System.String' to type 'System.Int32'`。
- **原因**：PowerShell 对 COM 调用的封装在该写法下出现参数绑定歧义，不是表数据本身有问题。
- **解决**：改为直接按固定单元格落点写入 `Text.xlsx` 和 `MinimapConstConfig.xlsx`，随后回读确认内容正确。

### 2026-04-16 - 旧 `green/gold/red/blue` 名称已被 atlas 旧资源占用
- **现象**：`YIUIAtlasData.asset` 里原本已有 `green/gold/red/blue`，继续让正式 `MapIcon` 使用这些名字会和 `export (4).png` 里的旧切片撞名。
- **原因**：POI 首版为了快速验证，直接复用了旧色块 sprite 名，后续正式美术到位时没有先清理旧 atlas 命名空间。
- **解决**：本轮把正式地图图标整体切到 `poi_*` 命名，并同步更新 `MinimapConstConfig`、运行时 JSON 和 `SDCMap` 的点位配置。

### 2026-04-16 - `MinimapConstConfig.xlsx` 的新增 POI 行列位发生漂移
- **现象**：`PoiIcon/PoiTipText/PoiSize` 新增行在源表里一度从 `A/C` 列起写，和表头 `B=Id, C=Key, D=FloatValue, E=StringValue` 不一致。
- **原因**：前一轮追加 POI 配置时，源表和运行时 JSON 没完全按同一列位写入。
- **解决**：本轮用 `ET.ExcelMcp` 把 `A26:E36` 整段对齐回标准列，同时把 `Minimap.MarkerSize` 更新为 `8`，避免后续正式导表时把 key/value 读歪。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 登记 W3 任务 32 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录 |
| 2026-04-16 | `Book/03-地图与视野/地图POI显示与追踪设计文档.md` | 新增 | 创建设计文档 |
| 2026-04-16 | `Book/03-地图与视野/地图POI显示与追踪开发日志.md` | 新增 | 创建开发日志 |
| 2026-04-16 | `Packages/cn.etetet.eca/Scripts/Model/Share/ECAPointParamKey.cs` | 修改 | 新增 `side_id`、`map_poi_*`、`container_profile`、`spawn_profile` 参数 key |
| 2026-04-16 | `Packages/cn.etetet.eca/Scripts/ModelView/Client/ECAPointMarker.cs` | 修改 | 扩展撤离点/容器/Boss 刷新点模板参数 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Server/SpawnPointManagerComponent.cs` | 修改 | 增加出生点 `side_id` 读取能力 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Share/SideComponent.cs` | 新增 | 新增独立玩家侧别组件 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Share/SideComponentSystem.cs` | 新增 | 新增侧别组件初始化逻辑 |
| 2026-04-16 | `Packages/cn.etetet.map/Proto/Map_C_11000.proto` | 修改 | `UnitInfo` 增加 `SideId` |
| 2026-04-16 | `Packages/cn.etetet.proto/CodeMode/Model/Client/Map_C_11000.cs` | 修改 | 同步客户端 `UnitInfo.SideId` |
| 2026-04-16 | `Packages/cn.etetet.proto/CodeMode/Model/ClientServer/Map_C_11000.cs` | 修改 | 同步 ClientServer `UnitInfo.SideId` |
| 2026-04-16 | `Packages/cn.etetet.proto/CodeMode/Model/Server/Map_C_11000.cs` | 修改 | 同步服务端 `UnitInfo.SideId` |
| 2026-04-16 | `Packages/cn.etetet.map/Scripts/Hotfix/Server/MapUnitEnterHelper.cs` | 修改 | 玩家出生时按出生点 `side_id` 设置独立 `SideComponent`，不再复用 `CampId` |
| 2026-04-16 | `Packages/cn.etetet.map/Scripts/Hotfix/Server/Unit/UnitHelper.cs` | 修改 | 下发 `UnitInfo.SideId` |
| 2026-04-16 | `Packages/cn.etetet.map/Scripts/Hotfix/Client/UnitFactory.cs` | 修改 | 客户端创建单位时挂载 `SideComponent` |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Client/MapPoiRuntimeComponent.cs` | 新增 | 新增地图 POI 运行时数据结构 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/MapPoiRuntimeComponentSystem.cs` | 新增 | 实现 ECA POI 加载、按玩家 `SideComponent` 过滤与选中校验 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/Scene/AfterCreateCurrentScene_AddMapPoiRuntime.cs` | 新增 | 当前场景自动挂载 POI 运行时 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/MinimapPoiIconHelper.cs` | 新增 | 新增 POI 默认图标与追踪图标解析 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 增加小地图 POI 图层、缓存和追踪图标字段 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 实现小地图 POI 显示、图标加载和追踪边缘指引 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MapWorldPanelComponent.cs` | 修改 | 增加大地图 POI 图层、按钮与图标缓存字段 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | 修改 | 实现大地图 POI 显示、点击 tips 与唯一追踪 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx` | 修改 | 新增 POI 通用文案 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx` | 修改 | 新增 POI 默认图标/文案配置 key |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/TextConfigCategory.json` | 修改 | 同步客户端运行时 POI 文案 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/TextConfigCategory.json` | 修改 | 同步 ClientServer 运行时 POI 文案 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/TextConfigCategory.json` | 修改 | 同步服务端运行时 POI 文案 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/et_minimapconstconfigcategory.json` | 修改 | 同步客户端 POI 图标/文案 key |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/et_minimapconstconfigcategory.json` | 修改 | 同步 ClientServer POI 图标/文案 key |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/et_minimapconstconfigcategory.json` | 修改 | 同步服务端 POI 图标/文案 key |
| 2026-04-16 | `Assets/GameRes/YIUI/Common/Sprites/Atlas1/MapIcon.png.meta` | 修改 | 正式地图图标切片命名为 `poi_*` |
| 2026-04-16 | `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 修改 | 登记正式地图图标 sprite 名 |
| 2026-04-16 | `Packages/cn.etetet.map/Bundles/ECA/SDCMap.txt` | 修改 | 给真实撤离点/高级容器/Boss 点位显式写入 `map_poi_icon` |
| 2026-04-16 | `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx` | 修改 | 把 `Minimap.MarkerSize` 下调到 `8`，并对齐 POI 新增行列位 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/et_minimapconstconfigcategory.json` | 修改 | 同步普通单位 marker 尺寸为 `8` |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/et_minimapconstconfigcategory.json` | 修改 | 同步普通单位 marker 尺寸为 `8` |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/et_minimapconstconfigcategory.json` | 修改 | 同步普通单位 marker 尺寸为 `8` |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | 修改 | 大地图 POI 改读独立 `PoiSize`，不再跟普通单位 marker 共用尺寸 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Main/Prefabs/Map/MapWorldPanel.prefab` | 修改 | 预绑小地图同款 RenderTexture，静态补 `PoiLayer` 与箭头 sprite |
| 2026-04-17 | `Packages/cn.etetet.statesync/Scripts/Model/Share/MinimapConstKey.cs` | 修改 | 新增小地图专用 `Minimap.CompactPoiSize` 配置 key |
| 2026-04-17 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 小地图 POI 与追踪边缘图标改读 `Minimap.CompactPoiSize`，默认回退 `PoiSize` |
| 2026-04-17 | `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx` | 修改 | 源表新增 `Minimap.CompactPoiSize=50`，保持后续导表不丢配置 |
| 2026-04-17 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/et_minimapconstconfigcategory.json` | 修改 | 同步客户端小地图 POI 独立尺寸为 `50` |
| 2026-04-17 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/et_minimapconstconfigcategory.json` | 修改 | 同步 ClientServer 小地图 POI 独立尺寸为 `50` |
| 2026-04-17 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/et_minimapconstconfigcategory.json` | 修改 | 同步服务端小地图 POI 独立尺寸为 `50` |
| 2026-04-17 | `Book/03-地图与视野/地图POI显示与追踪设计文档.md` | 修改 | 回写“小地图 POI 再放大 1 倍”的尺寸拆分方案 |
| 2026-04-17 | `Book/03-地图与视野/地图POI显示与追踪开发日志.md` | 修改 | 记录本轮 POI 尺寸回归调整 |

## 开发总结

- **实际完成**：已完成 POI 运行时、ECA 参数扩展、大地图点击 tips 与唯一追踪、小地图同步显示与边缘追踪指引、`Text/MinimapConst` 配置及三端运行时 json 同步；并补上 `SpawnPoint.side_id -> SideComponent -> UnitInfo.SideId -> 客户端 POI 过滤` 独立链路。本轮又接入正式 `MapIcon` 资源，默认 key 和 `SDCMap` 真实点位 icon 都切到 `poi_*` 语义名；随后继续按用户回归把普通单位 marker 下调到 `8`、大地图 POI 保持 `25`，再把小地图 POI 与追踪边缘图标继续拆到 `Minimap.CompactPoiSize=50`，并把展开地图底图与静态 `PoiLayer` 迁回 `MapWorldPanel.prefab`。
- **未完成**：未做局内人工验收；Boss 刷新点只完成通用结构预留，尚未由场景配置实际接入。
- **与设计的偏差**：首版确实先复用了 `green/gold/red/blue`；但正式图标到位后，当前实现已经切回独立 `poi_*` 命名，不再继续依赖旧色块 sprite。
- **后续待办**：你在场景里补出生点 `side_id` 和撤离点/高级容器/Boss 刷新点参数后，重点回归“大地图点击 tips、小地图边缘追踪、出生侧撤离点过滤”三项。
