# 起装武器配置与Weapon全量同步开发日志

**功能**：起装武器配置与 Weapon 全量同步
**关联设计文档**：[起装武器配置与Weapon全量同步设计文档.md](D:/05ET/MatchTest/ETGame/Book/04-起装与装备/起装武器配置与Weapon全量同步设计文档.md)
**关联任务**：M0.2-W3 #19
**开始时间**：2026-04-13
**开发者**：AI

## 开发进度

- [x] 步骤1：补周计划、专项设计文档和开发日志
- [x] 步骤2：同步 `Item.xlsx` 起装武器条目到 `Weapon.xlsx` 当前全量
- [x] 步骤3：补服务端与客户端的一致性门禁
- [x] 步骤4：执行导表、编译与定向回归

## 决策记录

### 2026-04-13 - 以 Weapon.xlsx 为唯一武器口径
- **背景**：现场反馈“应该配了 9 个武器，起装只能看到 5 个”，代码扫描后发现局外起装实际读 `ItemConfig`，运行时战斗实际读 `WeaponConfig`。
- **方案**：本轮不再继续维护两套并行武器口径，而是明确以 `Weapon.xlsx` 当前全量作为正式武器清单，再把 `Item.xlsx` 同步成展示层与购买层的镜像配置。
- **原因**：运行时真实武器能力只存在于 `WeaponConfig`；若继续以 `Item.xlsx` 为主，最终仍会出现“界面显示”和“进图战斗”两张表错位。
- **替代方案**：只改 UI 过滤或只补 `Item.xlsx` 中缺失条目。放弃原因是这样仍无法阻止后续再次出现 `ItemConfig` / `WeaponConfig` 漂移。

### 2026-04-13 - 同时补商店购买门禁与 atlas 索引
- **背景**：仅改 `ValidateWeaponSlot` 只能挡住“确认起装”阶段，仍挡不住局外商店把伪武器卖进背包；同时新增武器图标资源虽然已在大图中存在，但 `YIUIAtlasData.asset` 只登记了 `export (40)_0~4`。
- **方案**：在 `LoadoutOperationHelper.TryResolveShopPurchaseItem` 同步补武器配置校验；在 `YIUIAtlasData.asset` 一次性补齐 `export (40)_5~10`。
- **原因**：这样可以把“买得到但不能确认”这种半损坏状态直接堵住，也能保证新增武器在 UI 上有图标可用。
- **替代方案**：只保留确认起装时的兜底，或把图标问题留给后续美术资源整理。放弃原因是用户当前目标就是“按 Weapon.xlsx 当前全量同步”，半同步会继续暴露脏状态。

### 2026-04-13 - 导表被 HomeBuilding 阻塞时先回写运行时 JSON
- **背景**：`dotnet .\\Bin\\ET.ExcelExporter.dll Luban` 在 `Config` 阶段被现有 `Packages/cn.etetet.home/Luban/Config/Datas/HomeBuilding.xlsx` 的 `BuildingType=主城` 解析错误挡住，导致 `Item.xlsx` 的新武器数据没有写进 `ItemConfigCategory.json`。
- **方案**：不改用户当前 Home 任务数据，直接按本轮已同步的 `Item.xlsx` 内容回写三份运行时 `ItemConfigCategory.json`，同时保留源表修改。
- **原因**：用户当前要解决的是“游戏里起装只看到 5 把武器”，手工回写运行时 JSON 可以在不越界修改 Home 任务数据的前提下，让本轮武器同步立即生效。
- **替代方案**：继续修改 `HomeBuilding.xlsx` 直到全量 Luban 导表通过。放弃原因是这属于另一条正在进行的 Home 数据线，当前缺少足够上下文，不适合在本任务里冒进改表。

### 2026-04-16 - 正式武器图标改用 `weapon_{ItemId}` 命名
- **背景**：用户提供了 11 个正式武器图标资源 `新建项目 (1).png`，但同目录旧 `export (40).png` 仍然占用了 `export (40)_0~17` 这组 sprite 名。
- **方案**：不复用旧名，直接把正式切片命名为 `weapon_50001~weapon_50011`，并同步回写 `Item.xlsx` 与客户端运行时 `ItemConfigCategory.json`。
- **原因**：这样可以避免 atlas 同名冲突，也让图标命名直接和正式武器 ID 对齐，后续继续扩武器时更稳定。
- **替代方案**：把新图强行改成 `export (40)_*`，或者删掉旧 `export (40).png`。放弃原因是前者一定撞名，后者会越界影响现有旧资源链路。

## 问题日志

### 2026-04-13 - 起装武器条目和 WeaponConfig ID 段错位
- **现象**：`Weapon.xlsx` 当前已有 `50001~50011` 共 11 把武器，但 `Item.xlsx` 起装武器条目还是旧的 `50009~50013` 共 5 条。
- **原因**：武器运行时配置扩容后，没有同步更新起装商店使用的 `Item.xlsx`，同时代码侧也没有强制要求武器槽位必须存在对应 `WeaponConfig`。
- **解决**：本轮同时处理源表同步和代码门禁，确保起装商店、确认起装、进图运行时都使用同一套武器 ID。

### 2026-04-13 - 新增武器图标 atlas 索引缺失
- **现象**：`export (40).png.meta` 中已有 `export (40)_5~10`，但 `YIUIAtlasData.asset` 只登记了 `export (40)_0~4`。
- **原因**：图集原始资源扩充后，没有同步把 sprite 名登记到 YIUI 的 atlas 数据资产里。
- **解决**：本轮直接把 `export (40)_5~10` 补进 `YIUIAtlasData.asset`，让新增武器条目能正常走现有图集加载链路。

