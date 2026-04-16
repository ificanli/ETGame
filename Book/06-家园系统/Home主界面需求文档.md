# Home主界面需求文档

**创建时间**：2026-04-10  
**最后更新**：2026-04-10  
**状态**：已完成  
**关联任务**：M0.2-W3 #15  
**涉及包**：`cn.etetet.home`、`cn.etetet.statesync`、`cn.etetet.map`

## 需求概述

当前 `Home` 方向真正缺的不是“完全没有服务端能力”，而是玩家进入 `Home` 后仍会打开通用 `LobbyPanel`，看到的是角色/配装/匹配语义为主的大厅外壳，而不是一个能承接基地玩法的 `Home` 主界面。

这会带来三个直接问题：

1. 玩家进入 `Home` 后没有“我已经回到基地”的明确界面反馈。
2. `Home` 已有的私有副本、建筑子 Entity、建造/升级/拆除/收取 Helper 等能力没有客户端承接层。
3. 后续工坊、合同、仓储、出击准备等家园子模块没有统一入口，只能继续塞进通用大厅页签，界面职责会越来越混乱。

因此本任务的核心不是补一个测试页，而是先补齐 `Home` 的首页承接能力，并在首页上落第一段真实可操作闭环。

## 当前工程约束

### 已有基础

1. `Home` 已是玩家私有副本，进入链路已接通。
2. 服务端已有 `PlayerHomeComponent`、`HomeBuilding`、`HomeProductionComponent`、`HomeContractComponent`。
3. 服务端已有 `HomeBuild / Upgrade / Demolish / Collect / Production / Contract` 样机 Helper。
4. `Home_C_15600.proto` 已定义快照与交互消息。
5. `LobbyPanel` 已预留 `BuildPanel` 页签和容器。

### 当前缺口

1. 进入 `Home` 后没有发送 `M2C_HomeSnapshot`。
2. `C2M_Home*` Handler 尚未接通。
3. 客户端没有 `Home` 运行时缓存组件与主界面刷新逻辑。
4. `BuildPanel` 当前只是空承载，不是 `Home` 首页。
5. 正式建筑配置表和正式 YIUI 资源结构都还没落地。

## 目标定义

### 目标态

玩家进入 `Home` 后，默认看到的是 `Home` 主界面，而不是通用大厅默认页。该主界面至少承担以下职责：

1. 告诉玩家当前处于基地场景。
2. 展示基地当前总览状态。
3. 提供家园子功能的统一入口分发。
4. 承接第一段真实可操作玩法闭环。

### 本期范围

本期只做 `Home` 首页首期版本，不承诺一次做完整家园系统全量 UI。

本期必须交付：

1. 进入 `Home` 后默认打开 `BuildPanel`，并把它作为 `Home` 首期主界面承载层。
2. 客户端能收到并缓存 `Home` 快照。
3. 首页能展示基地总览信息。
4. 首页能展示当前建筑列表与选中详情。
5. 首页能完成 `建造 / 升级 / 拆除 / 收取` 首段真实闭环。
6. 首页提供通往现有 `配装 / 匹配` 的快速入口，承接“基地 -> 出击准备”动线。

### 本期明确不做

1. 独立 `HomePanel` prefab 与完整 YIUI 资源重构。
2. 正式建筑配置表与正式建筑命名、图标、成本展示。
3. 工坊、合同、仓储的独立详情页与完整闭环。
4. 场景内建筑点击交互、槽位高亮和 3D 建筑可视化。
5. 好友拜访、联盟、掠夺、防守等二期内容。

## 界面需求

### 1. 页面定位

首期 `Home` 主界面是“基地首页”，不是“完整城建详情页”。

它要优先解决的是：

1. 玩家进入 `Home` 后看到正确语义的首页。
2. 让玩家知道基地里现在有什么、能做什么。
3. 给出第一段真实操作闭环，而不是只做展示。

### 2. 页面结构

首期页面由四个区块组成：

1. 头部区
2. 总览区
3. 建造入口区
4. 建筑列表 + 详情区

#### 2.1 头部区

职责：

1. 明确当前是 `Home` 基地首页。
2. 承接“去配装 / 去匹配”的快速跳转。

展示内容：

