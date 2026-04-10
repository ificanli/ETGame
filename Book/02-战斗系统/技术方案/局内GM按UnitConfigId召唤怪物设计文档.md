# 局内GM按UnitConfigId召唤怪物设计文档

**创建时间**：2026-03-31
**最后更新**：2026-03-31
**状态**：已完成
**关联任务**：M0.2-W2 #12
**涉及包**：cn.etetet.statesync, cn.etetet.map, cn.etetet.yiuigm

## 需求概述

为了在局内快速验证怪物 AI 和技能，需要在现有 GM 面板中增加一个测试命令。该命令通过一个整型输入框接收 `UnitConfig.Id`，点击按钮后在当前玩家前方固定距离召唤对应怪物。

约束如下：

- 入口必须是现有局内 GM 面板，不单独修改 prefab。
- 输入参数语义固定为 `UnitConfig.Id`。
- 生成位置固定为当前玩家前方一段距离。
- 只允许召唤 `UnitType.Monster`，避免把玩家或 NPC 配置错误地走进测试链路。

## 技术方案

### 整体思路

复用 `cn.etetet.yiuigm` 现有的动态 GM 命令扫描机制，不改 UI 结构，只新增一个 `IGMCommand`。客户端命令负责采集 `UnitConfig.Id` 并向地图服发起 `ILocationRequest`；服务端在 `statesync` 中新增 Handler 和 Helper，统一完成怪物配置校验、前方固定距离找点、NavMesh 投影、怪物创建、出生点记录和 AI Buff 初始化。

这样做的原因：

- UI 已经支持“按钮 + 输入框”，不需要再维护一套独立面板。
- 召唤逻辑放服务端，能复用现有 `UnitFactory`、`MapUnitEnterHelper` 和怪物运行时初始化链路。
- 用独立 Helper 承载业务，避免把复杂逻辑直接堆在 `MessageLocationHandler` 中，符合 ET 架构规范。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Packages/cn.etetet.statesync/Proto/StateSync_C_10700.proto` | 修改 | 新增局内调试召唤怪物请求/响应协议 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/GM/GM_Command_SpawnMonsterByUnitConfig.cs` | 新增 | 新增 GM 命令，提供按钮和 `UnitConfig.Id` 输入框 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/C2M_DebugSpawnMonsterHandler.cs` | 新增 | 地图服处理 GM 召唤请求 |
| `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/DebugSpawnMonsterHelper.cs` | 新增 | 统一处理前方找点、落地、AI 初始化和日志 |
| `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物设计文档.md` | 新增 | 设计说明 |
| `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物开发日志.md` | 新增 | 开发过程记录 |

### Entity/Component 设计

本次不新增 Entity 或 Component，只复用现有：

- `Unit`
- `PathfindingComponent`
- `UnitSpawnPointComponent`
- `BuffComponent`
- `ThreatComponent`
- `CampComponent`

### 接口设计

新增地图服请求：

- `C2M_DebugSpawnMonster : ILocationRequest`
  - `RpcId`
  - `UnitConfigId`

新增地图服响应：

- `M2C_DebugSpawnMonster : ILocationResponse`
  - `RpcId`
  - `Error`
  - `Message`
  - `SpawnedUnitId`
  - `SpawnedConfigId`

### 数据结构

- 输入 ID 来源：`UnitConfig.Id`
- 目标配置必须满足 `UnitConfig.UnitType == UnitType.Monster`
- 固定前方距离由服务端 Helper 常量维护，避免散落在调用方

## 实现步骤

1. 先补计划、设计文档和开发日志，登记 W2 任务。
2. 在 `StateSync_C_10700.proto` 中新增调试召唤怪物协议，并生成 Proto 代码。
3. 新增客户端 GM 命令，提供整型输入并向地图服发送请求。
4. 新增服务端 Handler 和 Helper，完成怪物创建、NavMesh 落点和 AI 初始化。
5. 执行 `dotnet build ET.sln` 验证编译，并回写文档和计划状态。

## 验收标准

- [ ] 局内 GM 面板出现“召唤怪物(UnitConfigId)”命令，且自带整型输入框。
- [ ] 输入合法的怪物 `UnitConfig.Id` 后，能在当前玩家前方固定距离召唤怪物。
- [ ] 新召唤的怪物完成 NavMesh 落点、出生点记录和 AI Buff 初始化，能够参与 AI/技能测试。
- [ ] 输入非法 ID、非怪物配置或缺少场景/玩家上下文时，能收到明确失败日志或响应。
- [x] `dotnet build ET.sln` 通过。

## 关联文档

- [M0.2-W2周计划.md](../../08-版本计划/M0.2-W2周计划.md)

## 实现追踪

> 开发完成后由 AI 自动填写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-03-31 | `Book/08-版本计划/M0.2-W2周计划.md`, `Book/08-版本计划/M0.2版本计划.md`, `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物设计文档.md`, `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物开发日志.md` | 无偏差 |
| 步骤2 | 2026-03-31 | `Packages/cn.etetet.statesync/Proto/StateSync_C_10700.proto` | 无偏差 |
| 步骤3 | 2026-03-31 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/GM/GM_Command_SpawnMonsterByUnitConfig.cs` | 无偏差 |
| 步骤4 | 2026-03-31 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/C2M_DebugSpawnMonsterHandler.cs`, `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/DebugSpawnMonsterHelper.cs`, `Packages/cn.etetet.statesync/Scripts/Model/Share/ErrorCode.cs` | 无偏差 |
| 步骤5 | 2026-03-31 | `Packages/cn.etetet.proto/CodeMode/Model/Client/StateSync_C_10700.cs`, `Packages/cn.etetet.proto/CodeMode/Model/ClientServer/StateSync_C_10700.cs`, `Packages/cn.etetet.proto/CodeMode/Model/Server/StateSync_C_10700.cs` | `dotnet Bin/ET.Proto2CS.dll` 控制台显示 `proto2cs ok!` 但返回码异常，后续以生成产物和 `dotnet build ET.sln` 通过作为有效依据 |
