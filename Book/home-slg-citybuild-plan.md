# Home 搜打撤基地建设系统需求与开发计划

## 1. 文档目标

本文档用于对齐 `Home` 场景下的基地建设系统一期目标、系统边界、包设计、数据模型、建筑功能、开发阶段与验收标准。

本系统不是独立 SLG，而是服务于搜打撤主循环的局外基地系统。基地的价值在于承接“局内带出 -> 局外处理 -> 下局准备”。

---

## 2. 当前项目基础

结合当前仓库代码，`Home` 已具备以下基础能力：

1. `Home` 已按玩家私有副本进入。
2. `Home` 副本 `mapId` 已绑定为 `player.Id`。
3. `MapManager` 已支持按指定副本 Id 创建 `Home` 副本。
4. 已存在自动化测试验证 `Home` 私有隔离与 `mapId == player.Id`。
5. 客户端切入 `Home` 时已有专门分支，不走常规战斗地图展示逻辑。

当前关键代码位置：

1. `Packages/cn.etetet.map/Scripts/Hotfix/Server/C2G_EnterMapHandler.cs`
2. `Packages/cn.etetet.mapmanager/Scripts/Hotfix/Server/MapManagerComponentSystem.cs`
3. `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Home_MapIdEqualsPlayerId_Test.cs`
4. `Packages/cn.etetet.test/Scripts/Hotfix/Test/Test_Home_TwoPlayers_Isolated_Test.cs`
5. `Packages/cn.etetet.map/Scripts/Hotfix/Client/SceneChangeHelper.cs`
6. `Packages/cn.etetet.map/Scripts/HotfixView/Client/Scene/SceneChangeFinishEvent_CreateUIHelp.cs`
7. `Packages/cn.etetet.map/Scripts/HotfixView/Client/AfterUnitCreate_CreateUnitView.cs`

结论：

`Home` 的“玩家私有场景”基础已经具备，基地系统应直接建立在这一基础上，不需要重复改造私有副本方案。

---

## 3. 一期产品目标

一期目标聚焦在“单人 Home 基地闭环”，服务搜打撤主循环，不追求完整 SLG 大世界玩法。

一期建议范围：

1. 玩家进入 `Home` 后加载自己的建筑数据。
2. 建筑直接做在 `Home` 场景内，不做纯 UI 城建。
3. 支持点击场景地块创建建筑。
4. 支持点击场景中的建筑，选择升级或拆除。
5. 支持基地生产，鼓励玩家每天上线收菜。
6. 支持“局内带出资源 -> 基地加工 -> 下局准备”的外围循环。
7. 支持离线后重登恢复。
8. 所有核心状态由服务端权威维护。

一期明确不做：

1. 好友拜访。
2. 联盟协作。
3. 掠夺与防守战斗。
4. 多建造队列。
5. 复杂付费加速链路。
6. 跨玩家 Home 交互。

一期核心定位补充：

1. 基地是搜打撤的外围养成，不是独立资源经营游戏。
2. 建筑效果优先服务局前准备、局后恢复、物资处理、情报获取和长期解锁。
3. 基地允许存在每日可收的被动产出，但不能替代局内搜集和撤离带出。

一期交互形态补充：

1. 玩家登录后直接进入 `Home` 场景。
2. 默认处于普通 Home 浏览态。
3. 点击 `LobbyPanel` 中的“建设”按钮后，进入 `Home` 场景内的建设模式。
4. 建设模式下，地块可点击，建筑可点击，不通过 UI 面板直接摆放建筑。

---

## 4. 核心设计原则

### 4.1 架构原则

遵循 `et-arch` 要求：

1. Entity 只存数据，不写业务逻辑。
2. 业务逻辑放在 System/Helper。
3. 不在 `map` 包中堆积基地业务。
4. 进入 `Home` 的链路仍由 `map` 负责，基地玩法由独立包承接。

### 4.2 权威原则

1. 客户端只发送建造、升级、收取、生产、收容意图，不发送最终结果。
2. 服务端校验资源、地块、前置条件、升级条件、生产条件和收容条件。
3. 服务端写入成功后，再同步给客户端。
4. 异常或不同步时，以服务端状态为准。

### 4.3 主循环结合原则

1. 基地产出必须服务下一局，而不是脱离单局体验自循环。
2. 高价值产物应主要依赖局内带出材料加工，而不是纯挂机生成。
3. 建筑效果优先做准备能力、恢复能力、信息能力、制作能力。
4. 应避免大量直接提升局内硬数值的建筑效果。

