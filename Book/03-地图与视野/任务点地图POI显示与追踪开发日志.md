# 任务点地图POI显示与追踪开发日志

**功能**：任务点地图POI显示与追踪
**关联设计文档**：[任务点地图POI显示与追踪设计文档.md](./任务点地图POI显示与追踪设计文档.md)
**关联任务**：M0.2-W3 #38
**开始时间**：2026-04-16
**开发者**：AI

## 开发进度

- [x] 步骤1：补齐计划文档、设计文档和开发日志
- [x] 步骤2：扩展任务点 POI 识别与运行时分支
- [x] 步骤3：扩展小地图/大地图任务点显示
- [x] 步骤4：补任务点默认常量与文本配置
- [x] 步骤5：执行验证并回写文档状态

## 决策记录

### 2026-04-16 - 任务点直接复用现有地图 POI 框架
- **背景**：地图 POI 首版已经接入撤离点、高级容器和 Boss 刷新点，当前又要求任务点也显示在地图上。
- **方案**：新增 `MapPoiType.MissionTask`，让任务点继续走现有 POI 运行时、小地图、大地图和追踪逻辑。
- **原因**：现有 POI 框架已经覆盖显示、点击 tips 和唯一追踪，继续复用能保证行为一致，也能避免新开第二套任务点地图系统。
- **替代方案**：单独做任务点地图标记逻辑。放弃原因是重复建设，后续维护会分叉。

### 2026-04-16 - 任务点识别按三个必填参数收口
- **背景**：任务点当前已经有一套稳定的 ECA 参数口径，且 `RogueMissionTaskFlow` 已把这组参数模板下发到点位 `Params`。
- **方案**：客户端只按 `mission_task_monster_unit_config_id / mission_task_monster_count / mission_task_reward_gold` 三个必填参数识别任务点。
- **原因**：这是任务点现有真实配置口径，不需要额外依赖 FlowGraph 扫描，也不需要新增场景点类型。
- **替代方案**：扫描 `FlowGraph` 是否包含 `StartTask`。放弃原因是对客户端导出结构耦合更重，参数口径已经足够稳定。

### 2026-04-16 - 任务点正式图标改成 `poi_mission_task`
- **背景**：首版任务点只留了独立 key 和橙色回退色；本轮用户已把正式 `MapIcon` 资源补齐。
- **方案**：把 `Minimap.PoiIcon.MissionTask` 切到正式 sprite 名 `poi_mission_task`，并同步在 `SDCMap` 的任务点实例上显式写入 `map_poi_icon`。
- **原因**：这样任务点不再依赖“sprite 缺失时回退到方块”这条兜底链路，局内效果直接和场景配置对齐。
- **替代方案**：继续只保留占位 key `mission_task`。放弃原因是正式图标已经到位，没有必要继续依赖占位链路。

## 问题日志

### 2026-04-16 - 任务点当前没有单独默认 icon 和文案常量
- **现象**：现有地图 POI 只为撤离点、高级容器、Boss 刷新点定义了默认 icon/tip key，任务点没有独立入口。
- **原因**：首版地图 POI 实现时还没有把任务点纳入范围。
- **解决**：本次补 `Minimap.PoiIcon.MissionTask`、`Minimap.PoiTipText.MissionTask` 和任务点颜色常量，并同步运行时配置。

### 2026-04-16 - `mission_task` 占位名不能继续沿用到正式资源
- **现象**：任务点首版默认 key 为 `mission_task`，但 atlas 中并没有稳定对应的正式 sprite；正式资源接入后还会和旧占位逻辑并存。
- **原因**：首版需求以功能闭环优先，只把“独立 key”留出来，没有把正式资源命名纳进设计。
- **解决**：本轮把正式切片命名为 `poi_mission_task`，并同步 `MinimapConst`、运行时 JSON 和 `SDCMap` 真实任务点配置。

### 2026-04-16 - 源表第一次回写时被残留 Excel 进程占成只读
- **现象**：`Text.xlsx` 和 `MinimapConstConfig.xlsx` 首次通过 Excel COM 打开时为 `ReadOnly=True`，保存后内容没有落盘。
- **原因**：前一轮读取脚本遗留了两个无窗口 `EXCEL` 进程，占住了工作簿。
- **解决**：只结束本轮脚本拉起的残留 `EXCEL` 进程，确认 `ReadOnly=False` 后重新回写，源表最终已成功落盘。

