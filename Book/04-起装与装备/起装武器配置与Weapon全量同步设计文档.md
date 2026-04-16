# 起装武器配置与Weapon全量同步设计文档

**创建时间**：2026-04-13
**最后更新**：2026-04-16
**状态**：已完成
**关联任务**：M0.2-W3 #19
**涉及包**：cn.etetet.item, cn.etetet.equipment, cn.etetet.statesync, cn.etetet.test

## 需求概述

当前局外起装商店显示的武器列表和实际战斗运行时武器配置不一致：

- `Packages/cn.etetet.statesync/Luban/Config/Datas/Weapon.xlsx` 当前已有 `50001~50011` 共 11 条武器配置。
- `Packages/cn.etetet.item/Luban/Config/Datas/Item.xlsx` 起装商店可见武器仍是旧口径，只保留了 `50009~50013` 共 5 条。
- 起装界面商店列表直接读取 `ItemConfig.LoadoutShopVisible`，不会自动从 `WeaponConfig` 补齐。
- 进图运行时又会直接把 `MainWeaponConfigId / SubWeaponConfigId` 当成 `WeaponConfigId` 使用。

结果就是：

1. 局外起装只能看到 5 把武器。
2. `Item.xlsx` 与 `Weapon.xlsx` 的武器 ID 段已经错位，后续很容易出现“界面可选”和“运行时有效武器”不一致。

本轮目标是以 `Weapon.xlsx` 当前全量为唯一准绳，同步起装商店武器配置，并补一层代码门禁，避免未来再次漂移。

## 技术方案

### 整体思路

采用“数据对齐 + 门禁收口”两段式方案：

1. 以 `Weapon.xlsx` 中现有 `50001~50011` 为基线，重建 `Item.xlsx` 中的起装武器条目。
2. 对武器槽位校验补齐“必须同时存在有效 `ItemConfig` 与 `WeaponConfig`”的检查。
3. 对局外起装商店列表补一层兜底：若物品被标为武器但没有对应 `WeaponConfig`，则不展示并记录告警。
4. 对商店图集索引补齐新增武器图标，避免数据同步后 UI 仍因 atlas 索引缺失显示不出来。
5. 正式武器图标不复用旧 `export (40)_*` sprite 名，而是统一改成 `weapon_{ItemId}`，避开同目录旧图集重名冲突。

这样可以同时解决：