### 4.4 表现原则

1. 建筑是 `Home` 场景内对象，不是列表式 UI 格子。
2. UI 只负责模式切换、弹窗确认、状态展示和操作按钮承载。
3. 地块点击、建筑点击、选中态、高亮态由场景交互层负责。

### 4.5 生产原则

1. 基地生产分为“被动产出”和“加工产出”两类。
2. 被动产出用于拉起每日上线动机，但价值不能过高。
3. 加工产出需要消耗局内带出的材料，体现搜打撤回收价值。
4. 收菜上限应受建筑等级和仓储上限约束。

---

## 5. 包设计建议

建议新增高层玩法包：`cn.etetet.home`

原因：

1. `map` 包应聚焦地图切换、副本、场景管理。
2. `home` 属于具体玩法系统，不应长期耦合在 `map`。
3. 后续基地若接任务、数值、背包、活动，独立包更容易维护依赖关系。

建议包内结构：

### 5.1 Model

路径建议：

`Packages/cn.etetet.home/Scripts/Model/`

职责：

1. 基地持久化组件定义。
2. 建筑数据结构。
3. 解锁状态、生产状态、建造队列状态。
4. 收容对象状态与处理结果。

### 5.2 Hotfix

路径建议：

`Packages/cn.etetet.home/Scripts/Hotfix/`

职责：

1. 基地进入加载逻辑。
2. 建造/升级/移动/收取的服务端校验。
3. 生产订单、被动产出和收容处理。
4. 协议处理与同步。
5. 离线收益结算与存档保存。

### 5.3 HotfixView

路径建议：

`Packages/cn.etetet.home/Scripts/HotfixView/`

职责：

1. Home 主界面交互。
2. 建设模式切换。
3. 地块点击、建筑点击、选中高亮和操作菜单。
4. 建筑状态展示、倒计时、红点和操作反馈。

### 5.4 Proto

路径建议：

`Packages/cn.etetet.home/Proto/`

职责：

1. 基地相关消息定义。
2. 客户端意图消息与服务端回推消息。

---

## 6. 数据模型设计

### 6.1 玩家持久化组件

建议新增：

`PlayerHomeComponent`

挂载位置建议：

挂在 `Player` 下，作为玩家基地的持久化组件。

建议字段：

1. `long HomeVersion`
2. `long LastSettleTime`
3. `int BuildingQueueCount`
4. `Dictionary<long, HomeBuildingData> Buildings`
5. `HashSet<int> UnlockedBuildingConfigIds`
6. `Dictionary<int, long> ResourceSnapshot`
7. `Dictionary<long, HomeProductionOrderData> ProductionOrders`
8. `Dictionary<long, HomeCaptureData> Captures`

说明：

1. `HomeVersion` 用于后续数据迁移与兼容。
2. `LastSettleTime` 用于离线收益结算。
3. `Buildings` 保存建筑实例数据。
4. `UnlockedBuildingConfigIds` 保存解锁状态，避免硬编码。
5. `ProductionOrders` 保存加工队列和完成时间。
6. `Captures` 保存医疗站捕捉/收容对象及处理状态。

### 6.2 建筑数据结构

建议新增结构体：

`HomeBuildingData`

建议字段：

1. `long BuildingId`
2. `int ConfigId`
3. `int Level`
4. `int State`
5. `int AreaId`
6. `float PositionX`
7. `float PositionY`
8. `float PositionZ`
9. `float RotationY`
10. `long BuildStartTime`
11. `long BuildFinishTime`
12. `long LastCollectTime`
13. `long LastProductionTime`

说明：

1. `BuildingId` 是实例 Id，不等于配置 Id。
2. `ConfigId` 对应建筑配置。
3. `State` 可覆盖空闲、建造中、升级中、待收取等状态。
4. 坐标字段用于支持自由摆放；如果一期改成固定槽位，也建议保留。
5. `LastProductionTime` 用于被动产出类建筑的离线结算。

### 6.3 生产与收容数据结构

建议新增结构体：

1. `HomeProductionOrderData`
2. `HomeCaptureData`

`HomeProductionOrderData` 建议字段：

1. `long OrderId`
2. `int BuildingConfigId`
3. `int RecipeId`
4. `int State`
5. `long StartTime`
6. `long FinishTime`
7. `Dictionary<int, long> CostItems`
8. `Dictionary<int, long> OutputItems`

`HomeCaptureData` 建议字段：

