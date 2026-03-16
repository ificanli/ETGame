# Home 城建系统 — UI 实现方案

> 本文档描述客户端 UI 层的完整实现方案，供后续开发参考。
> 服务端逻辑（Helper/Handler/Component）已完成骨架，本文档聚焦于 **ModelView / HotfixView** 两层。

---

## 一、整体架构

### 1.1 UI 层级关系

```
LobbyPanel (已有)
  ├─ RolePanel (角色页签)
  ├─ EquipPanel (装备页签)
  ├─ BuildPanel ← 基地主页签（需实现）
  │    ├─ 建筑槽位列表 (LoopScroll)
  │    ├─ 资源显示区
  │    └─ 功能按钮区 (建造/升级/拆除/收取)
  ├─ MatchPanel (匹配页签)
  └─ ExplorePanel (探索页签)

HomeBuildView (建造选择弹窗)
  └─ 可建造建筑列表 (LoopScroll)

HomeProductionView (工坊加工弹窗)
  ├─ 配方列表 (LoopScroll)
  └─ 当前订单状态

HomeContractView (悬赏合同弹窗)
  ├─ 可接取合同列表
  └─ 进行中合同列表
```

### 1.2 YIUI 三层代码结构（每个 Panel/View 均需）

| 层 | 目录 | 说明 |
|----|------|------|
| ModelView/Component | `Scripts/ModelView/Client/YIUIComponent/` | 数据字段、EntityRef 引用 |
| HotfixView/Gen | `Scripts/HotfixView/Client/YIUIGen/` | **YIUI 工具自动生成**，绑定 Inspector 组件 |
| HotfixView/System | `Scripts/HotfixView/Client/YIUISystem/` | 自定义业务逻辑 |

生命周期顺序：`Awake → YIUIBind(自动) → YIUIInitialize → YIUIOpen`

---

## 二、客户端数据模型

### 2.1 客户端镜像组件

在 `Scripts/Model/Client/` 下创建，由 M2C 消息填充，UI 读取。

```csharp
// Scripts/Model/Client/PlayerHomeComponent.cs
namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class PlayerHomeComponent : Entity, IAwake, IDestroy
    {
        public long HomeVersion;
        public long LastSettleTime;
        public List<int> UnlockedBuildingConfigIds = new();
        public List<HomeBuildingInfo> Buildings = new();        // proto 结构直接存
    }
}

// Scripts/Model/Client/HomeProductionComponent.cs
namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class HomeProductionComponent : Entity, IAwake, IDestroy
    {
        public List<HomeProductionOrderInfo> Orders = new();
    }
}

// Scripts/Model/Client/HomeContractComponent.cs
namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class HomeContractComponent : Entity, IAwake, IDestroy
    {
        public List<HomeContractInfo> AvailableContracts = new();
        public List<HomeContractInfo> ActiveContracts = new();
    }
}
```

### 2.2 客户端组件 System

```csharp
// Scripts/Hotfix/Client/System/PlayerHomeComponentSystem.cs
namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerHomeComponent))]
    public static partial class PlayerHomeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerHomeComponent self) { }

        [EntitySystem]
        private static void Destroy(this PlayerHomeComponent self)
        {
            self.Buildings.Clear();
            self.UnlockedBuildingConfigIds.Clear();
        }

        // 按 SlotId 查找建筑
        public static HomeBuildingInfo GetBuildingBySlot(this PlayerHomeComponent self, int slotId)
        {
            foreach (var b in self.Buildings)
            {
                if (b.SlotId == slotId) return b;
            }
            return null;
        }

        // 按 BuildingId 查找建筑
        public static HomeBuildingInfo GetBuilding(this PlayerHomeComponent self, long buildingId)
        {
            foreach (var b in self.Buildings)
            {
                if (b.BuildingId == buildingId) return b;
            }
            return null;
        }
    }
}
```

---

## 三、M2C 消息处理

### 3.1 快照推送（进入 Home 时）