### 2026-04-13 - 全量导表被 HomeBuilding.xlsx 现有错误阻塞
- **现象**：Luban `Config` 导出阶段报 `HomeBuilding.xlsx` 的 `BuildingType` 字段解析失败，`ItemConfigCategory.json` 仍停留在旧的 `50009~50013` 武器口径。
- **原因**：`HomeBuilding.xlsx` 当前数据与其 schema 不匹配，属于仓库里另一条 Home 数据改动线。
- **解决**：本轮不动 Home 表，先手工回写 `ItemConfigCategory.json` 到 `50001~50011` 新口径，再继续编译和跑定向测试。

### 2026-04-16 - 正式武器图标不能复用旧 `export (40)_*`
- **现象**：`Assets/GameRes/YIUI/Common/Sprites/Atlas1/export (40).png.meta` 里已经存在 `export (40)_0~17`，如果把正式图切片直接改成旧名，Unity atlas 会出现 sprite 名冲突。
- **原因**：旧武器占位图集文件仍在工程内，且 YIUI atlas 也已经登记了这组旧 sprite 名。
- **解决**：本轮把正式武器图标独立命名为 `weapon_50001~weapon_50011`，同步 `Item.xlsx`、客户端运行时 JSON 和 `YIUIAtlasData.asset` 到新命名。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-04-13 | `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 新增 W3 #19 并登记任务现状 |
| 2026-04-13 | `Book/04-起装与装备/起装武器配置与Weapon全量同步设计文档.md` | 新增 | 补专项设计文档 |
| 2026-04-13 | `Book/04-起装与装备/起装武器配置与Weapon全量同步开发日志.md` | 新增 | 创建开发过程记录 |
| 2026-04-13 | `Packages/cn.etetet.item/Luban/Config/Datas/Item.xlsx` | 修改 | 把起装武器条目同步到 `Weapon.xlsx` 当前全量 `50001~50011` |
| 2026-04-13 | `Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutStateHelper.cs` | 修改 | 武器槽位校验改为必须同时存在可装备武器的 `ItemConfig` 与同 ID `WeaponConfig` |
| 2026-04-13 | `Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutOperationHelper.cs` | 修改 | 商店购买链路补武器配置门禁，拒绝伪武器条目 |
| 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_LoadoutSourceUi.cs` | 修改 | 起装商店 UI 过滤无 `WeaponConfig` 的武器条目并打告警 |
| 2026-04-13 | `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 修改 | 补齐 `export (40)_5~10` 图标索引 |
| 2026-04-13 | `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Loadout_WeaponConfigConsistency_Test.cs` | 新增 | 新增起装武器与 `WeaponConfig` 一致性测试 |
| 2026-04-13 | `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_PlayerCorpseLoot_Insurance_Test.cs` | 修改 | 兼容 `ItemConfigCategory` 改为 `JSONNode` 构造后的既有测试，恢复整仓编译 |
| 2026-04-13 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/ItemConfigCategory.json` | 修改 | 手工回写服务端运行时物品配置，补齐 `50001~50011` 武器 |
| 2026-04-13 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/ItemConfigCategory.json` | 修改 | 手工回写客户端运行时物品配置，补齐 `50001~50011` 武器与图标 |
| 2026-04-13 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/ItemConfigCategory.json` | 修改 | 手工回写共享运行时物品配置，补齐 `50001~50011` 武器与图标 |
| 2026-04-16 | `Assets/GameRes/YIUI/Common/Sprites/Atlas1/新建项目 (1).png.meta` | 修改 | 正式武器图标切片改名为 `weapon_50001~weapon_50011` |
| 2026-04-16 | `Packages/cn.etetet.item/Luban/Config/Datas/Item.xlsx` | 修改 | 起装武器 `Icon` 列切换到 `weapon_50001~weapon_50011` |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/ItemConfigCategory.json` | 修改 | 同步客户端运行时武器 icon 到正式图标名 |
| 2026-04-16 | `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/ItemConfigCategory.json` | 修改 | 同步 ClientServer 运行时武器 icon 到正式图标名 |
| 2026-04-16 | `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 修改 | 补登记 `weapon_50001~weapon_50011` 正式武器 sprite 名 |

## 开发总结

- **实际完成**：已把 `Item.xlsx`、运行时 `ItemConfigCategory.json`、服务端校验、商店购买门禁、客户端商店过滤、YIUI atlas 索引和自动化测试全部同步到 `Weapon.xlsx` 当前全量 `50001~50011`；本轮又把正式武器图标切到独立 `weapon_{ItemId}` 命名，避免和旧 `export (40).png` 重名，并同步回写源表和客户端运行时配置。
- **未完成**：没有在本任务里修复 `HomeBuilding.xlsx` 的 schema 解析问题，所以全量 Luban `Config` 导表仍不是干净通过。
- **与设计的偏差**：相比原设计，额外手工回写了三份运行时 `ItemConfigCategory.json`，因为全量导表被 Home 侧现有错误阻塞，否则源表改动不会即时生效。
- **后续待办**：后续 Home 数据线修好 `HomeBuilding.xlsx` 后，需要重新跑一次正式 Luban 导表，把本轮手工回写的 JSON 重新纳入自动生成链路。