1. 页面标题：基地 / Home
2. 页面说明：当前为基地总览与首期建筑操作页
3. 快捷按钮：前往配装、前往匹配

#### 2.2 总览区

职责：

1. 快速告诉玩家当前基地整体状态。
2. 不依赖正式资源和复杂配置，也能成立。

展示指标：

1. 当前建筑数量
2. 建筑最高等级
3. 当前生产订单数量
4. 当前合同数量

说明：

1. 本期不展示正式资源条和正式建筑评分。
2. 本期总览指标必须全部来自运行时真实数据，不能做假数据。

#### 2.3 建造入口区

职责：

1. 给“基地还没有任何建筑”的玩家一个明确起手动作。
2. 让用户不用离开首页就能开始第一段闭环。

交互要求：

1. 提供“创建原型建筑”的可点击入口。
2. 建造动作必须走真实 `C2M_HomeBuildRequest`，不能做本地假数据。
3. 槽位由客户端按“当前最小未占用 SlotId”自动选择，避免本期额外补固定槽位 UI。

说明：

1. 由于正式建筑配置表尚未落地，本期建造入口只承接“原型建筑”建造，不承诺最终建筑命名和数值口径。
2. 这不是测试页，而是正式 `Home` 首页下的首期样机入口。

#### 2.4 建筑列表 + 详情区

职责：

1. 让玩家看到当前基地里已存在的建筑。
2. 让玩家能对选中建筑进行真实操作。

列表展示要求：

1. 按 `SlotId` 升序展示。
2. 每个建筑卡片至少展示：`SlotId`、`ConfigId`、`Level`、`State`。
3. 支持选中态切换。

详情区要求：

1. 当有选中建筑时，展示当前建筑详情。
2. 至少提供三个操作按钮：升级、拆除、收取。
3. 所有操作都必须走真实协议请求，不做本地模拟。
4. 操作完成后首页状态必须即时刷新。

空态要求：

1. 当没有任何建筑时，详情区展示“当前基地为空，请先创建首座建筑”。
2. 空态下仍保留首页身份，不跳到别的页。

## 技术方案

### 整体思路

本期采用“目标态明确、实现态分期”的方式：

1. 产品目标上，明确 `Home` 需要专属主界面。
2. 实现载体上，首期先复用现有 `LobbyPanel.BuildPanel`。
3. 数据链路上，先把 `Home` 快照和 `建造/升级/拆除/收取` 真正接通。
4. 资源层上，本期不新建独立 `HomePanel`，但会把首页静态骨架落到现有 `LobbyPanel.BuildPanel` prefab。
5. 构建方式上，通过 `YIUIMCP ExecuteMenu` 驱动 `ET/YIUI/Build Home Resources`，把 `Home` 首页骨架固化到资源层。

这样做的原因：

1. 当前 `BuildPanel` 已经存在，是最现实的首期落点。
2. 如果直接新建完整 `HomePanel`，会把工作重心拉到资源重建，而不是先拿到真实功能。
3. 先做首页承接和首段闭环，后续再把它从 `LobbyPanel.BuildPanel` 升级为独立 `HomePanel`，迁移成本可控。

### 涉及的包和文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Book/08-版本计划/M0.2-W3周计划.md` | 修改 | 登记任务 |
| `Book/08-版本计划/M0.2版本计划.md` | 修改 | 回写版本变更 |
| `Packages/cn.etetet.home/Scripts/Hotfix/Server/Helper/HomeEnterHelper.cs` | 修改 | 进入 Home 时补齐快照所需组件 |
| `Packages/cn.etetet.home/Scripts/Hotfix/Server/Helper/*.cs` | 复用/补辅助 | 复用现有 Home 样机能力 |
| `Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/*.cs` | 新增 | 接通 `C2M_Home*` 请求 |
| `Packages/cn.etetet.home/Scripts/Model/Client/*.cs` | 新增 | 客户端 Home 运行时缓存 |
| `Packages/cn.etetet.home/Scripts/Hotfix/Client/*.cs` | 新增 | 快照处理与客户端请求辅助 |
| `Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/M2M_UnitTransferRequestHandler.cs` | 修改 | 进入 Home 后主动推送快照 |
| `Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.cs` | 修改 | 增加 Home 首页运行时字段 |
| `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/*.cs` | 修改/新增 | BuildPanel 承接 Home 首页与操作逻辑 |