```csharp
// Scripts/Hotfix/Client/Handler/M2C_HomeSnapshotHandler.cs
namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_HomeSnapshotHandler : MessageHandler<Scene, M2C_HomeSnapshot>
    {
        protected override async ETTask Run(Scene scene, M2C_HomeSnapshot message)
        {
            // 1. 更新 PlayerHomeComponent
            PlayerHomeComponent homeComp = scene.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                homeComp = scene.AddComponent<PlayerHomeComponent>();
            }
            homeComp.HomeVersion = message.HomeVersion;
            homeComp.LastSettleTime = message.LastSettleTime;
            homeComp.UnlockedBuildingConfigIds.Clear();
            homeComp.UnlockedBuildingConfigIds.AddRange(message.UnlockedBuildingConfigIds);
            homeComp.Buildings.Clear();
            homeComp.Buildings.AddRange(message.Buildings);

            // 2. 更新 HomeProductionComponent
            HomeProductionComponent prodComp = scene.GetComponent<HomeProductionComponent>();
            if (prodComp == null)
            {
                prodComp = scene.AddComponent<HomeProductionComponent>();
            }
            prodComp.Orders.Clear();
            prodComp.Orders.AddRange(message.ProductionOrders);

            // 3. 更新 HomeContractComponent
            HomeContractComponent contractComp = scene.GetComponent<HomeContractComponent>();
            if (contractComp == null)
            {
                contractComp = scene.AddComponent<HomeContractComponent>();
            }
            contractComp.AvailableContracts.Clear();
            contractComp.AvailableContracts.AddRange(message.AvailableContracts);
            contractComp.ActiveContracts.Clear();
            contractComp.ActiveContracts.AddRange(message.ActiveContracts);

            // 4. 发布事件刷新 UI
            EventSystem.Instance.Publish(scene, new HomeSnapshotRefresh());

            await ETTask.CompletedTask;
        }
    }
}
```

### 3.2 建筑变更推送

```csharp
// Scripts/Hotfix/Client/Handler/M2C_HomeBuildingChangedHandler.cs
namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_HomeBuildingChangedHandler : MessageHandler<Scene, M2C_HomeBuildResponse>
    {
        protected override async ETTask Run(Scene scene, M2C_HomeBuildResponse message)
        {
            if (message.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"Home build failed: {message.Error} {message.Message}");
                return;
            }

            PlayerHomeComponent homeComp = scene.GetComponent<PlayerHomeComponent>();
            if (homeComp == null) return;

            // 插入新建筑
            homeComp.Buildings.Add(message.Building);

            // 发布事件
            EventSystem.Instance.Publish(scene, new HomeBuildingChanged
            {
                BuildingId = message.Building.BuildingId,
                ChangeType = HomeBuildingChangeType.Build
            });

            await ETTask.CompletedTask;
        }
    }
}
```

### 3.3 生产/合同变更推送

同理，分别处理 `M2C_HomeProductionChangedHandler`、`M2C_HomeContractsChangedHandler`，更新对应客户端组件后发布事件。

### 3.4 事件定义

```csharp
// Scripts/Model/Client/HomeUIEvents.cs
namespace ET.Client
{
    // 基地快照刷新（整体刷新）
    public struct HomeSnapshotRefresh { }

    // 单个建筑变更
    public struct HomeBuildingChanged
    {
        public long BuildingId;
        public int ChangeType;  // HomeBuildingChangeType
    }

    // 生产订单变更
    public struct HomeProductionChanged
    {
        public long OrderId;
    }

    // 合同列表变更
    public struct HomeContractChanged { }
}

public static class HomeBuildingChangeType
{
    public const int Build = 1;
    public const int Upgrade = 2;
    public const int Demolish = 3;
    public const int Collect = 4;
}
```

---

## 四、LobbyPanel 基地页签集成

### 4.1 现有入口

LobbyPanel 已预留 Build 页签：

```
Inspector 绑定：u_ComBuildPanelRectTransform (RectTransform)
事件绑定：     u_EventBuildToggle → OnEventBuildToggleInvoke
ShowPanel 切换：self.u_ComBuildPanelRectTransform.gameObject.SetActive(true)
```

点击"基地"Tab → `OnEventBuildToggleInvoke` → `ShowPanel(u_ComBuildPanelRectTransform)` → 显示基地面板内容。

### 4.2 Build 面板内容区布局

在 `u_ComBuildPanelRectTransform` 下搭建以下 UI 结构：