1. `long CaptureId`
2. `int CaptureType`
3. `int State`
4. `long StartTime`
5. `long FinishTime`
6. `int RewardType`
7. `long RewardValue`

说明：

1. 生产订单用于工坊等建筑的加工。
2. 收容数据用于医疗站处理被捕捉/收容目标。
3. 医疗站的“捕捉”当前建议理解为对局内带回的伤员、样本或特殊目标进行收容和处理，具体对象定义后续再细化。

### 6.4 运行时数据

如确实需要运行时态，建议独立为非持久化组件：

1. `HomeRuntimeComponent`
2. `HomeBuildingRuntimeComponent`
3. `HomeBuildModeComponent`
4. `HomeSelectionComponent`

职责：

1. 当前进入 `Home` 后的运行中状态缓存。
2. 当前场景下的建筑表现映射。
3. 当前是否处于建设模式。
4. 当前选中的地块或建筑。
5. 不直接替代持久化数据。

---

## 7. 配置表设计

一期至少需要以下配置：

### 7.1 BuildingConfig

用途：

建筑静态定义。

建议字段：

1. `Id`
2. `Name`
3. `Type`
4. `AreaId`
5. `Prefab`
6. `MaxLevel`
7. `UnlockConditionType`
8. `UnlockConditionValue`
9. `InitialLevel`
10. `CanMove`
11. `CanCollect`
12. `FunctionType`
13. `PassiveOutputGroupId`
14. `ProductionGroupId`
15. `CaptureGroupId`

### 7.2 BuildingLevelConfig

用途：

建筑等级成长配置。

建议字段：

1. `Id`
2. `BuildingId`
3. `Level`
4. `UpgradeCost`
5. `UpgradeTime`
6. `PassiveOutputRate`
7. `Capacity`
8. `RequiredPlayerLevel`
9. `RequiredBuildingId`
10. `RequiredBuildingLevel`
11. `FunctionValue`

### 7.3 HomeProductionConfig

用途：

定义基地被动产出和加工配方。

建议字段：

1. `Id`
2. `BuildingId`
3. `ProductionType`
4. `InputItemIds`
5. `InputItemCounts`
6. `OutputItemIds`
7. `OutputItemCounts`
8. `Duration`
9. `Capacity`
10. `DailyLimit`

### 7.4 HomeCaptureConfig

用途：

定义医疗站可收容对象和产出规则。

建议字段：

1. `Id`
2. `CaptureType`
3. `RequireBuildingLevel`
4. `Duration`
5. `RewardType`
6. `RewardValue`
7. `FailRate`
8. `ExtraCondition`

### 7.5 HomeAreaConfig

用途：

地块或摆放区域定义。

建议字段：

1. `Id`
2. `MapName`
3. `AreaType`
4. `CenterX`
5. `CenterY`
6. `CenterZ`
7. `SizeX`
8. `SizeY`
9. `SizeZ`
10. `PlacementRule`

说明：

如果一期采用“点击具体地块建造”，`HomeAreaConfig` 应进一步细化到可交互地块或地块组，而不是只做大区域包围盒。

---

## 8. 一期推荐建筑规划

一期推荐建筑不走传统 SLG 的木石粮路线，而是围绕搜打撤的外围养成来设计。

### 8.1 指挥中心

定位：

基地主建筑，决定其它建筑解锁、等级上限和可开放地块。

一期功能：

1. 解锁新建筑。
2. 提升全基地功能上限。
3. 决定可用生产队列上限。

### 8.2 补给站

定位：

每日可收的基础被动产出建筑。

一期功能：

1. 被动产出基础补给品、通用零件或低级耗材。
2. 产出有缓存上限，鼓励玩家每日上线收取。
3. 高等级提升产出速度和缓存上限。

### 8.3 工坊

定位：

消耗局内带出材料进行加工的核心建筑。

一期功能：

1. 消耗局内带出资源生产弹药、药品、投掷物、维修件或通用改装材料。
2. 支持订单队列和加工时间。
3. 高等级解锁更高级配方。

### 8.4 医疗收容站

定位：

局后恢复与收容处理建筑。

一期功能：

1. 处理角色伤势、恢复状态。
2. 被动产出基础医疗资源。
3. 支持“捕捉/收容”局内带回的伤员、样本或特殊目标。
4. 收容完成后产出医疗物资、研究材料或情报奖励。

说明：

这里的“捕捉”先按基地收容系统设计，不在一期展开复杂 AI 生物玩法。