- 当前用户可见问题：起装界面只显示 5 把武器。
- 潜在一致性问题：未来如果有人只改了 `Item.xlsx` 或只改了 `Weapon.xlsx`，系统会尽早暴露，而不是拖到进图后才发现。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.item/Luban/Config/Datas/Item.xlsx` | 修改 | 以 `Weapon.xlsx` 为准同步起装商店武器条目 |
| `Assets/GameRes/YIUI/Common/Sprites/Atlas1/新建项目 (1).png.meta` | 修改 | 把 11 个正式武器切片命名为 `weapon_50001~weapon_50011` |
| `Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutStateHelper.cs` | 修改 | 武器槽位校验同时校验 `ItemConfig` 与 `WeaponConfig` |
| `Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutOperationHelper.cs` | 修改 | 商店购买时拒绝缺失 `WeaponConfig` 的伪武器条目 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_LoadoutSourceUi.cs` | 修改 | 起装商店列表过滤无对应 `WeaponConfig` 的伪武器条目 |
| `Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 修改 | 补齐新增武器图标的 atlas 索引 |
| `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Loadout_WeaponConfigConsistency_Test.cs` | 新增 | 回归“起装武器清单和 `Weapon.xlsx` 一致” |
| `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_PlayerCorpseLoot_Insurance_Test.cs` | 修改 | 修复既有测试对 `ItemConfigCategory(JSONNode)` 新签名的兼容问题 |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/ItemConfigCategory.json` | 修改 | 在全量 Luban 导表被现有 Home 表阻塞时，手工回写服务端运行时物品配置 |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/ItemConfigCategory.json` | 修改 | 在全量 Luban 导表被现有 Home 表阻塞时，手工回写客户端运行时物品配置 |
| `Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/ItemConfigCategory.json` | 修改 | 在全量 Luban 导表被现有 Home 表阻塞时，手工回写共享运行时物品配置 |

### 数据结构

本轮不新增新的配置字段，也不扩 Proto。

继续复用现有 `ItemConfig` 字段：

- `LoadoutShopVisible`
- `LoadoutShopCategory`
- `LoadoutBuyPrice`
- `CanEquipMainWeapon`
- `CanEquipSubWeapon`
- `GridWidth`
- `GridHeight`
- `Name`
- `Desc`
- `Icon`

约束收口为：

- 只要某条 `ItemConfig` 被当成局外起装武器使用，就必须存在同 ID 的 `WeaponConfig`。
- `Weapon.xlsx` 中的武器若要成为正式起装武器，就必须在 `Item.xlsx` 中有同 ID 对应条目，并标记为可见武器。
- 正式武器图标统一使用 `weapon_{ItemId}` 命名，不再复用旧 `export (40)_*` 占位名。

## 实现步骤

1. 先把周计划、设计文档、开发日志补齐，明确本轮以 `Weapon.xlsx` 当前全量为准。
2. 修改 `Item.xlsx` 武器条目，移除不在 `Weapon.xlsx` 中的旧武器口径，补齐 `50001~50011`。
3. 修改服务端 `LoadoutStateHelper.ValidateWeaponSlot`，要求武器槽位必须对应真实 `WeaponConfig`。
4. 修改客户端起装商店列表生成逻辑，对“可装备主/副武器但无 `WeaponConfig`”的条目直接过滤并打日志。
5. 补一条配置一致性测试，保证后续导表或改表时能第一时间发现偏差。
6. 执行导表、`dotnet build ET.sln` 与定向测试，回写文档与周计划状态。

## 验收标准

- [ ] 起装商店中显示的正式武器数量与 `Weapon.xlsx` 当前全量一致。
- [ ] `Item.xlsx` 中不存在“起装可见武器但 `WeaponConfig` 缺失”的条目。
- [ ] 服务器在确认起装时，不再接受没有对应 `WeaponConfig` 的武器 ID。
- [ ] 相关导表、编译和定向测试完成，文档与周计划同步更新。

## 关联文档

- [局内局外联动起装技术设计文档.md](D:/05ET/MatchTest/ETGame/Book/04-起装与装备/局内局外联动起装技术设计文档.md)
- [M0.2-W3周计划.md](D:/05ET/MatchTest/ETGame/Book/08-版本计划/M0.2-W3周计划.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-13 | `Book/08-版本计划/M0.2-W3周计划.md`、`Book/04-起装与装备/起装武器配置与Weapon全量同步设计文档.md`、`Book/04-起装与装备/起装武器配置与Weapon全量同步开发日志.md` | 无偏差 |
| 步骤2 | 2026-04-13 | `Packages/cn.etetet.item/Luban/Config/Datas/Item.xlsx` | 无偏差 |
| 步骤3 | 2026-04-13 | `Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutStateHelper.cs`、`Packages/cn.etetet.equipment/Scripts/Hotfix/Server/LoadoutOperationHelper.cs` | 额外补了一层商店购买门禁，避免服务端继续接受伪武器 |
| 步骤4 | 2026-04-13 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_LoadoutSourceUi.cs`、`Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 额外补齐 atlas 索引，避免新增武器图标缺失 |
| 步骤5 | 2026-04-13 | `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Loadout_WeaponConfigConsistency_Test.cs`、`Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_PlayerCorpseLoot_Insurance_Test.cs` | 顺手修复了既有测试对 `ItemConfigCategory(JSONNode)` 新签名的兼容问题，恢复整仓可编译 |
| 步骤6 | 2026-04-13 | `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/ItemConfigCategory.json`、`Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/ItemConfigCategory.json`、`Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/ItemConfigCategory.json` | 由于全量 Luban 导表被现有 `HomeBuilding.xlsx` 阻塞，本轮手工回写运行时 JSON，保证武器同步即时生效 |
| 步骤7 | 2026-04-13 | `ET.sln`、`Logs/` | `dotnet build ET.sln` 通过，`Test_Loadout_WeaponConfigConsistency_Test` 与 `Equipment_LoadoutShopBuyToFixedSlot_Test` 通过；删除旧日志时有少量 Unity 占用中的 `shadercompiler/AssetImportWorker` 日志未能清除，不影响本轮验证 |
| 步骤8 | 2026-04-16 | `Assets/GameRes/YIUI/Common/Sprites/Atlas1/新建项目 (1).png.meta`、`Packages/cn.etetet.item/Luban/Config/Datas/Item.xlsx`、`Packages/cn.etetet.excel/Bundles/Luban/Config/Client/Json/ItemConfigCategory.json`、`Packages/cn.etetet.excel/Bundles/Luban/Config/ClientServer/Json/ItemConfigCategory.json`、`Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset` | 正式武器图标落地后，确认旧 `export (40).png` 仍占用 `export (40)_*` 名称，因此改为独立 `weapon_{ItemId}` 命名并同步配置 |
