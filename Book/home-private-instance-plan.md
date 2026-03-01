# Home 私有副本改造方案（方案2）

## 1. 背景与目标

当前登录后进入 `Home` 的流程是联网地图流程，且 `Home` 配置为普通地图（`CopyType=Normal`），会导致多玩家进入同类副本并通过 AOI 同步可见。

本方案目标：

1. 保持服务端权威（防作弊与数据一致性不下降）。
2. 将 `Home` 改为玩家私有副本（每个玩家一个 Home，互相不可见）。
3. 为后续“按玩家数据加载建筑”提供可扩展基础。

---

## 2. 现状证据（代码定位）

1. 登录完成后触发进图：
   - `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LoginFinish_CreateLobbyUI.cs:9`
2. 客户端发起 `C2G_EnterMap`：
   - `Packages/cn.etetet.map/Scripts/Hotfix/Client/EnterMapHelper.cs:13`
3. 服务端固定传送到 `Home`，且 mapId=0：
   - `Packages/cn.etetet.map/Scripts/Hotfix/Server/C2G_EnterMapHandler.cs:45`
4. `Home` 当前配置（Normal + 推荐人数 50）：
   - `Packages/cn.etetet.excel/Bundles/Luban/Config/Server/Json/MapConfigCategory.json:44`
5. Map 副本分配会优先复用同图运行副本：
   - `Packages/cn.etetet.mapmanager/Scripts/Hotfix/Server/MapManagerComponentSystem.cs:96`
6. AOI 进入视野会下发单位创建，导致彼此可见：
   - `Packages/cn.etetet.map/Scripts/Hotfix/Server/Unit/UnitEnterSightRange_NotifyClient.cs:21`

---

## 3. 改造范围与原则

范围：仅改“Home 副本分配与生命周期”相关逻辑，不改现有登录协议主流程。

原则：

1. 不引入 hard code 破坏现有扩展点。
2. Entity 只放数据，逻辑放 System/Helper（遵循 et-arch）。
3. 先完成一期“私有副本隔离”，再做二期“建筑数据加载”。

---

## 4. 一期方案（先解决互相可见）

### 4.1 进入 Home 时绑定玩家私有 mapId

改造点：

- 文件：`Packages/cn.etetet.map/Scripts/Hotfix/Server/C2G_EnterMapHandler.cs`
- 现状：`TransferAtFrameFinish(player, unit, "Home", 0)`
- 目标：改为 `TransferAtFrameFinish(player, unit, "Home", player.Id)`
- 同时将该方法签名中的 `mapId` 从 `int` 提升为 `long`，与 `TransferHelper` 保持一致。

效果：

- 每个玩家固定进入自己的 Home 副本（副本ID=玩家ID）。

### 4.2 修复 MapManager 对“指定 id 副本”的创建路径

改造点：

- 文件：`Packages/cn.etetet.mapmanager/Scripts/Hotfix/Server/MapManagerComponentSystem.cs`
- 现状：`GetCopy(id!=0)` 找不到时直接 `return null`。
- 目标：当 `id!=0` 且不存在时，创建该 id 的副本（`AddChildWithIdAsync(id)`）并返回。

效果：

- 首次进入 Home（指定玩家ID副本）能正确创建，避免空返回导致后续异常。

### 4.3 修复登出上报 MapName 规范

改造点：

- 文件：`Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/G2Map_LogoutHandler.cs`
- 现状：`managerLogoutRequest.MapName = unit.Scene().Name`（可能携带 `@副本Id`）。
- 目标：改为 `unit.Scene().Name.GetSceneConfigName()`。

效果：

- `Map2MapManager_LogoutRequestHandler` 能准确找到 `MapInfo(MapName)`，避免玩家残留在 `Players` 集合。

### 4.4 Home 空副本回收

改造点：

- 文件：`Packages/cn.etetet.map/Scripts/Hotfix/Server/Map2MapManager_LogoutRequestHandler.cs`
- 目标：玩家移除后，若 `mapName == "Home"` 且 `Players.Count == 0` 且 `WaitEnterPlayer.Count == 0`，调用 `MapInfo.RemoveCopy(mapId)`。

效果：

- 避免“每人一个副本”带来的长期副本堆积与资源泄漏。

### 4.5 防御性校验

改造点：

- 文件：`Packages/cn.etetet.mapmanager/Scripts/Hotfix/Server/A2MapManager_GetMapRequestHandler.cs`
- 目标：对 `GetMapAsync` 结果做空判与错误日志，避免 NRE 影响链路。

---

## 5. 二期方案（按玩家数据加载建筑）

### 5.1 数据模型

建议新增：

1. `PlayerHomeComponent`：玩家 Home 持久化组件（归属 Player）。
2. `HomeBuildingData`：结构体（`BuildingId/ConfigId/Position/Rotation/Level/State`）。

### 5.2 进入 Home 的加载时机

建议在：

- `Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/M2M_UnitTransferRequestHandler.cs`

流程：

1. 判定 `mapName == "Home"` 且 `unit.UnitType == Player`。
2. 调用 `HomeEnterHelper` 从玩家 Home 数据创建建筑实体。
3. 仅在玩家私有副本内同步显示。

### 5.3 建造/移动/升级协议

建议新增 `C2M_Home*` / `M2C_Home*` 消息：

1. 客户端仅发意图。
2. 服务端校验（地块冲突、资源、冷却、上限）。
3. 写库成功后再回推客户端。

### 5.4 保存策略

1. 变更后增量保存。
2. 玩家登出强制刷盘。
3. 异常回滚采用“服务端状态为准”。

---

## 6. 兼容性与风险

1. 风险：`GetCopy(id!=0)` 行为变更可能影响其它“指定副本ID必须已存在”的调用方。
   - 规避：仅对 `Home` 路径启用“缺失即创建”，或引入 `createIfMissing` 参数（默认 false）。
2. 风险：副本回收与并发进入竞态。
   - 规避：在 MapManager Fiber 串行处理下执行；必要时对 Home 副本加短时锁。
3. 风险：登出路径若异常中断，可能仍有残留。
   - 规避：增加定时清理空副本巡检（可选）。

---

## 7. 验收标准

1. 两个账号登录后进入 `Home`，`SceneId` 不同。
2. 账号A在 Home 操作（移动/状态变更）不会触发账号B的 `M2C_CreateUnits`/AOI可见。
3. 账号登出后，空 Home 副本会被回收。
4. 构建通过：`dotnet build ET.sln`。

---

## 8. 实施建议（分阶段）

1. 先做一期并联调（隔离可见性问题先落地）。
2. 一期稳定后再做二期（建筑数据与玩法）。
3. 每阶段都跑最小回归：登录、进Home、切图、登出、重连。

---

## 9. 待你确认的决策

1. Home 私有副本ID是否采用 `player.Id`（推荐）。
2. `GetCopy(id!=0)` 的“缺失即创建”是否只对 `Home` 生效，还是全局生效。
3. 二期是否暂不开放“好友拜访 Home”。