```
BuildPanel (RectTransform)
├── TopBar
│   ├── ResourceDisplay        # 关键资源数量 (木材/金属/食物等)
│   └── RefreshTimer           # 悬赏合同刷新倒计时
├── SlotGrid                   # 建筑槽位网格
│   └── LoopScroll (HomeBuildSlotItem)
│       ├── SlotIcon           # 空位/建筑图标
│       ├── LevelText          # 等级文本
│       ├── StateIcon          # 可收取/生产中 等状态标记
│       └── SlotButton         # 点击交互
├── BuildingDetail             # 选中建筑详情区（右侧或底部）
│   ├── BuildingName
│   ├── BuildingLevel
│   ├── BuildingDesc
│   ├── PassiveOutputInfo      # 被动产出信息
│   └── ActionButtons
│       ├── BtnUpgrade         # 升级按钮
│       ├── BtnCollect         # 收取按钮
│       ├── BtnDemolish        # 拆除按钮
│       ├── BtnProduction      # 进入工坊
│       └── BtnContract        # 进入合同
└── EmptySlotDetail            # 空槽位详情（点击空位时显示）
    ├── BtnBuild               # 建造按钮 → 打开 HomeBuildView
    └── SlotTypeText           # 槽位类型描述
```

### 4.3 交互流程

```
用户点击"基地" Tab
  → ShowPanel(BuildPanel)
  → 读取 PlayerHomeComponent.Buildings 渲染槽位列表
  → 每个槽位显示建筑图标、等级、状态

用户点击空槽位
  → 显示 EmptySlotDetail
  → 点击"建造" → 打开 HomeBuildView（建造选择弹窗）

用户点击已有建筑槽位
  → 显示 BuildingDetail（建筑名、等级、产出信息）
  → 显示对应操作按钮：
    - 补给站/农场 → 收取按钮
    - 工坊 → 进入加工按钮
    - 情报站 → 进入悬赏合同按钮
    - 通用 → 升级/拆除按钮
```

---

## 五、Panel/View 实现清单

### 5.1 HomeBuildSlotItemComponent（槽位列表项，LoopScroll Item）

**ModelView/Component：**
```csharp
// Scripts/ModelView/Client/YIUIComponent/Home/HomeBuildSlotItemComponent.cs
namespace ET.Client
{
    public partial class HomeBuildSlotItemComponent : Entity
    {
        // YIUI 自动生成的 Inspector 绑定字段（在 Gen 层）
        // u_DataSlotId        : UIDataValueInt     — 槽位Id
        // u_DataBuildingName  : UIDataValueString  — 建筑名称（空位显示"空"）
        // u_DataLevel         : UIDataValueInt     — 建筑等级
        // u_DataIcon          : UIDataValueString  — 建筑图标资源名
        // u_DataHasBuilding   : UIDataValueBool    — 是否有建筑
        // u_DataCanCollect    : UIDataValueBool    — 是否可收取
        // u_EventSelect       : UITaskEventP0      — 点击事件
    }
}
```

**HotfixView/System：**
```csharp
// Scripts/HotfixView/Client/YIUISystem/Home/HomeBuildSlotItemComponentSystem.cs
namespace ET.Client
{
    [FriendOf(typeof(HomeBuildSlotItemComponent))]
    public static partial class HomeBuildSlotItemComponentSystem
    {
        // 由 LoopScroll 的 YIUILoopRenderer 调用，绑定数据
        public static void Refresh(this HomeBuildSlotItemComponent self,
            int slotId, HomeBuildingInfo building)
        {
            self.u_DataSlotId?.SetValue(slotId);
            if (building != null)
            {
                self.u_DataHasBuilding?.SetValue(true);
                self.u_DataBuildingName?.SetValue(GetBuildingName(building.ConfigId));
                self.u_DataLevel?.SetValue(building.Level);
                self.u_DataIcon?.SetValue(GetBuildingIcon(building.ConfigId));
                self.u_DataCanCollect?.SetValue(building.State == HomeBuildingState.Collecting);
            }
            else
            {
                self.u_DataHasBuilding?.SetValue(false);
                self.u_DataBuildingName?.SetValue("空槽位");
                self.u_DataLevel?.SetValue(0);
                self.u_DataIcon?.SetValue("");
                self.u_DataCanCollect?.SetValue(false);
            }
        }
    }
}
```

### 5.2 HomeBuildView（建造选择弹窗）