### 数据与交互边界

本期真实闭环只接到以下层级：

1. 进入 `Home` -> 服务端初始化 -> 主动推送 `M2C_HomeSnapshot`
2. 客户端缓存 `Home` 快照
3. `Home` 首页渲染快照
4. 用户在首页发起 `建造/升级/拆除/收取`
5. 服务端执行现有 Helper
6. 客户端根据响应更新本地缓存并刷新首页

### 关键约束

1. 不手改 `YIUIGen` 自动生成文件。
2. 不新开“纯测试页”。
3. 本期首页所有数据必须来自真实运行时，而不是写死假数据。
4. 正式建筑名、图标、成本和功能文案未配置化前，不在客户端硬编码最终产品口径。

## 实现步骤

1. 补计划文档与本需求文档、开发日志。
2. 打通 `Home` 进入时的快照推送与客户端缓存。
3. 接通 `建造/升级/拆除/收取` 对应 `C2M_Home*` Handler。
4. 把 `LobbyPanel` 的默认落点从 `RolePanel` 切到 `BuildPanel`。
5. 在 `BuildPanel` 内实现 `Home` 首期主界面和建筑操作。
6. 执行 `dotnet build ET.sln` 验证并回写文档。

## 验收标准

- [x] 进入 `Home` 后默认看到的是 `Home` 首期主界面，而不是角色页。
- [x] 首页能展示真实的建筑数量、等级和基础总览信息。
- [x] 当基地为空时，首页空态正确展示，且可从首页直接创建首座原型建筑。
- [x] 首页能完成真实的建造、升级、拆除、收取请求。
- [x] 操作结果会即时刷新到首页，不需要重新进图。
- [x] 首页提供现有 `配装`、`匹配` 的快速入口。
- [x] `dotnet build ET.sln` 通过。

## 关联文档

- [家园系统现状.md](../10-项目架构/家园系统现状.md)
- [家园系统前后端闭环说明.md](../10-项目架构/家园系统前后端闭环说明.md)
- [home-slg-citybuild-plan.md](home-slg-citybuild-plan.md)
- [home-ui-plan.md](../../Packages/cn.etetet.home/Doc/home-ui-plan.md)

## 实现追踪

> 开发完成后回写

| 步骤 | 完成日期 | 涉及文件 | 偏差说明 |
|------|---------|---------|---------|
| 步骤1 | 2026-04-10 | `Book/08-版本计划/*`、本文档、开发日志 | 无偏差 |
| 步骤2 | 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Helper/HomeEnterHelper.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Server/Helper/HomeSnapshotMessageHelper.cs`、`Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/M2M_UnitTransferRequestHandler.cs`、`Packages/cn.etetet.home/Scripts/Model/Client/HomeClientComponent.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientComponentSystem.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientHelper.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Client/M2C_HomeSnapshotHandler.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Client/M2C_HomeContractsChangedHandler.cs` | 无偏差 |
| 步骤3 | 2026-04-10 | `Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeBuildRequestHandler.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeUpgradeRequestHandler.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeDemolishRequestHandler.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Server/Handler/C2M_HomeCollectRequestHandler.cs`、`Packages/cn.etetet.home/Scripts/Hotfix/Client/HomeClientRequestHelper.cs` | 无偏差 |
| 步骤4 | 2026-04-10 | `Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_LoadoutUi.cs` | 无偏差 |
| 步骤5 | 2026-04-10 | `Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/LobbyPanel.prefab`、`Packages/cn.etetet.statesync/Editor/HomeYiuiBuilder.cs`、`Packages/cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.Home.cs`、`Packages/cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem_Home.cs` | 未新建独立 `HomePanel` prefab，但已把首页静态骨架迁回 `LobbyPanel.BuildPanel`，并通过 `YIUIMCP ExecuteMenu -> ET/YIUI/Build Home Resources` 实测生成成功 |
| 步骤6 | 2026-04-10 | `Packages/cn.etetet.home/Scripts/Model/Client/HomeClientComponent.cs`、`dotnet build ET.sln` | 为通过 `ET.Model` 分析器，对 3 个客户端 Home DTO 补充 `[EnableClass]`；整仓编译已通过 |
