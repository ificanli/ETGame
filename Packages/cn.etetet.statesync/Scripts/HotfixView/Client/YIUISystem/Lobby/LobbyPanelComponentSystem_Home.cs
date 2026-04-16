using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private const string HOME_RUNTIME_ROOT_NAME = "HomeRuntimeRoot";
        private const string HOME_BUILD_ENTRY_ROW_NAME = "HomeBuildEntryRow";
        private const string HOME_BUILD_ENTRY_HINT_NAME = "HomeBuildEntryHint";
        private const string HOME_BUILDING_LIST_CONTENT_NAME = "HomeBuildingListContent";
        private const string HOME_BUILDING_LIST_TITLE_NAME = "HomeBuildingListTitle";
        private const string HOME_SUBTITLE_TEXT_NAME = "HomeSubtitleText";
        private const string HOME_DETAIL_TITLE_NAME = "HomeDetailTitleText";
        private const string HOME_DETAIL_SUBTITLE_NAME = "HomeDetailSubtitleText";
        private const string HOME_DETAIL_STATUS_NAME = "HomeDetailStatusText";
        private const string HOME_DETAIL_HINT_NAME = "HomeDetailHintText";
        private const string HOME_QUICK_EQUIP_BUTTON_NAME = "HomeQuickEquipButton";
        private const string HOME_QUICK_MATCH_BUTTON_NAME = "HomeQuickMatchButton";
        private const string HOME_UPGRADE_BUTTON_NAME = "HomeUpgradeButton";
        private const string HOME_COLLECT_BUTTON_NAME = "HomeCollectButton";
        private const string HOME_DEMOLISH_BUTTON_NAME = "HomeDemolishButton";
        private const string HOME_FONT_RESOURCE_PATH = "Fonts/HomeChinese SDF";
        private const int HOME_SUBVIEW_OVERVIEW = 0;
        private const int HOME_SUBVIEW_MAIN_CITY_TASKS = 1;
        private const int HOME_SUBVIEW_MUSEUM_MANAGE = 2;

        public static void RefreshHomeUiNow(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.LastHomeSnapshot = string.Empty;
            self.TryRefreshHomeUi(true);
        }

        private static void TryRefreshHomeUi(this LobbyPanelComponent self, bool force)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            RectTransform buildPanel = self.u_ComBuildPanelRectTransform;
            if (buildPanel == null || !buildPanel.gameObject.activeInHierarchy)
            {
                return;
            }

            Scene root = self.Root();
            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            bool inHome = IsInHomeScene(root);
            string snapshot = BuildHomeSnapshot(
                runtime,
                inHome,
                self.SelectedHomeBuildingId,
                self.SelectedHomeSlotId,
                self.HomeSubViewMode,
                self.HomeSubViewBuildingId);
            if (!force && self.LastHomeSnapshot == snapshot)
            {
                return;
            }

            self.LastHomeSnapshot = snapshot;
            self.EnsureHomeRuntimeUi();
            if (self.HomeRuntimeRoot == null)
            {
                return;
            }

            self.RenderHomeUi(runtime, inHome);
        }

        private static string BuildHomeSnapshot(
            HomeClientComponent runtime,
            bool inHome,
            long selectedBuildingId,
            int selectedSlotId,
            int subViewMode,
            long subViewBuildingId)
        {
            StringBuilder builder = new StringBuilder(768);
            builder.Append(inHome ? '1' : '0').Append('|');
            builder.Append(selectedBuildingId).Append('|');
            builder.Append(selectedSlotId).Append('|');
            builder.Append(subViewMode).Append('|');
            builder.Append(subViewBuildingId).Append('|');

            if (runtime == null)
            {
                return builder.ToString();
            }

            builder.Append(runtime.HomeVersion).Append('|');
            builder.Append(runtime.LastSettleTime).Append('|');
            builder.Append(runtime.TotalWealth).Append('|');
            builder.Append(runtime.MainCitySummary?.Level ?? 0).Append(':')
                    .Append(runtime.MainCitySummary?.TaskGroupId ?? 0).Append(':')
                    .Append(runtime.MainCitySummary?.UnlockedSlotCount ?? 0).Append(':')
                    .Append(runtime.MainCitySummary?.OtherBuildingMaxLevel ?? 0).Append(':')
                    .Append(runtime.MainCitySummary?.TaskFinishedCount ?? 0).Append(':')
                    .Append(runtime.MainCitySummary?.TaskTotalCount ?? 0).Append(':')
                    .Append((runtime.MainCitySummary?.CanUpgrade ?? false) ? 1 : 0).Append('|');
            builder.Append(runtime.WarehouseSummary?.Capacity ?? 0).Append(':')
                    .Append(runtime.WarehouseSummary?.OccupiedCellCount ?? 0).Append(':')
                    .Append(runtime.WarehouseSummary?.ItemCount ?? 0).Append('|');
            builder.Append(runtime.ProductionOrders.Count).Append('|');
            builder.Append(runtime.MainCityTasks.Count).Append('|');
            builder.Append(runtime.MuseumDisplays.Count).Append('|');
            builder.Append(runtime.AvailableContracts.Count).Append('|');
            builder.Append(runtime.ActiveContracts.Count).Append('|');

            HomeClientBuildingData selectedBuilding = runtime.GetBuilding(selectedBuildingId);
            if (selectedBuilding == null && selectedSlotId > 0)
            {
                HomeClientSlotData selectedSlot = runtime.GetSlot(selectedSlotId);
                if (selectedSlot != null && selectedSlot.BuildingId > 0)
                {
                    selectedBuilding = runtime.GetBuilding(selectedSlot.BuildingId);
                }
            }

            int selectedBuildingType = GetHomeBuildingType(selectedBuilding?.ConfigId ?? 0);
            if (selectedBuildingType == HomeBuildingType.Farm || selectedBuildingType == HomeBuildingType.RecycleRoom)
            {
                builder.Append(TimeInfo.Instance.ServerNow() / 1000).Append('|');
            }

            for (int i = 0; i < runtime.UnlockedBuildingConfigIds.Count; ++i)
            {
                builder.Append(runtime.UnlockedBuildingConfigIds[i]).Append(',');
            }

            builder.Append('|');

            for (int i = 0; i < runtime.Slots.Count; ++i)
            {
                HomeClientSlotData slot = runtime.Slots[i];
                if (slot == null)
                {
                    continue;
                }

                builder.Append(slot.SlotId).Append(':')
                        .Append(slot.Unlocked ? 1 : 0).Append(':')
                        .Append(slot.SortOrder).Append(':')
                        .Append(slot.BuildingId).Append(':')
                        .Append(slot.BuildingConfigId).Append(':');

                for (int j = 0; j < slot.CanBuildTypes.Count; ++j)
                {
                    builder.Append(slot.CanBuildTypes[j]).Append(',');
                }

                builder.Append('|');
            }

            for (int i = 0; i < runtime.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = runtime.ProductionOrders[i];
                if (order == null)
                {
                    continue;
                }

                builder.Append(order.OrderId).Append(':')
                        .Append(order.BuildingEntityId).Append(':')
                        .Append(order.RecipeId).Append(':')
                        .Append(order.State).Append(':')
                        .Append(order.StartTime).Append(':')
                        .Append(order.FinishTime).Append('|');
            }

            for (int i = 0; i < runtime.MainCityTasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = runtime.MainCityTasks[i];
                if (task == null)
                {
                    continue;
                }

                builder.Append(task.TaskId).Append(':')
                        .Append(task.Progress).Append(':')
                        .Append(task.Target).Append(':')
                        .Append(task.Completed ? 1 : 0).Append('|');
            }

            for (int i = 0; i < runtime.MuseumDisplays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = runtime.MuseumDisplays[i];
                if (display == null)
                {
                    continue;
                }

                builder.Append(display.DisplayId).Append(':')
                        .Append(display.BuildingId).Append(':')
                        .Append(display.SlotIndex).Append(':')
                        .Append(display.ItemConfigId).Append('|');
            }

            for (int i = 0; i < runtime.Buildings.Count; ++i)
            {
                HomeClientBuildingData building = runtime.Buildings[i];
                if (building == null)
                {
                    continue;
                }

                builder.Append(building.BuildingId).Append(':')
                        .Append(building.ConfigId).Append(':')
                        .Append(building.Level).Append(':')
                        .Append(building.State).Append(':')
                        .Append(building.SlotId).Append(':')
                        .Append(building.LastCollectTime).Append(':')
                        .Append(building.LastProductionTime).Append('|');
            }

            return builder.ToString();
        }

        private static void EnsureHomeRuntimeUi(this LobbyPanelComponent self)
        {
            RectTransform buildPanel = self.u_ComBuildPanelRectTransform;
            if (buildPanel == null)
            {
                return;
            }

            self.HomeRuntimeRoot = FindDescendantRectTransform(buildPanel, HOME_RUNTIME_ROOT_NAME);
            if (self.HomeRuntimeRoot == null)
            {
                return;
            }

            self.ResolveHomeRuntimeRefs();
            self.BindHomeButtons();

            for (int i = 0; i < buildPanel.childCount; ++i)
            {
                if (buildPanel.GetChild(i) is not RectTransform child)
                {
                    continue;
                }

                if (child == self.HomeRuntimeRoot)
                {
                    child.gameObject.SetActive(true);
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private static void RenderHomeUi(this LobbyPanelComponent self, HomeClientComponent runtime, bool inHome)
        {
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData selectedBuilding = ResolveBuildingInSlot(selectedSlot, buildings);
            NormalizeHomeSubViewMode(self, selectedBuilding);

            if (self.HomeHeaderSubtitleText != null)
            {
                self.HomeHeaderSubtitleText.text = inHome
                    ? $"当前已进入家园场景。主城等级 {runtime?.MainCitySummary?.Level ?? 0}，金币 {runtime?.TotalWealth ?? 0}，仓库 {runtime?.WarehouseSummary?.OccupiedCellCount ?? 0}/{runtime?.WarehouseSummary?.Capacity ?? 0}。"
                    : "当前不在家园场景，建造页仅保留家园首页承接结构。";
            }

            RefreshOverviewStats(self, runtime, buildings, selectedBuilding);
            RefreshPrototypeButtons(self, runtime, selectedSlot, selectedBuilding, inHome);
            switch (self.HomeSubViewMode)
            {
                case HOME_SUBVIEW_MAIN_CITY_TASKS:
                    self.RenderMainCityTaskCards(runtime, selectedBuilding);
                    break;
                case HOME_SUBVIEW_MUSEUM_MANAGE:
                    self.RenderMuseumManageCards(runtime, selectedBuilding, inHome);
                    break;
                default:
                    self.RenderSlotCards(runtime, buildings, inHome);
                    break;
            }

            self.RefreshHomeDetail(selectedSlot, selectedBuilding, inHome, runtime);

            if (self.HomeRuntimeRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(self.HomeRuntimeRoot);
            }
        }

        private static void NormalizeHomeSubViewMode(this LobbyPanelComponent self, HomeClientBuildingData selectedBuilding)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (self.HomeSubViewMode == HOME_SUBVIEW_MAIN_CITY_TASKS)
            {
                if (selectedBuilding == null || selectedBuilding.BuildingId != self.HomeSubViewBuildingId || GetHomeBuildingType(selectedBuilding.ConfigId) != HomeBuildingType.MainCity)
                {
                    self.HomeSubViewMode = HOME_SUBVIEW_OVERVIEW;
                    self.HomeSubViewBuildingId = 0;
                }

                return;
            }

            if (self.HomeSubViewMode == HOME_SUBVIEW_MUSEUM_MANAGE)
            {
                if (selectedBuilding == null || selectedBuilding.BuildingId != self.HomeSubViewBuildingId || GetHomeBuildingType(selectedBuilding.ConfigId) != HomeBuildingType.Museum)
                {
                    self.HomeSubViewMode = HOME_SUBVIEW_OVERVIEW;
                    self.HomeSubViewBuildingId = 0;
                }
            }
        }

        private static List<HomeClientBuildingData> BuildSortedHomeBuildings(HomeClientComponent runtime)
        {
            List<HomeClientBuildingData> result = new();
            if (runtime == null)
            {
                return result;
            }

            for (int i = 0; i < runtime.Buildings.Count; ++i)
            {
                if (runtime.Buildings[i] != null)
                {
                    result.Add(runtime.Buildings[i]);
                }
            }

            result.Sort(static (a, b) =>
            {
                int slotCompare = a.SlotId.CompareTo(b.SlotId);
                return slotCompare != 0 ? slotCompare : a.BuildingId.CompareTo(b.BuildingId);
            });
            return result;
        }

        private static async ETTask BuildHomePrototypeAsync(this LobbyPanelComponent self, int configId)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行建造。");
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            if (configId <= 0)
            {
                ShowHomeTips(root, "当前没有可用的原型建筑配置。");
                return;
            }

            HomeClientSlotData slot = runtime.GetSlot(self.SelectedHomeSlotId);
            if (slot == null)
            {
                ShowHomeTips(root, "请先在下方选择一个空槽位，再执行建造。");
                return;
            }

            if (!slot.Unlocked)
            {
                ShowHomeTips(root, "当前槽位尚未解锁，无法建造。");
                return;
            }

            if (slot.BuildingId > 0)
            {
                ShowHomeTips(root, "当前槽位已有建筑，请先选择空槽位。");
                return;
            }

            if (!CanBuildInSlot(slot, configId))
            {
                ShowHomeTips(root, "当前槽位不能建造该建筑。");
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            int slotId = slot.SlotId;
            M2C_HomeBuildResponse response = await HomeClientRequestHelper.Build(root, slotId, configId);

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "建造失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("建造失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyBuildResponse(root, response);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedHomeBuildingId = response.Building?.BuildingId ?? 0;
            self.SelectedHomeSlotId = slotId;
            self.RefreshHomeUiNow();
            ShowHomeTips(root, $"建造完成：{GetHomeBuildingName(configId)} 已落到槽位 {slotId}。");
        }

        private static async ETTask UpgradeSelectedHomeBuildingAsync(this LobbyPanelComponent self)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行升级。");
                return;
            }

            HomeClientComponent runtime = root.GetComponent<HomeClientComponent>();
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData building = ResolveBuildingInSlot(selectedSlot, buildings);
            if (building == null)
            {
                ShowHomeTips(root, "当前没有可升级的建筑。");
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            long buildingId = building.BuildingId;
            M2C_HomeUpgradeResponse response = await HomeClientRequestHelper.Upgrade(root, buildingId);

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "升级失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("升级失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyUpgradeResponse(root, response);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedHomeBuildingId = buildingId;
            self.SelectedHomeSlotId = building.SlotId;
            self.RefreshHomeUiNow();
        }

        private static async ETTask CollectSelectedHomeBuildingAsync(this LobbyPanelComponent self)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行收取。");
                return;
            }

            HomeClientComponent runtime = root.GetComponent<HomeClientComponent>();
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData building = ResolveBuildingInSlot(selectedSlot, buildings);
            if (building == null)
            {
                ShowHomeTips(root, "当前没有可收取的建筑。");
                return;
            }

            int buildingType = GetHomeBuildingType(building.ConfigId);
            switch (buildingType)
            {
                case HomeBuildingType.MainCity:
                    self.HomeSubViewMode = HOME_SUBVIEW_MAIN_CITY_TASKS;
                    self.HomeSubViewBuildingId = building.BuildingId;
                    self.SelectedHomeBuildingId = building.BuildingId;
                    self.SelectedHomeSlotId = building.SlotId;
                    self.RefreshHomeUiNow();
                    return;
                case HomeBuildingType.Museum:
                    self.HomeSubViewMode = HOME_SUBVIEW_MUSEUM_MANAGE;
                    self.HomeSubViewBuildingId = building.BuildingId;
                    self.SelectedHomeBuildingId = building.BuildingId;
                    self.SelectedHomeSlotId = building.SlotId;
                    self.RefreshHomeUiNow();
                    return;
                case HomeBuildingType.Warehouse:
                    self.ShowPanel(self.u_ComEquipPanelRectTransform);
                    self.SwitchLoadoutContentTab(true);
                    await self.RefreshHeroList();
                    return;
                case HomeBuildingType.RecycleRoom:
                    await self.ExecuteRecycleActionAsync(root, runtime, building);
                    return;
                case HomeBuildingType.Farm:
                    break;
                default:
                    ShowHomeTips(root, "当前建筑没有可执行的收取动作。");
                    return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            long buildingId = building.BuildingId;
            M2C_HomeCollectResponse response;
            try
            {
                response = await HomeClientRequestHelper.Collect(root, buildingId);
            }
            catch (RpcException exception)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, BuildHomeErrorText("收取失败", exception.Error, null));
                return;
            }
            catch (Exception exception)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, "收取失败：网络或状态异常，请稍后重试。");
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "收取失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("收取失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyCollect(root, buildingId);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedHomeBuildingId = buildingId;
            self.SelectedHomeSlotId = building.SlotId;
            self.RefreshHomeUiNow();
            ShowHomeTips(root, BuildCollectSuccessText(response));
        }

        private static async ETTask ExecuteRecycleActionAsync(
            this LobbyPanelComponent self,
            Scene root,
            HomeClientComponent runtime,
            HomeClientBuildingData building)
        {
            long readyOrderId = GetFirstReadyRecycleOrderId(runtime, building.BuildingId);
            if (readyOrderId > 0)
            {
                EntityRef<LobbyPanelComponent> selfRef = self;
                EntityRef<Scene> rootRef = root;
                M2C_HomeCollectProductionResponse response;
                try
                {
                    response = await HomeClientRequestHelper.CollectProduction(root, readyOrderId);
                }
                catch (RpcException exception)
                {
                    root = rootRef;
                    if (root == null || root.IsDisposed)
                    {
                        return;
                    }

                    Log.Error(exception);
                    ShowHomeTips(root, BuildHomeErrorText("收取失败", exception.Error, null));
                    return;
                }
                catch (Exception exception)
                {
                    root = rootRef;
                    if (root == null || root.IsDisposed)
                    {
                        return;
                    }

                    Log.Error(exception);
                    ShowHomeTips(root, "收取失败：网络或状态异常，请稍后重试。");
                    return;
                }

                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                if (response == null)
                {
                    ShowHomeTips(root, "收取失败：未收到服务端响应。");
                    return;
                }

                if (response.Error != ErrorCode.ERR_Success)
                {
                    ShowHomeTips(root, BuildHomeErrorText("收取失败", response.Error, response.Message));
                    return;
                }

                HomeClientHelper.ApplyCollectProduction(root, readyOrderId);
                self = selfRef;
                if (self == null || self.IsDisposed)
                {
                    return;
                }

                await self.RefreshHomeLoadoutSnapshotAsync();
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                self = selfRef;
                if (self == null || self.IsDisposed)
                {
                    return;
                }

                self.SelectedHomeBuildingId = building.BuildingId;
                self.SelectedHomeSlotId = building.SlotId;
                self.RefreshHomeUiNow();
                ShowHomeTips(root, BuildCollectProductionSuccessText(response));
                return;
            }

            int sourceItemConfigId = ResolveHomeRecycleSourceItemConfig(root);
            if (sourceItemConfigId <= 0)
            {
                ShowHomeTips(root, "仓库里没有可放入回收间的物品。");
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef2 = self;
            EntityRef<Scene> rootRef2 = root;
            M2C_HomeStartProductionResponse startResponse;
            try
            {
                startResponse = await HomeClientRequestHelper.StartProduction(root, building.BuildingId, sourceItemConfigId);
            }
            catch (RpcException exception)
            {
                root = rootRef2;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, BuildHomeErrorText("开始回收失败", exception.Error, null));
                return;
            }
            catch (Exception exception)
            {
                root = rootRef2;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, "开始回收失败：网络或状态异常，请稍后重试。");
                return;
            }

            root = rootRef2;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (startResponse == null)
            {
                ShowHomeTips(root, "开始回收失败：未收到服务端响应。");
                return;
            }

            if (startResponse.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("开始回收失败", startResponse.Error, startResponse.Message));
                return;
            }

            HomeClientHelper.ApplyStartProductionResponse(root, startResponse);
            self = selfRef2;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef2;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef2;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedHomeBuildingId = building.BuildingId;
            self.SelectedHomeSlotId = building.SlotId;
            self.RefreshHomeUiNow();
            ShowHomeTips(root, $"已放入 {GetHomeItemDisplayName(sourceItemConfigId)}，开始回收。");
        }

        private static async ETTask RefreshHomeLoadoutSnapshotAsync(this LobbyPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHeroList();
        }

        private static async ETTask PlaceMuseumDisplayAsync(this LobbyPanelComponent self, long buildingId, long itemUid)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再管理收藏馆。");
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeMuseumPlaceResponse response;
            try
            {
                response = await HomeClientRequestHelper.MuseumPlace(root, buildingId, itemUid);
            }
            catch (RpcException exception)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, BuildHomeErrorText("摆放失败", exception.Error, null));
                return;
            }
            catch (Exception exception)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, "摆放失败：网络或状态异常，请稍后重试。");
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "摆放失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("摆放失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyMuseumPlaceResponse(root, response);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            HomeClientBuildingData building = runtime.GetBuilding(buildingId);
            self.HomeSubViewMode = HOME_SUBVIEW_MUSEUM_MANAGE;
            self.HomeSubViewBuildingId = buildingId;
            self.SelectedHomeBuildingId = buildingId;
            self.SelectedHomeSlotId = building?.SlotId ?? self.SelectedHomeSlotId;
            self.RefreshHomeUiNow();
            ShowHomeTips(root, $"已将 {GetHomeItemDisplayName(response.ItemConfigId)} 摆入收藏馆。");
        }

        private static async ETTask TakeDownMuseumDisplayAsync(this LobbyPanelComponent self, long displayId, long buildingId)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再管理收藏馆。");
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeMuseumTakeDownResponse response;
            try
            {
                response = await HomeClientRequestHelper.MuseumTakeDown(root, displayId);
            }
            catch (RpcException exception)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, BuildHomeErrorText("取下失败", exception.Error, null));
                return;
            }
            catch (Exception exception)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                Log.Error(exception);
                ShowHomeTips(root, "取下失败：网络或状态异常，请稍后重试。");
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "取下失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("取下失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyMuseumTakeDownResponse(root, response);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            HomeClientBuildingData building = runtime.GetBuilding(buildingId);
            self.HomeSubViewMode = HOME_SUBVIEW_MUSEUM_MANAGE;
            self.HomeSubViewBuildingId = buildingId;
            self.SelectedHomeBuildingId = buildingId;
            self.SelectedHomeSlotId = building?.SlotId ?? self.SelectedHomeSlotId;
            self.RefreshHomeUiNow();
            ShowHomeTips(root, $"已将 {GetHomeItemDisplayName(response.ItemConfigId)} 取下并放回仓库。");
        }

        private static async ETTask DemolishSelectedHomeBuildingAsync(this LobbyPanelComponent self)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行拆除。");
                return;
            }

            HomeClientComponent runtime = root.GetComponent<HomeClientComponent>();
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData building = ResolveBuildingInSlot(selectedSlot, buildings);
            if (building == null)
            {
                ShowHomeTips(root, "当前没有可拆除的建筑。");
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            long buildingId = building.BuildingId;
            M2C_HomeDemolishResponse response = await HomeClientRequestHelper.Demolish(root, buildingId);

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "拆除失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("拆除失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyDemolish(root, buildingId);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync();
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedHomeBuildingId = 0;
            self.SelectedHomeSlotId = building.SlotId;
            self.HomeSubViewMode = HOME_SUBVIEW_OVERVIEW;
            self.HomeSubViewBuildingId = 0;
            self.RefreshHomeUiNow();
        }

        private static bool IsInHomeScene(Scene root)
        {
            Scene currentScene = root?.CurrentScene();
            return currentScene != null && !currentScene.IsDisposed && currentScene.Name.GetSceneConfigName() == "Home";
        }

        private static void ResolveHomeRuntimeRefs(this LobbyPanelComponent self)
        {
            RectTransform root = self.HomeRuntimeRoot;
            if (root == null)
            {
                return;
            }

            self.HomeBuildEntryRow ??= FindDescendantRectTransform(root, HOME_BUILD_ENTRY_ROW_NAME);
            self.HomeBuildingListContent ??= FindDescendantRectTransform(root, HOME_BUILDING_LIST_CONTENT_NAME);
            self.HomeHeaderSubtitleText ??= FindDescendantText(root, HOME_SUBTITLE_TEXT_NAME);
            self.HomeBuildEntryHintText ??= FindDescendantText(root, HOME_BUILD_ENTRY_HINT_NAME);
            self.HomeBuildingListTitleText ??= FindDescendantText(root, HOME_BUILDING_LIST_TITLE_NAME);
            self.HomeDetailTitleText ??= FindDescendantText(root, HOME_DETAIL_TITLE_NAME);
            self.HomeDetailSubtitleText ??= FindDescendantText(root, HOME_DETAIL_SUBTITLE_NAME);
            self.HomeDetailStatusText ??= FindDescendantText(root, HOME_DETAIL_STATUS_NAME);
            self.HomeDetailHintText ??= FindDescendantText(root, HOME_DETAIL_HINT_NAME);
            self.HomeQuickEquipButton ??= FindDescendantButton(root, HOME_QUICK_EQUIP_BUTTON_NAME);
            self.HomeQuickMatchButton ??= FindDescendantButton(root, HOME_QUICK_MATCH_BUTTON_NAME);
            self.HomeUpgradeButton ??= FindDescendantButton(root, HOME_UPGRADE_BUTTON_NAME);
            self.HomeCollectButton ??= FindDescendantButton(root, HOME_COLLECT_BUTTON_NAME);
            self.HomeDemolishButton ??= FindDescendantButton(root, HOME_DEMOLISH_BUTTON_NAME);

            RectTransform overviewRoot = FindDescendantRectTransform(root, "HomeOverviewGrid");
            if (overviewRoot == null)
            {
                return;
            }

            for (int i = 0; i < self.HomeStatValueTexts.Length && self.HomeStatValueTexts[i] == null; ++i)
            {
                self.HomeStatLabelTexts[i] = FindDescendantText(overviewRoot, $"HomeStatLabelText_{i}");
                self.HomeStatValueTexts[i] = FindDescendantText(overviewRoot, $"HomeStatValueText_{i}");
            }
        }

        private static void BindHomeButtons(this LobbyPanelComponent self)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;

            BindButton(self.HomeQuickEquipButton, selfRef, static panel =>
            {
                panel.CloseBattleRecordOverlay();
                panel.ShowPanel(panel.u_ComEquipPanelRectTransform);
                panel.RefreshLoadoutContentTabUi();
                panel.TryRefreshLoadoutUi(true);
            });

            BindButton(self.HomeQuickMatchButton, selfRef, static panel =>
            {
                panel.CloseBattleRecordOverlay();
                panel.ShowPanel(panel.u_ComMatchPanelRectTransform);
                panel.BindMatchModeButtons();
                panel.RefreshMatchModeSelection();
            });

            self.HomeUpgradeButton?.onClick.RemoveAllListeners();
            self.HomeUpgradeButton?.onClick.AddListener(() =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                panel.UpgradeSelectedHomeBuildingAsync().Coroutine();
            });

            self.HomeCollectButton?.onClick.RemoveAllListeners();
            self.HomeCollectButton?.onClick.AddListener(() =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                panel.CollectSelectedHomeBuildingAsync().Coroutine();
            });

            self.HomeDemolishButton?.onClick.RemoveAllListeners();
            self.HomeDemolishButton?.onClick.AddListener(() =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                panel.DemolishSelectedHomeBuildingAsync().Coroutine();
            });
        }

        private static void BindButton(Button button, EntityRef<LobbyPanelComponent> selfRef, Action<LobbyPanelComponent> action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                action(panel);
            });
        }

        private static void ShowHomeTips(Scene root, string text)
        {
            if (root == null || root.IsDisposed || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            TipsHelper.OpenSync<TipsTextViewComponent>(root, text);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static TMP_Text CreateText(
            LobbyPanelComponent self,
            string name,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            FontStyles fontStyles)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(HOME_FONT_RESOURCE_PATH);
            if (fontAsset != null)
            {
                tmp.font = fontAsset;
                tmp.fontSharedMaterial = fontAsset.material;
            }

            TMP_Text template = ResolveHomeTextTemplate(self);
            if (fontAsset == null && template != null)
            {
                tmp.font = template.font;
                tmp.fontSharedMaterial = template.fontSharedMaterial;
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = fontStyles;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateActionButton(
            LobbyPanelComponent self,
            string name,
            Transform parent,
            string text,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            layout.flexibleWidth = 1f;

            Image image = go.GetComponent<Image>();
            image.color = color;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color * 1.08f;
            colors.pressedColor = color * 0.92f;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.25f, 0.28f, 0.32f, 0.72f);
            button.colors = colors;
            button.targetGraphic = image;

            TMP_Text label = CreateText(self, "Label", go.transform, text, 20f, Color.white, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 6f);
            labelRect.offsetMax = new Vector2(-10f, -6f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        private static TMP_Text ResolveHomeTextTemplate(LobbyPanelComponent self)
        {
            TMP_Text template = self?.u_ComBuildPanelRectTransform?.GetComponentInChildren<TMP_Text>(true);
            template ??= self?.u_ComEquipPanelRectTransform?.GetComponentInChildren<TMP_Text>(true);
            template ??= self?.u_ComRolePanelRectTransform?.GetComponentInChildren<TMP_Text>(true);
            return template;
        }

        private static TMP_Text FindDescendantText(RectTransform parent, params string[] names)
        {
            RectTransform rect = FindDescendantRectTransform(parent, names);
            return rect?.GetComponent<TMP_Text>();
        }

        private static Button FindDescendantButton(RectTransform parent, params string[] names)
        {
            RectTransform rect = FindDescendantRectTransform(parent, names);
            return rect?.GetComponent<Button>();
        }

        private static void RefreshOverviewStats(
            this LobbyPanelComponent self,
            HomeClientComponent runtime,
            List<HomeClientBuildingData> buildings,
            HomeClientBuildingData selectedBuilding)
        {
            if (self.HomeBuildingListTitleText != null)
            {
                self.HomeBuildingListTitleText.text = self.HomeSubViewMode switch
                {
                    HOME_SUBVIEW_MAIN_CITY_TASKS => "主城任务",
                    HOME_SUBVIEW_MUSEUM_MANAGE => $"收藏馆管理 / {GetHomeBuildingName(selectedBuilding?.ConfigId ?? 0)}",
                    _ => "建筑与槽位",
                };
            }

            for (int i = 0; i < self.HomeStatLabelTexts.Length; ++i)
            {
                TMP_Text label = self.HomeStatLabelTexts[i];
                if (label != null)
                {
                    label.text = GetHomeOverviewStatLabel(i);
                }
            }

            if (self.HomeStatValueTexts.Length >= 4)
            {
                SetStatText(self.HomeStatValueTexts[0], buildings?.Count ?? 0);
                SetStatText(self.HomeStatValueTexts[1], runtime?.MainCitySummary?.Level ?? 0);
                SetStatText(
                    self.HomeStatValueTexts[2],
                    $"{runtime?.WarehouseSummary?.OccupiedCellCount ?? 0}/{runtime?.WarehouseSummary?.Capacity ?? 0}");
                SetStatText(self.HomeStatValueTexts[3], runtime?.TotalWealth ?? 0);
            }
        }

        private static void RefreshPrototypeButtons(
            this LobbyPanelComponent self,
            HomeClientComponent runtime,
            HomeClientSlotData selectedSlot,
            HomeClientBuildingData selectedBuilding,
            bool inHome)
        {
            RectTransform row = self.HomeBuildEntryRow;
            if (row == null)
            {
                return;
            }

            if (self.HomeSubViewMode != HOME_SUBVIEW_OVERVIEW)
            {
                if (self.HomeBuildEntryHintText != null)
                {
                    self.HomeBuildEntryHintText.text = self.HomeSubViewMode == HOME_SUBVIEW_MAIN_CITY_TASKS
                        ? "当前为主城任务页，可查看任务进度并返回家园概览。"
                        : "当前为收藏馆管理页，可直接摆放或取下展示单位。";
                }

                ClearChildren(row);
                self.HomeBuildPrototypeButtons.Clear();
                Button backButton = CreateActionButton(
                    self,
                    "HomeBackToOverviewButton",
                    row,
                    "返回家园概览",
                    new Color(0.24f, 0.3f, 0.36f, 0.96f));
                EntityRef<LobbyPanelComponent> backSelfRef = self;
                backButton.onClick.AddListener(() =>
                {
                    LobbyPanelComponent panel = backSelfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.HomeSubViewMode = HOME_SUBVIEW_OVERVIEW;
                    panel.HomeSubViewBuildingId = 0;
                    panel.RefreshHomeUiNow();
                });
                self.HomeBuildPrototypeButtons.Add(backButton);
                return;
            }

            List<int> configIds = runtime != null ? runtime.GetBuildablePrototypeConfigIds() : new List<int>();
            if (self.HomeBuildEntryHintText != null)
            {
                self.HomeBuildEntryHintText.text = BuildHomeBuildEntryHint(self, selectedSlot, selectedBuilding, inHome, configIds.Count);
            }

            ClearChildren(row);
            self.HomeBuildPrototypeButtons.Clear();

            if (configIds.Count == 0)
            {
                Button placeholder = CreateActionButton(
                    self,
                    "HomeBuildPrototypePlaceholder",
                    row,
                    "暂无可建建筑",
                    new Color(0.25f, 0.29f, 0.34f, 0.92f));
                SetButtonInteractable(placeholder, false);
                self.HomeBuildPrototypeButtons.Add(placeholder);
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            for (int i = 0; i < configIds.Count; ++i)
            {
                int configId = configIds[i];
                HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
                string label = config != null && config.BuildGoldCost > 0
                    ? $"{GetHomeBuildingName(configId)}\n{config.BuildGoldCost} 金币"
                    : GetHomeBuildingName(configId);
                Button button = CreateActionButton(
                    self,
                    $"HomeBuildPrototypeButton_{configId}",
                    row,
                    label,
                    GetHomeBuildButtonColor(configId, i));

                bool canBuild = inHome && CanBuildInSlot(selectedSlot, configId);
                SetButtonInteractable(button, canBuild);

                int capturedConfigId = configId;
                button.onClick.AddListener(() =>
                {
                    LobbyPanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.BuildHomePrototypeAsync(capturedConfigId).Coroutine();
                });

                self.HomeBuildPrototypeButtons.Add(button);
            }
        }

        private static void RenderSlotCards(this LobbyPanelComponent self, HomeClientComponent runtime, List<HomeClientBuildingData> buildings, bool inHome)
        {
            RectTransform content = self.HomeBuildingListContent;
            if (content == null)
            {
                return;
            }

            ClearChildren(content);
            List<HomeClientSlotData> slots = BuildSortedHomeSlots(runtime);
            if (slots.Count == 0)
            {
                RectTransform emptyCard = CreatePanel("HomeSlotEmptyCard", content, new Color(0.15f, 0.17f, 0.2f, 0.92f));
                LayoutElement emptyLayout = emptyCard.gameObject.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 74f;

                VerticalLayoutGroup emptyCardLayout = emptyCard.gameObject.AddComponent<VerticalLayoutGroup>();
                emptyCardLayout.padding = new RectOffset(14, 14, 12, 12);
                emptyCardLayout.childAlignment = TextAnchor.MiddleLeft;
                emptyCardLayout.childControlWidth = true;
                emptyCardLayout.childControlHeight = true;
                emptyCardLayout.childForceExpandWidth = true;
                emptyCardLayout.childForceExpandHeight = false;

                TMP_Text text = CreateText(self, "HomeSlotEmptyText", emptyCard, "当前未收到家园槽位数据。", 22f, new Color(0.86f, 0.89f, 0.93f, 1f), FontStyles.Normal);
                text.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;
            for (int i = 0; i < slots.Count; ++i)
            {
                HomeClientSlotData slot = slots[i];
                HomeClientBuildingData building = ResolveBuildingInSlot(slot, buildings);
                bool selected = slot.SlotId == self.SelectedHomeSlotId;
                RectTransform card = CreatePanel(
                    $"HomeSlotCard_{slot.SlotId}",
                    content,
                    GetHomeSlotCardColor(slot, building, selected));

                LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 96f;

                VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(14, 14, 10, 10);
                cardLayout.spacing = 4f;
                cardLayout.childAlignment = TextAnchor.UpperLeft;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;

                Button button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card.GetComponent<Image>();
                int slotId = slot.SlotId;
                long buildingId = building?.BuildingId ?? 0;
                button.onClick.AddListener(() =>
                {
                    LobbyPanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.SelectedHomeSlotId = slotId;
                    panel.SelectedHomeBuildingId = buildingId;
                    panel.RefreshHomeUiNow();
                });

                CreateText(self, "Title", card, BuildHomeSlotTitle(slot, building), 24f, Color.white, FontStyles.Bold);
                CreateText(self, "Subtitle", card, BuildHomeSlotSubtitle(slot, building), 20f, new Color(0.9f, 0.92f, 0.95f, 1f), FontStyles.Normal);
                TMP_Text hint = CreateText(self, "Hint", card, BuildHomeSlotHint(slot, building, inHome), 18f, new Color(0.76f, 0.8f, 0.86f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static void RenderMainCityTaskCards(this LobbyPanelComponent self, HomeClientComponent runtime, HomeClientBuildingData selectedBuilding)
        {
            RectTransform content = self.HomeBuildingListContent;
            if (content == null)
            {
                return;
            }

            ClearChildren(content);
            List<HomeClientMainCityTaskData> tasks = BuildSortedMainCityTasks(runtime);
            if (selectedBuilding == null || tasks.Count == 0)
            {
                RectTransform emptyCard = CreatePanel("HomeMainCityTaskEmptyCard", content, new Color(0.17f, 0.2f, 0.24f, 0.94f));
                LayoutElement emptyLayout = emptyCard.gameObject.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 88f;

                VerticalLayoutGroup emptyLayoutGroup = emptyCard.gameObject.AddComponent<VerticalLayoutGroup>();
                emptyLayoutGroup.padding = new RectOffset(14, 14, 12, 12);
                emptyLayoutGroup.spacing = 4f;
                emptyLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                emptyLayoutGroup.childControlWidth = true;
                emptyLayoutGroup.childControlHeight = true;
                emptyLayoutGroup.childForceExpandWidth = true;
                emptyLayoutGroup.childForceExpandHeight = false;

                CreateText(self, "Title", emptyCard, "当前没有主城任务数据", 24f, Color.white, FontStyles.Bold);
                TMP_Text hint = CreateText(self, "Hint", emptyCard, "请检查主城等级配置和家园快照是否已同步。", 18f, new Color(0.83f, 0.87f, 0.91f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            CreateTaskSummaryCard(self, content, runtime);
            for (int i = 0; i < tasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = tasks[i];
                RectTransform card = CreatePanel(
                    $"HomeMainCityTaskCard_{task.TaskId}",
                    content,
                    task.Completed ? new Color(0.17f, 0.33f, 0.22f, 0.96f) : new Color(0.27f, 0.19f, 0.12f, 0.96f));

                LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 110f;

                VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(14, 14, 12, 12);
                cardLayout.spacing = 4f;
                cardLayout.childAlignment = TextAnchor.UpperLeft;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;

                CreateText(self, "Title", card, task.Title ?? $"任务 {task.TaskId}", 24f, Color.white, FontStyles.Bold);
                CreateText(
                    self,
                    "Subtitle",
                    card,
                    task.Completed ? $"进度 {task.Progress}/{task.Target} · 已完成" : $"进度 {task.Progress}/{task.Target} · 待完成",
                    20f,
                    new Color(0.92f, 0.94f, 0.96f, 1f),
                    FontStyles.Normal);
                TMP_Text hint = CreateText(self, "Hint", card, string.IsNullOrWhiteSpace(task.Desc) ? "暂无任务说明。" : task.Desc, 18f, new Color(0.84f, 0.88f, 0.92f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static void RenderMuseumManageCards(this LobbyPanelComponent self, HomeClientComponent runtime, HomeClientBuildingData selectedBuilding, bool inHome)
        {
            RectTransform content = self.HomeBuildingListContent;
            if (content == null)
            {
                return;
            }

            ClearChildren(content);
            if (selectedBuilding == null)
            {
                RectTransform emptyCard = CreatePanel("HomeMuseumEmptyCard", content, new Color(0.17f, 0.2f, 0.24f, 0.94f));
                LayoutElement emptyLayout = emptyCard.gameObject.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 88f;

                VerticalLayoutGroup emptyLayoutGroup = emptyCard.gameObject.AddComponent<VerticalLayoutGroup>();
                emptyLayoutGroup.padding = new RectOffset(14, 14, 12, 12);
                emptyLayoutGroup.spacing = 4f;
                emptyLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                emptyLayoutGroup.childControlWidth = true;
                emptyLayoutGroup.childControlHeight = true;
                emptyLayoutGroup.childForceExpandWidth = true;
                emptyLayoutGroup.childForceExpandHeight = false;

                CreateText(self, "Title", emptyCard, "当前没有收藏馆可管理", 24f, Color.white, FontStyles.Bold);
                TMP_Text hint = CreateText(self, "Hint", emptyCard, "请先选择一个收藏馆建筑。", 18f, new Color(0.83f, 0.87f, 0.91f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            int capacity = GetMuseumCapacity(selectedBuilding);
            int placedCount = CountMuseumDisplays(runtime, selectedBuilding.BuildingId);
            CreateMuseumSummaryCard(self, content, selectedBuilding, capacity, placedCount, inHome);

            EntityRef<LobbyPanelComponent> selfRef = self;
            List<HomeClientMuseumDisplayData> displays = BuildSortedMuseumDisplays(runtime, selectedBuilding.BuildingId);
            for (int i = 0; i < displays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = displays[i];
                RectTransform card = CreatePanel(
                    $"HomeMuseumDisplayCard_{display.DisplayId}",
                    content,
                    new Color(0.34f, 0.23f, 0.13f, 0.96f));

                LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 102f;

                VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(14, 14, 12, 12);
                cardLayout.spacing = 4f;
                cardLayout.childAlignment = TextAnchor.UpperLeft;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;

                Button button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card.GetComponent<Image>();
                button.interactable = inHome;
                long displayId = display.DisplayId;
                long buildingId = display.BuildingId;
                button.onClick.AddListener(() =>
                {
                    LobbyPanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.TakeDownMuseumDisplayAsync(displayId, buildingId).Coroutine();
                });

                CreateText(self, "Title", card, $"已摆放 / 展位 {display.SlotIndex}", 24f, Color.white, FontStyles.Bold);
                CreateText(self, "Subtitle", card, GetHomeItemDisplayName(display.ItemConfigId), 20f, new Color(0.94f, 0.91f, 0.84f, 1f), FontStyles.Normal);
                TMP_Text hint = CreateText(self, "Hint", card, inHome ? "点击后会将该展示单位取下并放回仓库。" : "进入家园场景后可取下该展示单位。", 18f, new Color(0.84f, 0.88f, 0.92f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
            }

            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            if (capacity - placedCount <= 0)
            {
                return;
            }

            if (loadout?.WarehouseItems == null || loadout.WarehouseItems.Count == 0)
            {
                RectTransform emptyWarehouseCard = CreatePanel("HomeMuseumWarehouseEmptyCard", content, new Color(0.16f, 0.18f, 0.21f, 0.94f));
                LayoutElement emptyLayout = emptyWarehouseCard.gameObject.AddComponent<LayoutElement>();
                emptyLayout.preferredHeight = 88f;

                VerticalLayoutGroup emptyLayoutGroup = emptyWarehouseCard.gameObject.AddComponent<VerticalLayoutGroup>();
                emptyLayoutGroup.padding = new RectOffset(14, 14, 12, 12);
                emptyLayoutGroup.spacing = 4f;
                emptyLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                emptyLayoutGroup.childControlWidth = true;
                emptyLayoutGroup.childControlHeight = true;
                emptyLayoutGroup.childForceExpandWidth = true;
                emptyLayoutGroup.childForceExpandHeight = false;

                CreateText(self, "Title", emptyWarehouseCard, "仓库里没有可展示物品", 24f, Color.white, FontStyles.Bold);
                TMP_Text hint = CreateText(self, "Hint", emptyWarehouseCard, "先去跑局或使用回收间产出一些物品，再回来摆放。", 18f, new Color(0.83f, 0.87f, 0.91f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                RectTransform card = CreatePanel(
                    $"HomeMuseumWarehouseCard_{item.ItemUid}",
                    content,
                    new Color(0.15f, 0.23f, 0.29f, 0.96f));

                LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 102f;

                VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(14, 14, 12, 12);
                cardLayout.spacing = 4f;
                cardLayout.childAlignment = TextAnchor.UpperLeft;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childForceExpandHeight = false;

                Button button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card.GetComponent<Image>();
                button.interactable = inHome;
                long itemUid = item.ItemUid;
                long buildingId = selectedBuilding.BuildingId;
                button.onClick.AddListener(() =>
                {
                    LobbyPanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.PlaceMuseumDisplayAsync(buildingId, itemUid).Coroutine();
                });

                CreateText(self, "Title", card, $"仓库物品 / {GetHomeItemDisplayName(item.ConfigId)}", 24f, Color.white, FontStyles.Bold);
                CreateText(self, "Subtitle", card, $"数量 {item.Count} · UID {item.ItemUid}", 20f, new Color(0.92f, 0.94f, 0.96f, 1f), FontStyles.Normal);
                TMP_Text hint = CreateText(self, "Hint", card, inHome ? "点击后会从仓库取出 1 个单位并摆入收藏馆。" : "进入家园场景后可把该物品摆入收藏馆。", 18f, new Color(0.84f, 0.88f, 0.92f, 1f), FontStyles.Normal);
                hint.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private static void CreateTaskSummaryCard(this LobbyPanelComponent self, RectTransform content, HomeClientComponent runtime)
        {
            RectTransform card = CreatePanel("HomeMainCityTaskSummaryCard", content, new Color(0.15f, 0.21f, 0.3f, 0.96f));
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 96f;

            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(14, 14, 12, 12);
            cardLayout.spacing = 4f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(self, "Title", card, $"当前主城任务组 {runtime?.MainCitySummary?.TaskGroupId ?? 0}", 24f, Color.white, FontStyles.Bold);
            CreateText(
                self,
                "Subtitle",
                card,
                $"完成度 {runtime?.MainCitySummary?.TaskFinishedCount ?? 0}/{runtime?.MainCitySummary?.TaskTotalCount ?? 0} · {((runtime?.MainCitySummary?.CanUpgrade ?? false) ? "可升级主城" : "未满足升级条件")}",
                20f,
                new Color(0.92f, 0.94f, 0.96f, 1f),
                FontStyles.Normal);
            TMP_Text hint = CreateText(self, "Hint", card, BuildMainCityTaskGroupHint(runtime), 18f, new Color(0.84f, 0.88f, 0.92f, 1f), FontStyles.Normal);
            hint.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void CreateMuseumSummaryCard(
            this LobbyPanelComponent self,
            RectTransform content,
            HomeClientBuildingData building,
            int capacity,
            int placedCount,
            bool inHome)
        {
            RectTransform card = CreatePanel("HomeMuseumSummaryCard", content, new Color(0.18f, 0.2f, 0.26f, 0.96f));
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 96f;

            VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(14, 14, 12, 12);
            cardLayout.spacing = 4f;
            cardLayout.childAlignment = TextAnchor.UpperLeft;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            CreateText(self, "Title", card, $"{GetHomeBuildingName(building.ConfigId)} 管理", 24f, Color.white, FontStyles.Bold);
            CreateText(self, "Subtitle", card, $"已摆放 {placedCount}/{capacity} · 剩余展位 {Math.Max(capacity - placedCount, 0)}", 20f, new Color(0.92f, 0.94f, 0.96f, 1f), FontStyles.Normal);
            TMP_Text hint = CreateText(self, "Hint", card, inHome ? "上方卡片代表已摆放展示位，下方卡片代表可从仓库摆入的物品。" : "当前不在家园场景，只能查看展示数据。", 18f, new Color(0.84f, 0.88f, 0.92f, 1f), FontStyles.Normal);
            hint.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void RefreshHomeDetail(
            this LobbyPanelComponent self,
            HomeClientSlotData slot,
            HomeClientBuildingData building,
            bool inHome,
            HomeClientComponent runtime)
        {
            if (slot == null)
            {
                if (self.HomeDetailTitleText != null)
                {
                    self.HomeDetailTitleText.text = "建筑详情";
                }

                if (self.HomeDetailSubtitleText != null)
                {
                    self.HomeDetailSubtitleText.text = "当前没有可查看的槽位或建筑。";
                }

                if (self.HomeDetailStatusText != null)
                {
                    self.HomeDetailStatusText.text = "状态：空基地";
                }

                if (self.HomeDetailHintText != null)
                {
                    self.HomeDetailHintText.text = inHome
                        ? "请先在下方列表中选择一个槽位或建筑。"
                        : "当前不在家园场景，所有基地操作按钮均已禁用。";
                }

                SetButtonText(self.HomeUpgradeButton, "升级");
                SetButtonText(self.HomeCollectButton, "操作");
                SetButtonText(self.HomeDemolishButton, "拆除");
                SetButtonInteractable(self.HomeUpgradeButton, false);
                SetButtonInteractable(self.HomeCollectButton, false);
                SetButtonInteractable(self.HomeDemolishButton, false);
                return;
            }

            if (building == null)
            {
                if (self.HomeDetailTitleText != null)
                {
                    self.HomeDetailTitleText.text = BuildHomeSlotTitle(slot, null);
                }

                if (self.HomeDetailSubtitleText != null)
                {
                    self.HomeDetailSubtitleText.text = BuildHomeSlotSubtitle(slot, null);
                }

                if (self.HomeDetailStatusText != null)
                {
                    self.HomeDetailStatusText.text = $"状态：{GetHomeSlotStatus(slot)}";
                }

                if (self.HomeDetailHintText != null)
                {
                    self.HomeDetailHintText.text = BuildHomeEmptySlotDetailHint(slot, inHome);
                }

                SetButtonText(self.HomeUpgradeButton, "升级");
                SetButtonText(self.HomeCollectButton, "操作");
                SetButtonText(self.HomeDemolishButton, "拆除");
                SetButtonInteractable(self.HomeUpgradeButton, false);
                SetButtonInteractable(self.HomeCollectButton, false);
                SetButtonInteractable(self.HomeDemolishButton, false);
                return;
            }

            int buildingType = GetHomeBuildingType(building.ConfigId);

            if (self.HomeDetailTitleText != null)
            {
                self.HomeDetailTitleText.text = $"槽位 {building.SlotId} / {GetHomeBuildingName(building.ConfigId)}";
            }

            if (self.HomeDetailSubtitleText != null)
            {
                self.HomeDetailSubtitleText.text = $"建筑Id {building.BuildingId} · 当前等级 {building.Level}";
            }

            if (self.HomeDetailStatusText != null)
            {
                self.HomeDetailStatusText.text = BuildHomeDetailStatus(building, buildingType, runtime);
            }

            if (self.HomeDetailHintText != null)
            {
                self.HomeDetailHintText.text = BuildHomeDetailHint(building, buildingType, runtime);
            }

            bool canUpgrade = false;
            if (inHome)
            {
                if (buildingType == HomeBuildingType.MainCity)
                {
                    canUpgrade = HasNextMainCityLevel(building.Level + 1) && (runtime?.MainCitySummary?.CanUpgrade ?? false);
                }
                else
                {
                    canUpgrade = HasNextHomeBuildingLevel(building.ConfigId, building.Level + 1) &&
                        building.Level < (runtime?.MainCitySummary?.OtherBuildingMaxLevel ?? 0);
                }
            }

            bool canCollect = inHome && CanExecuteHomeAction(building, buildingType, runtime);
            bool canDemolish = inHome && CanDemolishHomeBuilding(building, buildingType, runtime);

            SetButtonText(self.HomeUpgradeButton, buildingType == HomeBuildingType.MainCity ? "升级主城" : "升级建筑");
            SetButtonText(self.HomeCollectButton, GetHomeActionButtonText(building, buildingType, runtime));
            SetButtonText(self.HomeDemolishButton, buildingType == HomeBuildingType.MainCity ? "主城不可拆" : "拆除建筑");

            SetButtonInteractable(self.HomeUpgradeButton, canUpgrade);
            SetButtonInteractable(self.HomeCollectButton, canCollect);
            SetButtonInteractable(self.HomeDemolishButton, canDemolish);
        }

        private static List<HomeClientSlotData> BuildSortedHomeSlots(HomeClientComponent runtime)
        {
            List<HomeClientSlotData> result = new();
            if (runtime == null)
            {
                return result;
            }

            for (int i = 0; i < runtime.Slots.Count; ++i)
            {
                if (runtime.Slots[i] != null)
                {
                    result.Add(runtime.Slots[i]);
                }
            }

            result.Sort(static (a, b) =>
            {
                int sortCompare = a.SortOrder.CompareTo(b.SortOrder);
                return sortCompare != 0 ? sortCompare : a.SlotId.CompareTo(b.SlotId);
            });
            return result;
        }

        private static HomeClientSlotData ResolveSelectedHomeSlot(
            LobbyPanelComponent self,
            HomeClientComponent runtime,
            List<HomeClientBuildingData> buildings)
        {
            List<HomeClientSlotData> slots = BuildSortedHomeSlots(runtime);
            if (slots.Count == 0)
            {
                self.SelectedHomeSlotId = 0;
                self.SelectedHomeBuildingId = 0;
                return null;
            }

            if (self.SelectedHomeBuildingId > 0)
            {
                for (int i = 0; i < buildings.Count; ++i)
                {
                    if (buildings[i].BuildingId != self.SelectedHomeBuildingId)
                    {
                        continue;
                    }

                    self.SelectedHomeSlotId = buildings[i].SlotId;
                    return runtime?.GetSlot(buildings[i].SlotId);
                }

                self.SelectedHomeBuildingId = 0;
            }

            if (self.SelectedHomeSlotId > 0)
            {
                HomeClientSlotData selectedSlot = runtime?.GetSlot(self.SelectedHomeSlotId);
                if (selectedSlot != null)
                {
                    self.SelectedHomeBuildingId = selectedSlot.BuildingId;
                    return selectedSlot;
                }

                self.SelectedHomeSlotId = 0;
            }

            HomeClientSlotData fallback = null;
            for (int i = 0; i < slots.Count; ++i)
            {
                if (slots[i].BuildingId > 0)
                {
                    fallback = slots[i];
                    break;
                }
            }

            if (fallback == null)
            {
                for (int i = 0; i < slots.Count; ++i)
                {
                    if (slots[i].Unlocked && slots[i].SlotType == HomeSlotType.Buildable)
                    {
                        fallback = slots[i];
                        break;
                    }
                }
            }

            fallback ??= slots[0];
            self.SelectedHomeSlotId = fallback.SlotId;
            self.SelectedHomeBuildingId = fallback.BuildingId;
            return fallback;
        }

        private static HomeClientBuildingData ResolveBuildingInSlot(HomeClientSlotData slot, List<HomeClientBuildingData> buildings)
        {
            if (slot == null || buildings == null)
            {
                return null;
            }

            for (int i = 0; i < buildings.Count; ++i)
            {
                HomeClientBuildingData building = buildings[i];
                if (building == null)
                {
                    continue;
                }

                if (slot.BuildingId > 0 && building.BuildingId == slot.BuildingId)
                {
                    return building;
                }

                if (building.SlotId == slot.SlotId)
                {
                    return building;
                }
            }

            return null;
        }

        private static void SetStatText(TMP_Text text, int value)
        {
            if (text != null)
            {
                text.text = value.ToString();
            }
        }

        private static void SetStatText(TMP_Text text, long value)
        {
            if (text != null)
            {
                text.text = value.ToString();
            }
        }

        private static void SetStatText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;

            if (button.targetGraphic is Graphic graphic)
            {
                graphic.color = interactable ? button.colors.normalColor : button.colors.disabledColor;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = interactable ? Color.white : new Color(1f, 1f, 1f, 0.72f);
            }
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; --i)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static string GetHomeOverviewStatLabel(int index)
        {
            return index switch
            {
                0 => "已建建筑",
                1 => "主城等级",
                2 => "仓库占用",
                3 => "金币储备",
                _ => "统计",
            };
        }

        private static Color GetHomeBuildButtonColor(int configId, int index)
        {
            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            return config?.BuildingType switch
            {
                HomeBuildingType.Museum => new Color(0.62f, 0.39f, 0.23f, 0.96f),
                HomeBuildingType.RecycleRoom => new Color(0.31f, 0.44f, 0.68f, 0.96f),
                HomeBuildingType.Farm => new Color(0.35f, 0.56f, 0.27f, 0.96f),
                HomeBuildingType.Warehouse => new Color(0.57f, 0.47f, 0.18f, 0.96f),
                _ => index % 2 == 0
                    ? new Color(0.35f, 0.56f, 0.27f, 0.96f)
                    : new Color(0.31f, 0.44f, 0.68f, 0.96f),
            };
        }

        private static bool CanBuildInSlot(HomeClientSlotData slot, int configId)
        {
            if (slot == null || !slot.Unlocked || slot.SlotType != HomeSlotType.Buildable || slot.BuildingId > 0)
            {
                return false;
            }

            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            if (config == null)
            {
                return false;
            }

            if (slot.CanBuildTypes.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < slot.CanBuildTypes.Count; ++i)
            {
                if (slot.CanBuildTypes[i] == config.BuildingType)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildHomeBuildEntryHint(
            LobbyPanelComponent self,
            HomeClientSlotData selectedSlot,
            HomeClientBuildingData selectedBuilding,
            bool inHome,
            int buildableCount)
        {
            if (self != null && self.HomeSubViewMode == HOME_SUBVIEW_MAIN_CITY_TASKS)
            {
                return "当前为主城任务页，可查看任务进度并返回家园概览。";
            }

            if (self != null && self.HomeSubViewMode == HOME_SUBVIEW_MUSEUM_MANAGE)
            {
                return "当前为收藏馆管理页，点击下方卡片可摆放或取下展示单位。";
            }

            if (!inHome)
            {
                return "当前不在家园场景，建造入口仅保留展示；进入 Home 后请先选择空槽位。";
            }

            if (selectedBuilding != null)
            {
                return $"当前选中 {GetHomeBuildingName(selectedBuilding.ConfigId)}。若要新建，请先改选一个空槽位。";
            }

            if (selectedSlot == null)
            {
                return "请先在下方列表中选择一个空槽位，再从这里挑选建筑。";
            }

            if (!selectedSlot.Unlocked)
            {
                return $"{GetHomeSlotLockText(selectedSlot)}，解锁后才能建造。";
            }

            if (selectedSlot.SlotType != HomeSlotType.Buildable)
            {
                return "当前选中的是主城保留槽位，不能新建其他建筑。";
            }

            if (selectedSlot.BuildingId > 0)
            {
                return "当前槽位已有建筑，不能重复建造。";
            }

            if (buildableCount <= 0)
            {
                return "当前没有可建建筑，请先提升主城或释放同类建筑数量上限。";
            }

            return $"当前选中槽位 {selectedSlot.SlotId}，可建：{GetHomeSlotBuildTypeSummary(selectedSlot)}。";
        }

        private static Color GetHomeSlotCardColor(HomeClientSlotData slot, HomeClientBuildingData building, bool selected)
        {
            if (building != null)
            {
                return selected ? new Color(0.44f, 0.35f, 0.16f, 0.97f) : new Color(0.15f, 0.17f, 0.21f, 0.95f);
            }

            if (slot != null && !slot.Unlocked)
            {
                return selected ? new Color(0.18f, 0.23f, 0.32f, 0.96f) : new Color(0.12f, 0.14f, 0.18f, 0.95f);
            }

            return selected ? new Color(0.18f, 0.33f, 0.24f, 0.96f) : new Color(0.14f, 0.18f, 0.15f, 0.95f);
        }

        private static string BuildHomeSlotTitle(HomeClientSlotData slot, HomeClientBuildingData building)
        {
            if (slot == null)
            {
                return "建筑与槽位";
            }

            if (building != null)
            {
                return $"槽位 {slot.SlotId} / {GetHomeBuildingName(building.ConfigId)}";
            }

            return slot.Unlocked ? $"槽位 {slot.SlotId} / 空地块" : $"槽位 {slot.SlotId} / 未解锁";
        }

        private static string BuildHomeSlotSubtitle(HomeClientSlotData slot, HomeClientBuildingData building)
        {
            if (slot == null)
            {
                return "--";
            }

            if (building != null)
            {
                return $"等级 {building.Level} · 状态 {FormatHomeBuildingState(building.State)}";
            }

            if (!slot.Unlocked)
            {
                return GetHomeSlotLockText(slot);
            }

            if (slot.SlotType != HomeSlotType.Buildable)
            {
                return "主城保留槽位";
            }

            return $"可建：{GetHomeSlotBuildTypeSummary(slot)}";
        }

        private static string BuildHomeSlotHint(HomeClientSlotData slot, HomeClientBuildingData building, bool inHome)
        {
            if (slot == null)
            {
                return "当前没有可显示的槽位信息。";
            }

            if (building != null)
            {
                return $"最近收取：{FormatHomeDateTime(building.LastCollectTime)}";
            }

            if (!slot.Unlocked)
            {
                return "升级主城后可解锁该地块。";
            }

            if (slot.SlotType != HomeSlotType.Buildable)
            {
                return "该槽位保留给主城，不参与普通建造。";
            }

            return inHome
                ? "选中该空槽位后，可从上方建造入口落下新建筑。"
                : "进入家园场景后，可在该槽位建造新建筑。";
        }

        private static string GetHomeSlotStatus(HomeClientSlotData slot)
        {
            if (slot == null)
            {
                return "空";
            }

            if (!slot.Unlocked)
            {
                return "锁定";
            }

            if (slot.SlotType != HomeSlotType.Buildable)
            {
                return "主城槽位";
            }

            return slot.BuildingId > 0 ? "占用中" : "待建造";
        }

        private static string BuildHomeEmptySlotDetailHint(HomeClientSlotData slot, bool inHome)
        {
            if (slot == null)
            {
                return "请先选择一个槽位。";
            }

            if (!slot.Unlocked)
            {
                return $"{GetHomeSlotLockText(slot)}。提升主城后可解锁该地块。";
            }

            if (slot.SlotType != HomeSlotType.Buildable)
            {
                return "该槽位属于主城保留位，不支持建造其他建筑。";
            }

            if (slot.BuildingId > 0)
            {
                return "该槽位已有建筑数据，等待客户端刷新完成。";
            }

            return inHome
                ? $"这是一个可建空槽位。当前兼容：{GetHomeSlotBuildTypeSummary(slot)}。请从上方建造入口选择建筑。"
                : "进入家园场景后，可在该槽位建造新建筑。";
        }

        private static string GetHomeSlotLockText(HomeClientSlotData slot)
        {
            HomeSlotConfig config = HomeSlotConfigCategory.Instance.GetOrDefault(slot?.SlotId ?? 0);
            int unlockLevel = config?.UnlockMainCityLevel ?? 0;
            return unlockLevel > 0 ? $"主城达到 {unlockLevel} 级解锁" : "当前槽位尚未解锁";
        }

        private static string GetHomeSlotBuildTypeSummary(HomeClientSlotData slot)
        {
            if (slot == null)
            {
                return "--";
            }

            if (slot.SlotType != HomeSlotType.Buildable)
            {
                return "主城";
            }

            if (slot.CanBuildTypes.Count == 0)
            {
                return "任意已解锁建筑";
            }

            StringBuilder builder = new StringBuilder(32);
            for (int i = 0; i < slot.CanBuildTypes.Count; ++i)
            {
                if (i > 0)
                {
                    builder.Append('、');
                }

                builder.Append(GetHomeBuildingTypeName(slot.CanBuildTypes[i]));
            }

            return builder.ToString();
        }

        private static string GetHomeBuildingTypeName(int buildingType)
        {
            return buildingType switch
            {
                HomeBuildingType.MainCity => "主城",
                HomeBuildingType.Museum => "大红收藏馆",
                HomeBuildingType.RecycleRoom => "物品回收间",
                HomeBuildingType.Farm => "农场",
                HomeBuildingType.Warehouse => "仓库",
                _ => $"类型 {buildingType}",
            };
        }

        private static bool HasNextMainCityLevel(int nextLevel)
        {
            return HomeMainCityLevelConfigCategory.Instance.GetOrDefault(nextLevel) != null;
        }

        private static List<HomeClientMainCityTaskData> BuildSortedMainCityTasks(HomeClientComponent runtime)
        {
            List<HomeClientMainCityTaskData> result = new();
            if (runtime?.MainCityTasks == null)
            {
                return result;
            }

            for (int i = 0; i < runtime.MainCityTasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = runtime.MainCityTasks[i];
                if (task != null)
                {
                    result.Add(task);
                }
            }

            result.Sort(static (a, b) =>
            {
                int sortCompare = a.SortOrder.CompareTo(b.SortOrder);
                return sortCompare != 0 ? sortCompare : a.TaskId.CompareTo(b.TaskId);
            });
            return result;
        }

        private static List<HomeClientMuseumDisplayData> BuildSortedMuseumDisplays(HomeClientComponent runtime, long buildingId)
        {
            List<HomeClientMuseumDisplayData> result = new();
            if (runtime?.MuseumDisplays == null || buildingId <= 0)
            {
                return result;
            }

            for (int i = 0; i < runtime.MuseumDisplays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = runtime.MuseumDisplays[i];
                if (display != null && display.BuildingId == buildingId)
                {
                    result.Add(display);
                }
            }

            result.Sort(static (a, b) =>
            {
                int slotCompare = a.SlotIndex.CompareTo(b.SlotIndex);
                return slotCompare != 0 ? slotCompare : a.DisplayId.CompareTo(b.DisplayId);
            });
            return result;
        }

        private static int CountMuseumDisplays(HomeClientComponent runtime, long buildingId)
        {
            int count = 0;
            if (runtime?.MuseumDisplays == null)
            {
                return count;
            }

            for (int i = 0; i < runtime.MuseumDisplays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = runtime.MuseumDisplays[i];
                if (display == null)
                {
                    continue;
                }

                if (buildingId > 0 && display.BuildingId != buildingId)
                {
                    continue;
                }

                ++count;
            }

            return count;
        }

        private static int GetMuseumCapacity(HomeClientBuildingData building)
        {
            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(building?.ConfigId ?? 0, building?.Level ?? 0);
            return Math.Max(levelConfig?.CapacityValue1 ?? 0, 0);
        }

        private static string BuildMainCityTaskGroupHint(HomeClientComponent runtime)
        {
            List<HomeClientMainCityTaskData> tasks = BuildSortedMainCityTasks(runtime);
            for (int i = 0; i < tasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = tasks[i];
                if (task == null || task.Completed)
                {
                    continue;
                }

                return $"当前待完成：{task.Title}（{task.Progress}/{task.Target}）";
            }

            return tasks.Count > 0 ? "当前主城任务已全部完成，可升级主城。" : "当前没有主城任务。";
        }

        private static string BuildHomeErrorText(string prefix, int error, string message)
        {
            string detail = error switch
            {
                ErrorCode.ERR_HomePrerequisiteNotMet => string.IsNullOrWhiteSpace(message) ? "前置条件未满足，请先完成主城任务或补足资源。" : message,
                ErrorCode.ERR_HomeSlotOccupied => "槽位已被占用。",
                ErrorCode.ERR_HomeBuildingMaxLevel => "该建筑已达当前样机最高等级。",
                ErrorCode.ERR_HomeNothingToCollect => "当前没有可收取产出。",
                ErrorCode.ERR_HomeBuildingNotFound => "目标建筑不存在。",
                ErrorCode.ERR_HomeNotInHomeScene => "当前不在家园场景。",
                ErrorCode.ERR_HomeProductionQueueFull => "回收位已满，请先收取或等待当前队列完成。",
                ErrorCode.ERR_HomeProductionNotReady => "当前回收结果还未完成。",
                ErrorCode.ERR_HomeProductionNotFound => "没有找到可收取的回收订单。",
                ErrorCode.ERR_HomeRecipeNotUnlocked => "当前没有可用的回收材料。",
                ErrorCode.ERR_HomeBuildingCannotDemolish => string.IsNullOrWhiteSpace(message) ? "当前建筑暂时不能拆除。" : message,
                ErrorCode.ERR_HomeResourceNotEnough => "金币或仓库资源不足。",
                _ when !string.IsNullOrWhiteSpace(message) => message,
                _ => $"错误码 {error}",
            };

            return $"{prefix} {detail}";
        }

        private static string BuildCollectSuccessText(M2C_HomeCollectResponse response)
        {
            if (response == null)
            {
                return "收取成功。";
            }

            if (response.WealthDelta > 0)
            {
                return $"收取成功，本次获得 {response.WealthDelta} 金币。";
            }

            if (response.ItemCounts == null || response.ItemCounts.Count == 0)
            {
                return "收取成功。";
            }

            int totalCount = 0;
            for (int i = 0; i < response.ItemCounts.Count; ++i)
            {
                totalCount += response.ItemCounts[i];
            }

            return $"收取成功，本次共获得 {totalCount} 个产出。";
        }

        private static string BuildCollectProductionSuccessText(M2C_HomeCollectProductionResponse response)
        {
            if (response == null || response.ItemCounts == null || response.ItemCounts.Count == 0)
            {
                return "回收产物已入仓。";
            }

            int totalCount = 0;
            for (int i = 0; i < response.ItemCounts.Count; ++i)
            {
                totalCount += response.ItemCounts[i];
            }

            return $"回收完成，本次共有 {totalCount} 个产物入仓。";
        }

        private static int GetHomeBuildingType(int configId)
        {
            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            return config?.BuildingType ?? 0;
        }

        private static bool CanExecuteHomeAction(HomeClientBuildingData building, int buildingType, HomeClientComponent runtime)
        {
            if (building == null)
            {
                return false;
            }

            return buildingType switch
            {
                HomeBuildingType.MainCity => true,
                HomeBuildingType.Museum => true,
                HomeBuildingType.Warehouse => true,
                HomeBuildingType.Farm => PeekHomeFarmWealth(building) > 0,
                HomeBuildingType.RecycleRoom => GetFirstReadyRecycleOrderId(runtime, building.BuildingId) > 0 || ResolveHomeRecycleSourceItemConfig(runtime?.Root()) > 0,
                _ => false,
            };
        }

        private static bool CanDemolishHomeBuilding(HomeClientBuildingData building, int buildingType, HomeClientComponent runtime)
        {
            if (building == null || buildingType == HomeBuildingType.MainCity)
            {
                return false;
            }

            if (buildingType == HomeBuildingType.Farm && PeekHomeFarmWealth(building) > 0)
            {
                return false;
            }

            if (buildingType == HomeBuildingType.Warehouse && (runtime?.WarehouseSummary?.ItemCount ?? 0) > 0)
            {
                return false;
            }

            if (buildingType == HomeBuildingType.RecycleRoom &&
                (CountRunningRecycleOrders(runtime, building.BuildingId) > 0 || CountReadyRecycleOrders(runtime, building.BuildingId) > 0))
            {
                return false;
            }

            if (buildingType == HomeBuildingType.Museum && CountMuseumDisplays(runtime, building.BuildingId) > 0)
            {
                return false;
            }

            return true;
        }

        private static string GetHomeActionButtonText(HomeClientBuildingData building, int buildingType, HomeClientComponent runtime)
        {
            if (building == null)
            {
                return "操作";
            }

            return buildingType switch
            {
                HomeBuildingType.MainCity => "查看任务",
                HomeBuildingType.Museum => "管理展示",
                HomeBuildingType.Warehouse => "查看仓库",
                HomeBuildingType.Farm => PeekHomeFarmWealth(building) > 0 ? "收取金币" : "等待产出",
                HomeBuildingType.RecycleRoom => GetFirstReadyRecycleOrderId(runtime, building.BuildingId) > 0
                    ? "收取产物"
                    : ResolveHomeRecycleSourceItemConfig(runtime?.Root()) > 0 ? "开始回收" : "缺少材料",
                _ => "无操作",
            };
        }

        private static string BuildHomeDetailStatus(HomeClientBuildingData building, int buildingType, HomeClientComponent runtime)
        {
            if (building == null)
            {
                return "状态：--";
            }

            if (buildingType == HomeBuildingType.MainCity)
            {
                return $"状态：主城 {building.Level} 级 · 任务 {runtime?.MainCitySummary?.TaskFinishedCount ?? 0}/{runtime?.MainCitySummary?.TaskTotalCount ?? 0}";
            }

            if (buildingType == HomeBuildingType.Museum)
            {
                int capacity = GetMuseumCapacity(building);
                int displayCount = CountMuseumDisplays(runtime, building.BuildingId);
                return $"状态：已摆放 {displayCount}/{capacity}";
            }

            if (buildingType == HomeBuildingType.RecycleRoom)
            {
                int readyCount = CountReadyRecycleOrders(runtime, building.BuildingId);
                int activeCount = CountRunningRecycleOrders(runtime, building.BuildingId);
                int capacity = GetRecycleSlotCapacity(building);
                if (readyCount > 0)
                {
                    return $"状态：{readyCount} 个产物待收取 · 总处理位 {capacity}";
                }

                if (activeCount > 0)
                {
                    return $"状态：{activeCount}/{capacity} 个处理位运行中";
                }

                return $"状态：空闲 · 当前处理位 {capacity}";
            }

            if (buildingType == HomeBuildingType.Farm)
            {
                long wealth = PeekHomeFarmWealth(building);
                return wealth > 0 ? $"状态：可收取 {wealth} 金币" : "状态：农场产出中";
            }

            if (buildingType == HomeBuildingType.Warehouse)
            {
                int capacity = runtime?.WarehouseSummary?.Capacity ?? 0;
                int occupied = runtime?.WarehouseSummary?.OccupiedCellCount ?? 0;
                return $"状态：仓库占用 {occupied}/{capacity}";
            }

            return $"状态：{FormatHomeBuildingState(building.State)}";
        }

        private static string FormatHomeBuildingState(int state)
        {
            return state switch
            {
                HomeBuildingState.Idle => "空闲",
                HomeBuildingState.Collecting => "可收取",
                _ => $"未知({state})",
            };
        }

        private static string BuildHomeDetailHint(HomeClientBuildingData building, int buildingType, HomeClientComponent runtime)
        {
            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(building?.ConfigId ?? 0, building?.Level ?? 0);
            if (buildingType == HomeBuildingType.MainCity)
            {
                return
                    $"主城任务组：{runtime?.MainCitySummary?.TaskGroupId ?? 0}，完成度 {runtime?.MainCitySummary?.TaskFinishedCount ?? 0}/{runtime?.MainCitySummary?.TaskTotalCount ?? 0}\n" +
                    $"当前已解锁槽位：{runtime?.MainCitySummary?.UnlockedSlotCount ?? 0}，其他建筑等级上限：{runtime?.MainCitySummary?.OtherBuildingMaxLevel ?? 0}\n" +
                    $"{BuildMainCityTaskGroupHint(runtime)}\n" +
                    $"{runtime?.MainCitySummary?.PreviewText ?? "主城负责解锁更多地块和建筑等级。"}";
            }

            if (buildingType == HomeBuildingType.Museum)
            {
                int capacity = levelConfig?.CapacityValue1 ?? 0;
                int displayCount = CountMuseumDisplays(runtime, building.BuildingId);
                return
                    $"当前展示容量：{capacity}，已摆放：{displayCount}\n" +
                    "点击“管理展示”后，可把仓库中的物品摆入收藏馆，也可以随时取下。";
            }

            if (buildingType == HomeBuildingType.RecycleRoom)
            {
                int capacity = GetRecycleSlotCapacity(building);
                int intervalMs = GetRecycleIntervalMs(building);
                int readyCount = CountReadyRecycleOrders(runtime, building.BuildingId);
                long nearestFinishTime = GetNearestRecycleFinishTime(runtime, building.BuildingId);
                string sourceItem = GetHomeItemDisplayName(ResolveHomeRecycleSourceItemConfig(runtime?.Root()));
                return
                    $"处理位：{capacity}，单次回收时长：{FormatDuration(intervalMs)}\n" +
                    $"待收取产物：{readyCount}，下一单完成时间：{FormatHomeDateTime(nearestFinishTime)}\n" +
                    $"开始回收会消耗仓库中的首个可用物品，当前候选：{sourceItem}";
            }

            if (buildingType == HomeBuildingType.Warehouse)
            {
                int capacity = runtime?.WarehouseSummary?.Capacity ?? 0;
                int occupied = runtime?.WarehouseSummary?.OccupiedCellCount ?? 0;
                int remain = Math.Max(capacity - occupied, 0);
                return
                    $"仓库占用：{occupied}/{capacity}，剩余空间：{remain}\n" +
                    $"当前物品堆：{runtime?.WarehouseSummary?.ItemCount ?? 0}\n" +
                    "回收间产物会直接尝试入仓，满仓时会被阻塞。";
            }

            if (buildingType == HomeBuildingType.Farm)
            {
                int intervalMs = GetFarmCollectIntervalMs(building);
                long wealth = PeekHomeFarmWealth(building);
                return
                    $"每轮产出：{levelConfig?.OutputValue ?? 0} 金币，产出间隔：{FormatDuration(intervalMs)}\n" +
                    $"当前待收金币：{wealth}\n" +
                    $"上次收取：{FormatHomeDateTime(building.LastCollectTime)}";
            }

            return
                $"上次收取：{FormatHomeDateTime(building.LastCollectTime)}\n" +
                $"最近生产：{FormatHomeDateTime(building.LastProductionTime)}";
        }

        private static int CountReadyRecycleOrders(HomeClientComponent runtime, long buildingId)
        {
            int count = 0;
            if (runtime == null || buildingId <= 0)
            {
                return count;
            }

            for (int i = 0; i < runtime.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = runtime.ProductionOrders[i];
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                if (GetHomeRecycleOrderState(order) == HomeProductionState.Completed)
                {
                    ++count;
                }
            }

            return count;
        }

        private static int CountRunningRecycleOrders(HomeClientComponent runtime, long buildingId)
        {
            int count = 0;
            if (runtime == null || buildingId <= 0)
            {
                return count;
            }

            for (int i = 0; i < runtime.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = runtime.ProductionOrders[i];
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                if (GetHomeRecycleOrderState(order) == HomeProductionState.InProgress)
                {
                    ++count;
                }
            }

            return count;
        }

        private static long GetFirstReadyRecycleOrderId(HomeClientComponent runtime, long buildingId)
        {
            long candidate = 0;
            long candidateFinishTime = long.MaxValue;
            if (runtime == null || buildingId <= 0)
            {
                return 0;
            }

            for (int i = 0; i < runtime.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = runtime.ProductionOrders[i];
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                if (GetHomeRecycleOrderState(order) != HomeProductionState.Completed)
                {
                    continue;
                }

                if (order.FinishTime < candidateFinishTime)
                {
                    candidate = order.OrderId;
                    candidateFinishTime = order.FinishTime;
                }
            }

            return candidate;
        }

        private static long GetNearestRecycleFinishTime(HomeClientComponent runtime, long buildingId)
        {
            long candidateFinishTime = 0;
            if (runtime == null || buildingId <= 0)
            {
                return 0;
            }

            for (int i = 0; i < runtime.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = runtime.ProductionOrders[i];
                if (order == null || order.BuildingEntityId != buildingId || GetHomeRecycleOrderState(order) != HomeProductionState.InProgress)
                {
                    continue;
                }

                if (candidateFinishTime == 0 || order.FinishTime < candidateFinishTime)
                {
                    candidateFinishTime = order.FinishTime;
                }
            }

            return candidateFinishTime;
        }

        private static int GetHomeRecycleOrderState(HomeClientProductionOrderData order)
        {
            if (order == null)
            {
                return HomeProductionState.Collected;
            }

            if (order.State == HomeProductionState.InProgress && TimeInfo.Instance.ServerNow() >= order.FinishTime)
            {
                return HomeProductionState.Completed;
            }

            return order.State;
        }

        private static int GetRecycleSlotCapacity(HomeClientBuildingData building)
        {
            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(building?.ConfigId ?? 0, building?.Level ?? 0);
            return Math.Max(levelConfig?.CapacityValue1 ?? 1, 1);
        }

        private static int GetRecycleIntervalMs(HomeClientBuildingData building)
        {
            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(building?.ConfigId ?? 0, building?.Level ?? 0);
            return Math.Max(GetHomeBuildingExtraInt(levelConfig, "processIntervalMs", 60_000), 1000);
        }

        private static int GetFarmCollectIntervalMs(HomeClientBuildingData building)
        {
            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(building?.ConfigId ?? 0, building?.Level ?? 0);
            return Math.Max(GetHomeBuildingExtraInt(levelConfig, "collectIntervalMs", 60_000), 1000);
        }

        private static long PeekHomeFarmWealth(HomeClientBuildingData building)
        {
            if (building == null || GetHomeBuildingType(building.ConfigId) != HomeBuildingType.Farm)
            {
                return 0;
            }

            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(building.ConfigId, building.Level);
            if (levelConfig == null)
            {
                return 0;
            }

            int intervalMs = GetFarmCollectIntervalMs(building);
            if (intervalMs <= 0)
            {
                return 0;
            }

            long elapsed = TimeInfo.Instance.ServerNow() - building.LastCollectTime;
            long tickCount = elapsed / intervalMs;
            return tickCount <= 0 ? 0 : tickCount * levelConfig.OutputValue;
        }

        private static HomeBuildingLevelConfig GetHomeBuildingLevelConfig(int configId, int level)
        {
            foreach (HomeBuildingLevelConfig config in HomeBuildingLevelConfigCategory.Instance.DataList)
            {
                if (config != null && config.BuildingConfigId == configId && config.Level == level)
                {
                    return config;
                }
            }

            return null;
        }

        private static int GetHomeBuildingExtraInt(HomeBuildingLevelConfig config, string key, int defaultValue)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ExtraParams) || string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            string token = $"\"{key}\"";
            int keyIndex = config.ExtraParams.IndexOf(token, StringComparison.Ordinal);
            if (keyIndex < 0)
            {
                return defaultValue;
            }

            int colonIndex = config.ExtraParams.IndexOf(':', keyIndex + token.Length);
            if (colonIndex < 0)
            {
                return defaultValue;
            }

            int startIndex = colonIndex + 1;
            while (startIndex < config.ExtraParams.Length && char.IsWhiteSpace(config.ExtraParams[startIndex]))
            {
                ++startIndex;
            }

            int endIndex = startIndex;
            if (endIndex < config.ExtraParams.Length && config.ExtraParams[endIndex] == '-')
            {
                ++endIndex;
            }

            while (endIndex < config.ExtraParams.Length && char.IsDigit(config.ExtraParams[endIndex]))
            {
                ++endIndex;
            }

            if (endIndex <= startIndex)
            {
                return defaultValue;
            }

            string numberText = config.ExtraParams.Substring(startIndex, endIndex - startIndex);
            return int.TryParse(numberText, out int value) ? value : defaultValue;
        }

        private static int ResolveHomeRecycleSourceItemConfig(Scene root)
        {
            LoadoutComponent loadout = root?.GetComponent<LoadoutComponent>();
            if (loadout?.WarehouseItems == null)
            {
                return 0;
            }

            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ConfigId > 0 && item.Count > 0)
                {
                    return item.ConfigId;
                }
            }

            return 0;
        }

        private static string GetHomeItemDisplayName(int configId)
        {
            if (configId <= 0)
            {
                return "无";
            }

            ResolveDisplayInfo(configId, out string name, out _, out _);
            return string.IsNullOrWhiteSpace(name) ? $"物品 {configId}" : name;
        }

        private static string FormatDuration(int durationMs)
        {
            if (durationMs <= 0)
            {
                return "--";
            }

            if (durationMs % 60_000 == 0)
            {
                return $"{durationMs / 60_000} 分钟";
            }

            return $"{durationMs / 1000} 秒";
        }

        private static bool HasNextHomeBuildingLevel(int configId, int nextLevel)
        {
            foreach (HomeBuildingLevelConfig config in HomeBuildingLevelConfigCategory.Instance.DataList)
            {
                if (config != null && config.BuildingConfigId == configId && config.Level == nextLevel)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetHomeBuildingName(int configId)
        {
            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            return config?.Name ?? $"建筑 {configId}";
        }

        private static string FormatHomeDateTime(long timestamp)
        {
            if (timestamp <= 0)
            {
                return "--";
            }

            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToLocalTime().ToString("MM-dd HH:mm");
            }
            catch
            {
                return timestamp.ToString();
            }
        }

    }
}
