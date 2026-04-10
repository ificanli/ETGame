# 局内GM按UnitConfigId召唤怪物开发日志

**功能**：局内GM按UnitConfigId召唤怪物
**关联设计文档**：[局内GM按UnitConfigId召唤怪物设计文档](./局内GM按UnitConfigId召唤怪物设计文档.md)
**关联任务**：M0.2-W2 #12
**开始时间**：2026-03-31
**开发者**：AI

## 开发进度

- [x] 步骤1：补周计划、版本计划、设计文档和开发日志
- [x] 步骤2：新增 Proto、客户端 GM 命令和服务端召唤链路
- [x] 步骤3：生成 Proto 并执行 `dotnet build ET.sln` 验证
- [x] 步骤4：回写文档与计划状态

## 决策记录

### 2026-03-31 - 复用现有动态GM体系
- **背景**：用户要“按钮和输入框”，并指定参数就是 `UnitConfig.Id`。
- **方案**：新增一个 `IGMCommand`，让 `cn.etetet.yiuigm` 自动生成按钮和整型输入框。
- **原因**：现有 GM 面板已经支持动态命令和参数渲染，改 prefab 没必要，维护成本也更低。
- **替代方案**：单独改 UI prefab。放弃原因是重复建设，而且不符合当前 GM 系统的扩展方式。

### 2026-03-31 - 服务端统一负责生成点和AI初始化
- **背景**：怪物必须真正进入现有地图运行时链路，才能验证 AI 和技能。
- **方案**：客户端只发 `UnitConfigId`，服务端通过独立 Helper 处理前方找点、NavMesh 投影、创建、出生点记录和 AI Buff 初始化。
- **原因**：现有怪物创建和 AI 初始化逻辑都在服务端，放在同一侧更安全，也能复用已有 Helper。
- **替代方案**：客户端直接本地生成表现对象。放弃原因是无法验证真实 AI、技能和服务端权威逻辑。

### 2026-03-31 - 生成链路对齐现有刷怪逻辑
- **背景**：新 GM 召唤出的怪物必须和地图自然刷出的怪物尽量走同一条运行时初始化路径。
- **方案**：`DebugSpawnMonsterHelper` 复用 `UnitFactory.Create + NavMesh 投影 + UnitSpawnPointComponent.SetSpawnPoint + EnsureConfiguredAIBuff + RefreshMonsterDisplayLevel` 这套现有链路。
- **原因**：这样更接近真实局内怪物状态，减少“GM 怪能生成但 AI/技能表现和正式刷怪不一致”的风险。
- **替代方案**：额外补一套调试专用初始化链路。放弃原因是容易和正式逻辑分叉，后续维护成本高。

## 问题日志

> 开发过程中遇到的问题和解决方案

### 2026-03-31 - Proto2CS 返回码异常
- **现象**：执行 `dotnet Bin/ET.Proto2CS.dll` 时控制台输出 `proto2cs ok!`，但进程返回码为 `1`。
- **原因**：当前工具链返回码行为异常，和生成结果不一致。
- **解决**：继续核对 `Packages/cn.etetet.proto/CodeMode/Model/*/StateSync_C_10700.cs` 生成产物，并以 `dotnet build ET.sln` 成功作为最终有效验证。

## 变更清单

| 时间 | 文件 | 操作 | 说明 |
|------|------|------|------|
| 2026-03-31 09:25:59 | `Book/08-版本计划/M0.2-W2周计划.md` | 修改 | 新增 W2 任务12：局内GM按UnitConfigId召唤怪物 |
| 2026-03-31 09:25:59 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 记录新增 FEAT-002 任务 |
| 2026-03-31 09:25:59 | `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物设计文档.md` | 新增 | 创建设计文档 |
| 2026-03-31 09:25:59 | `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物开发日志.md` | 新增 | 创建开发日志 |
| 2026-03-31 09:33:16 | `Packages/cn.etetet.statesync/Scripts/Model/Share/ErrorCode.cs` | 修改 | 新增调试召唤怪物错误码 |
| 2026-03-31 09:33:16 | `Packages/cn.etetet.statesync/Proto/StateSync_C_10700.proto` | 修改 | 新增 `C2M_DebugSpawnMonster` / `M2C_DebugSpawnMonster` |
| 2026-03-31 09:33:16 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/GM/GM_Command_SpawnMonsterByUnitConfig.cs` | 新增 | 新增局内 GM 命令，提供 `UnitConfigId` 输入框并发请求 |
| 2026-03-31 09:33:16 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/C2M_DebugSpawnMonsterHandler.cs` | 新增 | 新增地图服召唤怪物请求处理 |
| 2026-03-31 09:33:16 | `Packages/cn.etetet.statesync/Scripts/Hotfix/Server/DebugSpawnMonsterHelper.cs` | 新增 | 新增服务端怪物调试召唤辅助逻辑 |
| 2026-03-31 09:36:53 | `Packages/cn.etetet.proto/CodeMode/Model/Client/StateSync_C_10700.cs` | 修改 | 生成客户端 Proto 代码 |
| 2026-03-31 09:36:53 | `Packages/cn.etetet.proto/CodeMode/Model/ClientServer/StateSync_C_10700.cs` | 修改 | 生成共享 Proto 代码 |
| 2026-03-31 09:36:53 | `Packages/cn.etetet.proto/CodeMode/Model/Server/StateSync_C_10700.cs` | 修改 | 生成服务端 Proto 代码 |
| 2026-03-31 09:36:53 | `Book/08-版本计划/M0.2-W2周计划.md` | 修改 | 将任务12状态推进到未回归 |
| 2026-03-31 09:36:53 | `Book/08-版本计划/M0.2版本计划.md` | 修改 | 记录任务12开发完成并待回归 |
| 2026-03-31 09:36:53 | `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物设计文档.md` | 修改 | 回写实现追踪并标记设计完成 |
| 2026-03-31 09:36:53 | `Book/02-战斗系统/技术方案/局内GM按UnitConfigId召唤怪物开发日志.md` | 修改 | 回写构建验证、问题日志和开发总结 |

## 开发总结

> 开发结束后填写

- **实际完成**：
- **实际完成**：已新增局内 GM 按钮和 `UnitConfigId` 输入框，补齐地图服召唤协议、错误码、召唤 Handler/Helper，并完成 Proto 生成与 `dotnet build ET.sln` 验证。
- **未完成**：尚未做局内人工回归，当前计划状态为“未回归”。
- **与设计的偏差**：无功能性偏差；仅 `Proto2CS` 工具返回码与输出结果不一致，最终通过生成产物和编译结果确认实现有效。
- **后续待办**：进入游戏后用 GM 输入真实怪物 `UnitConfig.Id`，重点验证前方生成位置、怪物 AI、技能释放和异常 ID 提示。