用户点击空槽位 → 打开此 View → 选择要建造的建筑类型。

**核心逻辑：**
```csharp
// Scripts/HotfixView/Client/YIUISystem/Home/HomeBuildViewComponentSystem.cs
namespace ET.Client
{
    [FriendOf(typeof(HomeBuildViewComponent))]
    public static partial class HomeBuildViewComponentSystem
    {
        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this HomeBuildViewComponent self)
        {
            // 从配置表读取可建造列表，按槽位类型过滤
            // 用 LoopScroll 展示
            await self.RefreshBuildList();
            return true;
        }

        // LoopScroll 渲染
        [EntitySystem]
        private static void YIUILoopRenderer(this HomeBuildViewComponent self,
            HomeBuildItemComponent item, BuildingConfig data, int index, bool select)
        {
            item.u_DataName?.SetValue(data.Name);
            item.u_DataIcon?.SetValue(data.Prefab);
            item.u_DataSelect?.SetValue(select);
            // 显示建造消耗
        }

        // 点击确认建造
        [YIUIInvoke(HomeBuildViewComponent.OnEventConfirmBuildInvoke)]
        private static async ETTask OnConfirmBuild(this HomeBuildViewComponent self)
        {
            if (self.SelectedConfigId <= 0) return;

            // 发送 C2M_HomeBuildRequest
            Scene scene = self.Scene();
            var response = await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeBuildRequest
                {
                    SlotId = self.TargetSlotId,
                    ConfigId = self.SelectedConfigId
                }) as M2C_HomeBuildResponse;

            if (response.Error != ErrorCode.ERR_Success)
            {
                // 显示错误提示
                Log.Warning($"Build failed: {response.Error}");
                return;
            }

            // M2C_HomeBuildResponse 会通过 Handler 更新数据并发布事件
            // UI 收到 HomeBuildingChanged 事件后自动刷新
            self.UIView.Close();
        }
    }
}
```

### 5.3 HomeProductionView（工坊加工弹窗）

**核心逻辑：**
```csharp
// 展示当前工坊的配方列表和进行中的订单
[EntitySystem]
private static async ETTask<bool> YIUIOpen(this HomeProductionViewComponent self)
{
    HomeProductionComponent prodComp = self.Scene().GetComponent<HomeProductionComponent>();

    // 读取可用配方 (从 HomeProductionConfig 表)
    await self.RefreshRecipeList();

    // 显示当前订单状态
    self.RefreshOrderStatus(prodComp);

    return true;
}

// 开始加工
private static async ETTask StartProduction(this HomeProductionViewComponent self, int recipeId)
{
    var response = await self.Scene().GetComponent<SessionComponent>().Session.Call(
        new C2M_HomeStartProductionRequest
        {
            BuildingId = self.BuildingId,
            RecipeId = recipeId
        }) as M2C_HomeStartProductionResponse;

    if (response.Error != ErrorCode.ERR_Success)
    {
        // 错误提示
        return;
    }

    // 刷新订单显示
    self.RefreshOrderStatus(self.Scene().GetComponent<HomeProductionComponent>());
}

// 收取产物
private static async ETTask CollectProduction(this HomeProductionViewComponent self, long orderId)
{
    var response = await self.Scene().GetComponent<SessionComponent>().Session.Call(
        new C2M_HomeCollectProductionRequest
        {
            OrderId = orderId
        }) as M2C_HomeCollectProductionResponse;

    if (response.Error != ErrorCode.ERR_Success) return;

    // 显示获得物品提示
    self.ShowRewardTips(response.ItemConfigIds, response.ItemCounts);
}
```

### 5.4 HomeContractView（悬赏合同弹窗）