### 8.5 情报站

定位：

局前信息优势建筑。

一期功能：

1. 被动产出情报点。
2. 提供每日刷新情报。
3. 解锁地图热点、物资倾向、特殊事件提示或委托入口。

### 8.6 仓储站

定位：

基地容量与缓存上限建筑。

一期功能：

1. 提升资源和产物缓存上限。
2. 提升生产订单缓存或特殊物资保留位。
3. 与补给站、工坊、医疗收容站的收菜上限联动。

---

## 9. 基地资源与生产设计

### 9.1 资源来源划分

基地资源建议分为两类：

1. 基地自产资源
2. 局内带出资源

### 9.2 基地自产资源

推荐一期类型：

1. 基础补给
2. 医疗物资
3. 情报点
4. 通用零件

用途：

1. 拉起每日上线收菜动机。
2. 用于基础制作、恢复和信息刷新。

### 9.3 局内带出资源

推荐一期类型：

1. 电子元件
2. 药剂原料
3. 稀有金属
4. 样本
5. 加密数据

用途：

1. 工坊加工。
2. 医疗收容处理。
3. 高级建筑升级和功能解锁。

### 9.4 生产模型

基地生产建议分两类：

1. 被动产出
2. 加工产出

被动产出：

1. 随时间累积。
2. 上线即可收取。
3. 受建筑等级和仓储上限限制。

加工产出：

1. 需要消耗局内带出材料。
2. 需要排队和等待完成。
3. 用于制作更有价值的下局物资。

### 9.5 搜打撤主循环绑定

推荐主循环：

1. 玩家完成单局并撤离带出材料。
2. 回到 `Home` 后处理基地收菜。
3. 在工坊和医疗收容站消耗材料进行加工或收容。
4. 在情报站获取下局情报。
5. 在补给站和工坊准备下局消耗品。
6. 再次进入单局玩法。

---

## 10. 服务端主流程设计

### 10.1 进入 Home

接入位置建议：

`Packages/cn.etetet.map/Scripts/Hotfix/Server/Map/M2M_UnitTransferRequestHandler.cs`

触发条件：

1. `mapName == "Home"`
2. `unit.UnitType == Player`

处理流程：

1. 玩家切入 `Home`。
2. `map` 层负责完成场景切换和玩家 Unit 初始化。
3. `home` 层通过 `HomeEnterHelper` 读取 `PlayerHomeComponent`。
4. 执行一次收益结算。
5. 构建进入时需要下发的基地快照。
6. 同步给客户端加载场景内建筑与资源状态。
7. 客户端默认进入普通 Home 浏览态，不自动进入建设模式。

说明：

不要在 `M2M_UnitTransferRequestHandler` 中直接堆积基地业务逻辑，应转调 `HomeEnterHelper`。

### 10.2 建造流程

建议协议：

1. `C2M_HomeBuildRequest`
2. `M2C_HomeBuildResponse`
3. `M2C_HomeBuildingChanged`

服务端流程：

1. 客户端点击 `LobbyPanel` 的“建设”按钮，进入建设模式。
2. 客户端点击场景地块，请求建造指定建筑。
3. 服务端校验当前场景是否为 `Home`。
4. 校验建筑配置是否合法。
5. 校验地块是否合法、是否已占用、是否满足前置条件、资源是否足够。
6. 扣除资源。
7. 创建 `HomeBuildingData`。
8. 写入玩家存档。
9. 下发最新建筑状态。

### 10.3 升级流程

建议协议：

1. `C2M_HomeUpgradeRequest`
2. `M2C_HomeUpgradeResponse`
3. `M2C_HomeBuildingChanged`

服务端流程：

1. 校验建筑实例存在。
2. 校验是否达到最大等级。
3. 校验前置建筑与资源。
4. 启动升级状态或直接完成升级。
5. 落库并回推。

### 10.4 移动流程

建议协议：

1. `C2M_HomeMoveRequest`
2. `M2C_HomeMoveResponse`

服务端流程：

1. 校验建筑是否允许移动。
2. 校验目标区域是否合法。
3. 校验与其它建筑不冲突。
4. 更新位置并同步。

### 10.5 收取流程

建议协议：

1. `C2M_HomeCollectRequest`
2. `M2C_HomeCollectResponse`
3. `M2C_HomeResourceChanged`

服务端流程：

1. 结算当前建筑可收取资源。
2. 增加资源。
3. 更新建筑最后收取时间。
4. 落库并回推。