### 2026-04-16 - 整仓编译先后被测试控制台和 RunTimeLimit UI 半成品阻塞
- **现象**：`dotnet build ET.sln` 首先被后台 `dotnet .\\Bin\\ET.App.dll --SceneName=Test --Console=1` 锁住 `Bin\\ET.Core.dll`，清锁后又暴露 `RunTimeLimitRoot/RunTimeLimitText/BindRunTimeLimitUi` 缺口。
- **原因**：工作区里有遗留测试控制台进程，同时倒计时 UI 相关代码存在一组未收口的模型/调用缺口。
- **解决**：定位并结束遗留测试控制台父子进程，随后补齐 `MainPanelComponent` 缺字段，并把 `BindRunTimeLimitUi` 两处调用改成当前 partial 类可解析的静态方法调用；之后 `dotnet build ET.sln` 通过。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-16 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 登记 W3 任务 38 |
| 2026-04-16 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 追加版本变更记录 |
| 2026-04-16 | `Book/03-地图与视野/任务点地图POI显示与追踪设计文档.md` | 新增 | 创建设计文档 |
| 2026-04-16 | `Book/03-地图与视野/任务点地图POI显示与追踪开发日志.md` | 新增 | 创建开发日志 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Client/MapPoiRuntimeComponent.cs` | 修改 | 新增 `MapPoiType.MissionTask` |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Client/MapPoiRuntimeComponentSystem.cs` | 修改 | 增加任务点识别与默认 icon/tip 分支 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/MinimapPoiIconHelper.cs` | 修改 | 增加任务点默认 icon key 解析 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs` | 修改 | 增加任务点小地图颜色分支，并顺手修正既有 `BindRunTimeLimitUi` 调用方式 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Main/MapWorldPanelComponentSystem.cs` | 修改 | 增加任务点大地图颜色分支 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/Model/Share/MinimapConstKey.cs` | 修改 | 新增任务点默认图标/文案/颜色 key |
| 2026-04-16 | `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs` | 修改 | 补齐既有 `RunTimeLimit` UI 缺失字段以恢复整仓编译 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Luban/Config/Datas/Text.xlsx` | 修改 | 回写任务点默认文案 |
| 2026-04-16 | `Packages/cn.etetet.statesync/Luban/Config/Datas/MinimapConstConfig.xlsx` | 修改 | 回写任务点默认图标/文案/颜色 key |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/TextConfigCategory.json` | 修改 | 同步客户端任务点默认文案 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/TextConfigCategory.json` | 修改 | 同步 ClientServer 任务点默认文案 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/TextConfigCategory.json` | 修改 | 同步服务端任务点默认文案 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/et_minimapconstconfigcategory.json` | 修改 | 同步客户端任务点默认 icon/color/tip key |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/et_minimapconstconfigcategory.json` | 修改 | 同步 ClientServer 任务点默认 icon/color/tip key |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/et_minimapconstconfigcategory.json` | 修改 | 同步服务端任务点默认 icon/color/tip key |
| 2026-04-16 | `Assets/GameRes/YIUI/Common/Sprites/Atlas1/MapIcon.png.meta` | 修改 | 正式任务点图标切片命名为 `poi_mission_task` |
| 2026-04-16 | `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 修改 | 登记正式任务点 sprite 名 |
| 2026-04-16 | `Packages/cn.etetet.map/Bundles/ECA/SDCMap.txt` | 修改 | 给 `SDCMap` 里的任务点实例显式写入 `map_poi_icon = poi_mission_task` |

## 开发总结

- **实际完成**：已把局内任务点接入现有地图 POI 体系，任务点进图后可直接出现在小地图/大地图，并复用当前点击 tips、唯一追踪和边缘指引逻辑；同时补了任务点专用默认图标、文案和颜色配置。正式 `MapIcon` 到位后，当前默认 key 与 `SDCMap` 实例配置都已切到 `poi_mission_task`。
- **未完成**：尚未做局内人工回归，当前状态推进到 `未回归`。
- **与设计的偏差**：首版确实先使用了占位 key `mission_task`；但正式图标补齐后，当前实现已经改成 `poi_mission_task`，不再依赖占位 sprite 缺失回退。
- **后续待办**：你进局后重点回归三项：1）任务点是否开局直接显示；2）大地图点击后 tips 与追踪是否正确；3）任务完成后地图上的任务点保留/隐藏是否符合你的真实预期。