**核心逻辑：**
```csharp
[EntitySystem]
private static async ETTask<bool> YIUIOpen(this HomeContractViewComponent self)
{
    HomeContractComponent contractComp = self.Scene().GetComponent<HomeContractComponent>();

    // 分两个区域展示：可接取 / 进行中
    await self.RefreshAvailableList(contractComp.AvailableContracts);
    await self.RefreshActiveList(contractComp.ActiveContracts);

    return true;
}

// 接取合同
private static async ETTask AcceptContract(this HomeContractViewComponent self, long contractId)
{
    var response = await self.Scene().GetComponent<SessionComponent>().Session.Call(
        new C2M_HomeAcceptContractRequest
        {
            ContractId = contractId
        }) as M2C_HomeAcceptContractResponse;

    if (response.Error != ErrorCode.ERR_Success) return;

    // M2C_HomeContractsChanged 推送后自动刷新
}

// 领取合同奖励
private static async ETTask CollectContractReward(this HomeContractViewComponent self, long contractId)
{
    var response = await self.Scene().GetComponent<SessionComponent>().Session.Call(
        new C2M_HomeCollectContractRewardRequest
        {
            ContractId = contractId
        }) as M2C_HomeCollectContractRewardResponse;

    if (response.Error != ErrorCode.ERR_Success) return;

    self.ShowRewardTips(response.ItemConfigIds, response.ItemCounts);
}
```

---

## 六、事件驱动刷新机制

### 6.1 事件订阅关系

```
M2C_HomeSnapshot
  → 更新客户端三大组件
  → 发布 HomeSnapshotRefresh
  → LobbyPanel.BuildPanel 整体刷新槽位列表

M2C_HomeBuildResponse
  → 更新 Buildings 列表
  → 发布 HomeBuildingChanged(Build)
  → LobbyPanel.BuildPanel 刷新对应槽位

M2C_HomeUpgradeResponse
  → 更新建筑等级
  → 发布 HomeBuildingChanged(Upgrade)
  → BuildingDetail 刷新等级显示

M2C_HomeDemolishResponse
  → 移除建筑数据
  → 发布 HomeBuildingChanged(Demolish)
  → LobbyPanel.BuildPanel 刷新对应槽位为空

M2C_HomeCollectResponse
  → 更新 LastCollectTime
  → 发布 HomeBuildingChanged(Collect)
  → 收取按钮状态更新 + 飘字提示

M2C_HomeContractsChanged
  → 更新合同列表
  → 发布 HomeContractChanged
  → HomeContractView 刷新列表
```

### 6.2 LobbyPanel 事件监听示例

```csharp
// 在 LobbyPanelComponentSystem.cs 中添加事件监听
[Event(SceneType.Client)]
public class HomeSnapshotRefreshHandler : AEvent<Scene, HomeSnapshotRefresh>
{
    protected override async ETTask Run(Scene scene, HomeSnapshotRefresh args)
    {
        // 找到 LobbyPanel 实例，刷新 BuildPanel 区域
        // 这里需要根据项目实际的 Panel 管理方式获取实例
        // 例如通过 scene.GetComponent<UIComponent>().GetPanel<LobbyPanelComponent>()

        await ETTask.CompletedTask;
    }
}

[Event(SceneType.Client)]
public class HomeBuildingChangedHandler : AEvent<Scene, HomeBuildingChanged>
{
    protected override async ETTask Run(Scene scene, HomeBuildingChanged args)
    {
        // 局部刷新变更的建筑槽位
        await ETTask.CompletedTask;
    }
}
```

---

## 七、请求发送工具方法

封装统一的请求发送，避免重复代码。

```csharp
// Scripts/Hotfix/Client/Helper/HomeRequestHelper.cs
namespace ET.Client
{
    public static class HomeRequestHelper
    {
        public static async ETTask<M2C_HomeBuildResponse> Build(Scene scene, int slotId, int configId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeBuildRequest { SlotId = slotId, ConfigId = configId }
            ) as M2C_HomeBuildResponse;
        }

        public static async ETTask<M2C_HomeUpgradeResponse> Upgrade(Scene scene, long buildingId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeUpgradeRequest { BuildingId = buildingId }
            ) as M2C_HomeUpgradeResponse;
        }

        public static async ETTask<M2C_HomeDemolishResponse> Demolish(Scene scene, long buildingId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeDemolishRequest { BuildingId = buildingId }
            ) as M2C_HomeDemolishResponse;
        }

        public static async ETTask<M2C_HomeCollectResponse> Collect(Scene scene, long buildingId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeCollectRequest { BuildingId = buildingId }
            ) as M2C_HomeCollectResponse;
        }

        public static async ETTask<M2C_HomeStartProductionResponse> StartProduction(
            Scene scene, long buildingId, int recipeId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeStartProductionRequest { BuildingId = buildingId, RecipeId = recipeId }
            ) as M2C_HomeStartProductionResponse;
        }

        public static async ETTask<M2C_HomeCollectProductionResponse> CollectProduction(
            Scene scene, long orderId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeCollectProductionRequest { OrderId = orderId }
            ) as M2C_HomeCollectProductionResponse;
        }

        public static async ETTask<M2C_HomeAcceptContractResponse> AcceptContract(
            Scene scene, long contractId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeAcceptContractRequest { ContractId = contractId }
            ) as M2C_HomeAcceptContractResponse;
        }

        public static async ETTask<M2C_HomeCollectContractRewardResponse> CollectContractReward(
            Scene scene, long contractId)
        {
            return await scene.GetComponent<SessionComponent>().Session.Call(
                new C2M_HomeCollectContractRewardRequest { ContractId = contractId }
            ) as M2C_HomeCollectContractRewardResponse;
        }
    }
}
```