### 10.6 拆除流程

建议协议：

1. `C2M_HomeDemolishRequest`
2. `M2C_HomeDemolishResponse`
3. `M2C_HomeBuildingChanged`

服务端流程：

1. 客户端点击场景中的建筑。
2. 客户端弹出建筑操作菜单，选择拆除。
3. 服务端校验建筑实例存在且允许拆除。
4. 校验是否存在不可拆前置依赖。
5. 删除建筑数据或改为拆除中状态。
6. 写入存档并回推场景刷新。

### 10.7 生产流程

建议协议：

1. `C2M_HomeStartProductionRequest`
2. `M2C_HomeStartProductionResponse`
3. `C2M_HomeCollectProductionRequest`
4. `M2C_HomeProductionChanged`

服务端流程：

1. 客户端点击工坊或其它可生产建筑。
2. 服务端校验建筑功能、配方、材料与队列。
3. 创建 `HomeProductionOrderData`。
4. 进入进行中状态。
5. 到时完成并允许收取。
6. 玩家上线或点击建筑时收取产物。

### 10.8 医疗收容流程

建议协议：

1. `C2M_HomeStartCaptureRequest`
2. `M2C_HomeStartCaptureResponse`
3. `C2M_HomeCollectCaptureRequest`
4. `M2C_HomeCaptureChanged`

服务端流程：

1. 客户端点击医疗收容站。
2. 选择可处理的收容对象。
3. 服务端校验对象、建筑等级和消耗。
4. 创建 `HomeCaptureData` 并开始处理。
5. 完成后生成医疗物资、研究材料或情报奖励。
6. 玩家手动收取结果。

---

## 11. 客户端表现建议

### 11.1 面板策略

当前客户端在进入 `Home` 后打开的是 `LobbyPanel`，但玩家已经身处 `Home` 场景。

一期有两种接法：

1. 在 `LobbyPanel` 基础上增加“建设”按钮，进入建设模式。
2. 单独新增 `HomePanel`，承接建设模式下的操作区和建筑菜单。

建议：

如果 `LobbyPanel` 已承载大量非基地逻辑，建议保留 `LobbyPanel` 作为入口层，再拆出独立 `HomePanel` 或建设模式 UI 作为操作层。

### 11.2 客户端职责

客户端只负责：

1. 展示场景内建筑。
2. 管理建设模式开关。
3. 处理地块点击与建筑点击。
4. 发送建造、升级、拆除、移动、收取、生产、收容意图。
5. 展示倒计时和资源变更。
6. 根据服务端推送刷新状态。

客户端不负责：

1. 最终资源计算。
2. 建筑是否合法。
3. 离线收益结算。
4. 升级完成判定。

### 11.3 场景交互建议

建议增加以下客户端运行时能力：

1. 地块点击检测。
2. 建筑点击检测。
3. 建设模式下的地块高亮。
4. 建筑选中后的操作菜单。
5. 非建设模式下屏蔽误触建造逻辑。
6. 不同建筑展示各自的生产、收容、升级和收取状态。

---

## 12. 测试方案

结合现有 `cn.etetet.test` 包，建议新增以下测试：

### 12.1 进入加载测试

目标：

1. 玩家进入 `Home` 后能正确加载基地快照。
2. 建筑数量与存档一致。

### 12.2 建造成功测试

目标：

1. 合法地块点击建造请求成功。
2. 建筑实例写入 `PlayerHomeComponent`。
3. 资源正确扣除。
4. 对应地块状态更新正确。

### 12.3 非法建造测试

目标：

1. 资源不足时拒绝建造。
2. 区域非法时拒绝建造。
3. 前置条件不足时拒绝建造。

### 12.4 升级测试

目标：

1. 建筑升级后等级正确变化。
2. 不满足条件时返回错误码。

### 12.5 拆除测试

目标：

1. 建筑拆除请求成功。
2. 建筑从玩家基地数据中移除或进入拆除状态。
3. 存在前置依赖时正确拒绝。

### 12.6 生产与收菜测试

目标：

1. 被动产出会按离线时间正确累积。
2. 收取后缓存正确清空或扣减。
3. 仓储上限正确生效。

### 12.7 医疗收容测试

目标：

1. 医疗站可正确启动收容处理。
2. 收容完成后可领取正确奖励。
3. 条件不足时正确拒绝。

### 12.8 重登恢复测试

目标：

1. 玩家退出后再次进入 `Home`。
2. 建筑位置、等级、状态与退出前一致。

### 12.9 隔离性回归测试

目标：

继续保留并回归现有测试：

1. `Test_Home_MapIdEqualsPlayerId_Test`
2. `Test_Home_TwoPlayers_Isolated_Test`

---

## 13. 分阶段开发计划

### 阶段一：包与配置骨架

目标：

1. 创建 `cn.etetet.home` 包。
2. 补齐 `package.json`、`PackageType`、基础程序集目录。
3. 新增建筑、生产、收容相关配置表定义。

产出：

1. 包结构可编译。
2. 配置表可导出。

### 阶段二：数据与进入加载

目标：

1. 增加 `PlayerHomeComponent`。
2. 打通进入 `Home` 时的基地快照加载。
3. 客户端能在场景中显示基础建筑数据。
4. 建筑功能类型和状态可正确展示。

产出：

1. 玩家进入 `Home` 可见自己的建筑。

### 阶段三：建造与升级闭环

目标：

1. 完成“建设”模式切换。
2. 完成地块点击建造协议。
3. 完成建筑点击升级/拆除协议。
4. 完成资源扣除与条件校验。
5. 完成六类一期建筑的功能骨架。

产出：

1. 玩家可在场景内完成基础建造、升级、拆除。

### 阶段四：生产、收容与离线结算

目标：

1. 完成生产型建筑的收益累积。
2. 完成工坊加工订单。
3. 完成医疗收容处理。
4. 完成离线收益结算。
5. 完成登出保存与重登恢复。

产出：

1. 核心基地循环成立。

### 阶段五：UI 完整化与回归

目标：

1. 完成 Home 主界面和建筑交互面板。
2. 跑通编译与测试回归。

产出：

1. 形成可试玩的一期版本。

---

## 14. 验收标准

一期验收建议以以下标准为准：

1. 玩家进入 `Home` 时，能稳定加载自己的建筑和资源状态。
2. 不同玩家进入 `Home` 后仍保持私有副本隔离。
3. 玩家点击 `LobbyPanel` 的“建设”按钮后，可进入建设模式。
4. 玩家可点击场景地块创建建筑。
5. 玩家可点击场景建筑执行升级或拆除。
6. `指挥中心`、`补给站`、`工坊`、`医疗收容站`、`情报站`、`仓储站` 均有真实效果，不是空壳建筑。
7. 基地被动产出和加工产出都能正常结算与收取。
8. 医疗收容站可启动收容处理并产出结果。
9. 建造、升级、拆除、移动、收取均由服务端校验并回推结果。
10. 玩家重登后，基地状态与退出前保持一致。
11. 关键异常路径有明确错误码，不使用 hard code 文案代替协议错误。
12. 构建通过：`dotnet build ET.sln`。
13. 基地相关自动化测试通过。

---

## 15. 当前待确认决策

在正式开发前，建议先确认以下产品决策：

1. 一期是否只做单人基地，不开放好友拜访。
2. 建筑布局是自由摆放，还是固定槽位。
3. 补给站、情报站和医疗站的被动产出节奏是按小时还是按天。
4. 工坊一期具体产物先做哪几类。
5. 医疗站“捕捉/收容”的对象是伤员、样本、目标人物，还是三者都做。
6. 一期是否只保留单建造队列和单生产队列。
7. `Home` 是否拆出独立 `HomePanel` 作为建设模式操作层。
8. 建筑升级是即时完成，还是带建造时间。

---

## 16. 推荐结论

推荐的一期落地方向如下：

1. `Home` 保持个人私有副本。
2. 新增独立玩法包 `cn.etetet.home`。
3. 一期只做单人基地闭环，且交互发生在 `Home` 场景内。
4. 采用服务端权威校验。
5. 登录后直接进入 `Home`，通过 `LobbyPanel` 的“建设”按钮进入建设模式。
6. 一期建筑优先做 `指挥中心`、`补给站`、`工坊`、`医疗收容站`、`情报站`、`仓储站`。
7. 生产分为被动收菜和消耗材料加工两类。
8. 医疗站按“医疗 + 收容处理”双功能设计。
9. 先实现建筑加载、地块建造、建筑升级、建筑拆除、收取、生产、收容、重登恢复。
10. 拜访、联盟、掠夺等内容放到二期以后。

这样可以在不破坏当前 `map` 结构的前提下，形成一套真正服务搜打撤单局循环的局外基地系统，而不是一个脱节的 SLG 空壳。