---

## 八、Unity Prefab 搭建指南

### 8.1 YIUI Panel/View 创建步骤

1. 在 Unity 中使用 YIUI 工具创建 Panel/View Prefab
2. 在 Inspector 中绑定 UI 组件到 ComponentTable
3. 绑定按钮事件到 EventTable
4. 运行 YIUI 代码生成工具 → 自动产出 Gen 层代码
5. 手动编写 System 层业务逻辑

### 8.2 需要创建的 Prefab 列表

| Prefab 名称 | 类型 | 说明 |
|-------------|------|------|
| HomeBuildSlotItem | LoopScroll Item | 建筑槽位列表项 |
| HomeBuildView | View | 建造选择弹窗 |
| HomeBuildItem | LoopScroll Item | 可建造建筑列表项 |
| HomeProductionView | View | 工坊加工弹窗 |
| HomeRecipeItem | LoopScroll Item | 配方列表项 |
| HomeContractView | View | 悬赏合同弹窗 |
| HomeContractItem | LoopScroll Item | 合同列表项 |
| HomeRewardTips | View | 获得奖励飘字提示 |

### 8.3 Inspector 绑定规范

- 数据绑定用 `u_Data` 前缀：`u_DataName`, `u_DataLevel`, `u_DataIcon`
- 事件绑定用 `u_Event` 前缀：`u_EventSelect`, `u_EventConfirm`
- 组件引用用 `u_Com` 前缀：`u_ComScrollRect`, `u_ComDetailPanel`
- 值类型对应：
  - `UIDataValueString` — 文本
  - `UIDataValueInt` — 整数
  - `UIDataValueBool` — 开关/可见性
  - `UITaskEventP0` — 无参事件

---

## 九、文件创建清单

### 9.1 Model/Client（数据模型）

```
Scripts/Model/Client/PlayerHomeComponent.cs
Scripts/Model/Client/HomeProductionComponent.cs
Scripts/Model/Client/HomeContractComponent.cs
Scripts/Model/Client/HomeUIEvents.cs
```

### 9.2 Hotfix/Client（消息处理 + 工具）

```
Scripts/Hotfix/Client/Handler/M2C_HomeSnapshotHandler.cs
Scripts/Hotfix/Client/Handler/M2C_HomeBuildingChangedHandler.cs
Scripts/Hotfix/Client/Handler/M2C_HomeProductionChangedHandler.cs
Scripts/Hotfix/Client/Handler/M2C_HomeContractsChangedHandler.cs
Scripts/Hotfix/Client/System/PlayerHomeComponentSystem.cs
Scripts/Hotfix/Client/System/HomeProductionComponentSystem.cs
Scripts/Hotfix/Client/System/HomeContractComponentSystem.cs
Scripts/Hotfix/Client/Helper/HomeRequestHelper.cs
```

### 9.3 ModelView/Client（UI 组件定义 — 需配合 YIUI 工具生成）

```
Scripts/ModelView/Client/YIUIComponent/Home/HomeBuildSlotItemComponent.cs
Scripts/ModelView/Client/YIUIComponent/Home/HomeBuildViewComponent.cs
Scripts/ModelView/Client/YIUIComponent/Home/HomeBuildItemComponent.cs
Scripts/ModelView/Client/YIUIComponent/Home/HomeProductionViewComponent.cs
Scripts/ModelView/Client/YIUIComponent/Home/HomeRecipeItemComponent.cs
Scripts/ModelView/Client/YIUIComponent/Home/HomeContractViewComponent.cs
Scripts/ModelView/Client/YIUIComponent/Home/HomeContractItemComponent.cs
```

### 9.4 HotfixView/Client/YIUIGen（YIUI 工具自动生成，无需手写）

```
Scripts/HotfixView/Client/YIUIGen/Home/HomeBuildSlotItemComponentSystemGen.cs
Scripts/HotfixView/Client/YIUIGen/Home/HomeBuildViewComponentSystemGen.cs
... (其他 View/Item 对应的 Gen 文件)
```

### 9.5 HotfixView/Client/YIUISystem（业务逻辑，手写）

```
Scripts/HotfixView/Client/YIUISystem/Home/HomeBuildSlotItemComponentSystem.cs
Scripts/HotfixView/Client/YIUISystem/Home/HomeBuildViewComponentSystem.cs
Scripts/HotfixView/Client/YIUISystem/Home/HomeProductionViewComponentSystem.cs
Scripts/HotfixView/Client/YIUISystem/Home/HomeContractViewComponentSystem.cs
```

### 9.6 事件处理器

```
Scripts/HotfixView/Client/Event/HomeSnapshotRefreshHandler.cs
Scripts/HotfixView/Client/Event/HomeBuildingChangedHandler.cs
Scripts/HotfixView/Client/Event/HomeProductionChangedHandler.cs
Scripts/HotfixView/Client/Event/HomeContractChangedHandler.cs
```

---

## 十、实现顺序建议

### Step 1：客户端数据层
1. 创建 Model/Client 镜像组件（PlayerHomeComponent, HomeProductionComponent, HomeContractComponent）
2. 创建 HomeUIEvents.cs 事件定义
3. 创建 M2C Handler（Snapshot, BuildingChanged, ProductionChanged, ContractChanged）
4. 创建 HomeRequestHelper.cs
5. **验证**：编译通过

### Step 2：LobbyPanel 基地页签
1. 在 Unity 中搭建 BuildPanel 内容区 Prefab
2. 创建 HomeBuildSlotItem 的 Prefab + 三层代码
3. 在 LobbyPanelComponentSystem 中实现 BuildPanel 的数据加载和刷新
4. 订阅 HomeSnapshotRefresh 和 HomeBuildingChanged 事件
5. **验证**：进入 Home 后 BuildPanel 显示槽位列表

### Step 3：建造弹窗
1. 创建 HomeBuildView Prefab + 三层代码
2. 实现建造选择列表、确认建造逻辑
3. **验证**：点击空槽位 → 选择建筑 → 建造成功 → 槽位列表刷新

### Step 4：升级/拆除/收取
1. 在 BuildingDetail 区域实现升级、拆除、收取按钮逻辑
2. 调用 HomeRequestHelper 对应方法
3. **验证**：各操作功能正常

### Step 5：工坊加工弹窗
1. 创建 HomeProductionView Prefab + 三层代码
2. 实现配方列表、开始加工、收取产物
3. **验证**：加工流程完整

### Step 6：悬赏合同弹窗
1. 创建 HomeContractView Prefab + 三层代码
2. 实现合同列表、接取、领取奖励
3. **验证**：合同流程完整

---

## 附录：参考文件

| 参考内容 | 文件路径 |
|---------|---------|
| LobbyPanel 组件定义 | `cn.etetet.statesync/Scripts/ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.cs` |
| LobbyPanel Gen | `cn.etetet.statesync/Scripts/HotfixView/Client/YIUIGen/Lobby/LobbyPanelComponentSystemGen.cs` |
| LobbyPanel 逻辑 | `cn.etetet.statesync/Scripts/HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs` |
| EquipSelectView (子View示例) | `cn.etetet.statesync/Scripts/.../Lobby/EquipSelectView*` |
| M2C Item Handler | `cn.etetet.item/Scripts/Hotfix/Client/M2C_UpdateItemHandler.cs` |
| M2C SyncBagData Handler | `cn.etetet.item/Scripts/Hotfix/Client/M2C_SyncBagDataHandler.cs` |
| Home Proto 定义 | `cn.etetet.home/Proto/Home_C_15600.proto` |
| 技术总方案 | `C:\Users\luxinyu\.claude\plans\bubbly-floating-lantern.md` |
