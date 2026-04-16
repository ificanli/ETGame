using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(HomePanelComponent))]
    public static partial class HomePanelComponentSystem
    {
        private const int PAGE_OVERVIEW = 0;
        private const int PAGE_MAIN_CITY_TASK = 1;
        private const int PAGE_MUSEUM = 2;
        private const int PAGE_RECYCLE = 3;
        private const int PAGE_FARM = 4;
        private const int PAGE_WAREHOUSE = 5;

        [EntitySystem]
        private static void YIUIInitialize(this HomePanelComponent self)
        {
            self.BindButtons();
        }

        [EntitySystem]
        private static void Destroy(this HomePanelComponent self)
        {
            self.LastHomeSnapshot = string.Empty;
            self.SelectedHomeSlotId = 0;
            self.SelectedHomeBuildingId = 0;
            self.CurrentPageMode = PAGE_OVERVIEW;
            self.SelectedMuseumWarehouseItemUid = 0;
            self.SelectedRecycleWarehouseItemUid = 0;
            self.SelectedWarehouseItemUid = 0;
            self.NeedTimedRefresh = false;
            self.NextTimedRefreshSecond = 0;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this HomePanelComponent self)
        {
            EntityRef<HomePanelComponent> selfRef = self;
            self.CurrentPageMode = PAGE_OVERVIEW;
            self.LastHomeSnapshot = string.Empty;
            self.SelectedMuseumWarehouseItemUid = 0;
            self.SelectedRecycleWarehouseItemUid = 0;
            self.SelectedWarehouseItemUid = 0;
            self.NeedTimedRefresh = false;
            self.NextTimedRefreshSecond = 0;
            self.BindButtons();
            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            self.RefreshHomeUiNow(true);
            return true;
        }

        [EntitySystem]
        private static void Update(this HomePanelComponent self)
        {
            if (!self.NeedTimedRefresh)
            {
                return;
            }

            long currentSecond = TimeInfo.Instance.ServerNow() / 1000;
            if (currentSecond < self.NextTimedRefreshSecond)
            {
                return;
            }

            self.RefreshHomeUiNow();
        }

        public static async ETTask<bool> OpenHomePanelAsync(this Scene root)
        {
            if (!IsInHomeScene(root))
            {
                return false;
            }

            YIUIRootComponent yiuiRoot = root.YIUIRoot();
            if (yiuiRoot == null)
            {
                return false;
            }

            HomePanelComponent panel = root.YIUIMgr()?.GetPanel<HomePanelComponent>();
            bool openedNow = false;
            if (panel == null || panel.IsDisposed)
            {
                EntityRef<Scene> rootRef = root;
                await yiuiRoot.OpenPanelAsync<HomePanelComponent>();
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return false;
                }

                panel = root.YIUIMgr()?.GetPanel<HomePanelComponent>();
                openedNow = true;
            }

            if (panel == null || panel.IsDisposed)
            {
                return false;
            }

            if (!openedNow)
            {
                panel.RefreshHomeUiNow(true);
            }

            return true;
        }

        public static async ETTask CloseHomePanelAsync(this Scene root, bool tween = true)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            YIUIMgrComponent yiuiMgr = root.YIUIMgr();
            if (yiuiMgr == null || yiuiMgr.IsDisposed || yiuiMgr.GetPanel<HomePanelComponent>() == null)
            {
                return;
            }

            try
            {
                await yiuiMgr.ClosePanelAsync<HomePanelComponent>(tween);
            }
            catch (Exception exception)
            {
                Log.Warning($"[HomePanel] close panel ignored exception: {exception.Message}");
            }
        }

        public static void SelectSceneTarget(this HomePanelComponent self, int slotId, long buildingId)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectedHomeSlotId = slotId;
            self.SelectedHomeBuildingId = buildingId;
            self.SelectedMuseumWarehouseItemUid = 0;
            self.SelectedRecycleWarehouseItemUid = 0;
            self.SelectedWarehouseItemUid = 0;

            Scene root = self.Root();
            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            HomeClientBuildingData building = ResolveSelectedBuilding(runtime, slotId, buildingId);
            self.CurrentPageMode = ResolvePageModeBySelection(building);
            self.RefreshHomeUiNow(true);
        }

        public static void RefreshHomeUiNow(this HomePanelComponent self, bool force = false)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Scene root = self.Root();
            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            LoadoutComponent loadout = root?.GetComponent<LoadoutComponent>();
            bool inHome = IsInHomeScene(root);

            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData selectedBuilding = ResolveBuildingInSlot(selectedSlot, buildings);
            self.NormalizeSelectionForPage(runtime, buildings, ref selectedSlot, ref selectedBuilding);
            self.NormalizeSelectedWarehouseItemState(loadout);
            self.UpdateTimedRefreshState(selectedBuilding);

            string snapshot = BuildHomeSnapshot(self, runtime, loadout, selectedSlot, selectedBuilding, inHome);
            if (!force && self.LastHomeSnapshot == snapshot)
            {
                return;
            }

            self.LastHomeSnapshot = snapshot;
            self.RefreshTopBar(runtime, selectedSlot, selectedBuilding, buildings);
            self.RefreshNavButtons();
            self.RenderBuildingList(runtime, buildings, selectedSlot, selectedBuilding, inHome);
            self.RefreshDetailCard(selectedSlot, selectedBuilding, inHome, runtime);
            self.RenderPages(runtime, loadout, selectedBuilding, inHome);
            self.ForceRebuildLayouts();
        }

        private static void BindButtons(this HomePanelComponent self)
        {
            EntityRef<HomePanelComponent> selfRef = self;

            BindButton(self.u_ComCloseButton, selfRef, static panel =>
            {
                panel.CloseSelfAsync().Coroutine();
            });

            BindButton(self.u_ComOverviewButton, selfRef, static panel =>
            {
                panel.FocusPage(PAGE_OVERVIEW);
            });

            BindButton(self.u_ComMainCityTaskButton, selfRef, static panel =>
            {
                panel.FocusPage(PAGE_MAIN_CITY_TASK);
            });

            BindButton(self.u_ComMuseumButton, selfRef, static panel =>
            {
                panel.FocusPage(PAGE_MUSEUM);
            });

            BindButton(self.u_ComRecycleButton, selfRef, static panel =>
            {
                panel.FocusPage(PAGE_RECYCLE);
            });

            BindButton(self.u_ComFarmButton, selfRef, static panel =>
            {
                panel.FocusPage(PAGE_FARM);
            });

            BindButton(self.u_ComWarehouseButton, selfRef, static panel =>
            {
                panel.FocusPage(PAGE_WAREHOUSE);
            });

            BindButton(self.u_ComDetailPrimaryButton, selfRef, static panel =>
            {
                panel.HandlePrimaryAction();
            });

            BindButton(self.u_ComDetailUpgradeButton, selfRef, static panel =>
            {
                panel.UpgradeSelectedHomeBuildingAsync().Coroutine();
            });

            BindButton(self.u_ComDetailDemolishButton, selfRef, static panel =>
            {
                panel.DemolishSelectedHomeBuildingAsync().Coroutine();
            });

            BindButton(self.u_ComFarmCollectButton, selfRef, static panel =>
            {
                panel.CollectFarmAsync().Coroutine();
            });
        }

        private static void BindButton(Button button, EntityRef<HomePanelComponent> selfRef, Action<HomePanelComponent> action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                HomePanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                action(panel);
            });
        }

        private static void FocusPage(this HomePanelComponent self, int pageMode)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.CurrentPageMode = pageMode;
            self.SelectedMuseumWarehouseItemUid = 0;
            self.SelectedRecycleWarehouseItemUid = 0;
            self.SelectedWarehouseItemUid = 0;

            Scene root = self.Root();
            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            int expectedBuildingType = GetExpectedBuildingType(pageMode);
            if (expectedBuildingType > 0)
            {
                HomeClientBuildingData building = FindFirstBuildingByType(buildings, expectedBuildingType);
                if (building != null)
                {
                    self.SelectedHomeBuildingId = building.BuildingId;
                    self.SelectedHomeSlotId = building.SlotId;
                }
            }

            self.RefreshHomeUiNow(true);
        }

        private static void HandlePrimaryAction(this HomePanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Scene root = self.Root();
            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData selectedBuilding = ResolveBuildingInSlot(selectedSlot, buildings);

            if (selectedBuilding == null)
            {
                self.FocusPage(PAGE_OVERVIEW);
                return;
            }

            int buildingType = GetHomeBuildingType(selectedBuilding.ConfigId);
            self.CurrentPageMode = buildingType switch
            {
                HomeBuildingType.MainCity => PAGE_MAIN_CITY_TASK,
                HomeBuildingType.Museum => PAGE_MUSEUM,
                HomeBuildingType.RecycleRoom => PAGE_RECYCLE,
                HomeBuildingType.Farm => PAGE_FARM,
                HomeBuildingType.Warehouse => PAGE_WAREHOUSE,
                _ => PAGE_OVERVIEW,
            };
            self.RefreshHomeUiNow(true);
        }

        private static async ETTask CloseSelfAsync(this HomePanelComponent self)
        {
            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                return;
            }

            await root.CloseHomePanelAsync();
        }

        private static void RefreshTopBar(
            this HomePanelComponent self,
            HomeClientComponent runtime,
            HomeClientSlotData selectedSlot,
            HomeClientBuildingData selectedBuilding,
            List<HomeClientBuildingData> buildings)
        {
            int capacity = runtime?.WarehouseSummary?.Capacity ?? 0;
            int occupied = runtime?.WarehouseSummary?.OccupiedCellCount ?? 0;

            SetText(self.u_ComTitleText, "家园");
            SetText(
                self.u_ComSelectionHintText,
                selectedBuilding != null
                    ? $"当前选中：槽位 {selectedBuilding.SlotId} / {GetHomeBuildingName(selectedBuilding.ConfigId)}"
                    : selectedSlot != null
                        ? $"当前选中：槽位 {selectedSlot.SlotId} / {GetHomeSlotStatus(selectedSlot)}"
                        : "点击中间场景中的主城、建筑或空地，右侧会打开对应管理页。");
            SetText(self.u_ComTotalWealthText, $"金币 {runtime?.TotalWealth ?? 0}");
            SetText(self.u_ComMainCityLevelText, $"主城 Lv.{runtime?.MainCitySummary?.Level ?? 0}");
            SetText(self.u_ComWarehouseSummaryText, $"仓库 {occupied}/{capacity}");
            SetText(self.u_ComTodoHintText, BuildTopTodoHint(runtime));

            StringBuilder summary = new(128);
            summary.Append($"已建建筑 {buildings?.Count ?? 0} 个").Append('\n');
            summary.Append($"主城任务 {runtime?.MainCitySummary?.TaskFinishedCount ?? 0}/{runtime?.MainCitySummary?.TaskTotalCount ?? 0}").Append('\n');
            if (selectedBuilding != null)
            {
                summary.Append($"详情目标：{GetHomeBuildingName(selectedBuilding.ConfigId)} · 等级 {selectedBuilding.Level}");
            }
            else if (selectedSlot != null)
            {
                summary.Append($"详情目标：槽位 {selectedSlot.SlotId} · {GetHomeSlotStatus(selectedSlot)}");
            }
            else
            {
                summary.Append("详情目标：等待选择一个建筑或地块");
            }

            SetText(self.u_ComOverviewSummaryText, summary.ToString());
            SetText(self.u_ComPageTitleText, GetPageTitle(self.CurrentPageMode, selectedBuilding));
            SetText(self.u_ComPageSubtitleText, GetPageSubtitle(self.CurrentPageMode, runtime, selectedBuilding));
        }

        private static void RefreshNavButtons(this HomePanelComponent self)
        {
            SetNavSelected(self.u_ComOverviewButton, self.CurrentPageMode == PAGE_OVERVIEW);
            SetNavSelected(self.u_ComMainCityTaskButton, self.CurrentPageMode == PAGE_MAIN_CITY_TASK);
            SetNavSelected(self.u_ComMuseumButton, self.CurrentPageMode == PAGE_MUSEUM);
            SetNavSelected(self.u_ComRecycleButton, self.CurrentPageMode == PAGE_RECYCLE);
            SetNavSelected(self.u_ComFarmButton, self.CurrentPageMode == PAGE_FARM);
            SetNavSelected(self.u_ComWarehouseButton, self.CurrentPageMode == PAGE_WAREHOUSE);
        }

        private static void RenderPages(
            this HomePanelComponent self,
            HomeClientComponent runtime,
            LoadoutComponent loadout,
            HomeClientBuildingData selectedBuilding,
            bool inHome)
        {
            SetVisible(self.u_ComOverviewPageRoot, self.CurrentPageMode == PAGE_OVERVIEW);
            SetVisible(self.u_ComTaskPageRoot, self.CurrentPageMode == PAGE_MAIN_CITY_TASK);
            SetVisible(self.u_ComMuseumPageRoot, self.CurrentPageMode == PAGE_MUSEUM);
            SetVisible(self.u_ComRecyclePageRoot, self.CurrentPageMode == PAGE_RECYCLE);
            SetVisible(self.u_ComFarmPageRoot, self.CurrentPageMode == PAGE_FARM);
            SetVisible(self.u_ComWarehousePageRoot, self.CurrentPageMode == PAGE_WAREHOUSE);

            switch (self.CurrentPageMode)
            {
                case PAGE_MAIN_CITY_TASK:
                    self.RenderTaskPage(runtime);
                    break;
                case PAGE_MUSEUM:
                    self.RenderMuseumPage(runtime, loadout, inHome);
                    break;
                case PAGE_RECYCLE:
                    self.RenderRecyclePage(runtime, loadout, inHome);
                    break;
                case PAGE_FARM:
                    self.RenderFarmPage(runtime, inHome);
                    break;
                case PAGE_WAREHOUSE:
                    self.RenderWarehousePage(runtime, loadout);
                    break;
                default:
                    self.RenderOverviewPage(runtime, selectedBuilding, inHome);
                    break;
            }
        }

        private static void RenderBuildingList(
            this HomePanelComponent self,
            HomeClientComponent runtime,
            List<HomeClientBuildingData> buildings,
            HomeClientSlotData selectedSlot,
            HomeClientBuildingData selectedBuilding,
            bool inHome)
        {
            RectTransform content = self.u_ComBuildingListContent;
            Button template = self.u_ComBuildingItemTemplate;
            if (content == null || template == null)
            {
                return;
            }

            ClearTemplateChildren(content, template.transform);
            List<HomeClientSlotData> slots = BuildSortedHomeSlots(runtime);
            bool hasData = slots.Count > 0;
            SetVisible(self.u_ComBuildingListEmptyText, !hasData);
            if (self.u_ComBuildingListEmptyText != null)
            {
                self.u_ComBuildingListEmptyText.text = "暂无家园槽位数据";
            }

            if (!hasData)
            {
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            for (int i = 0; i < slots.Count; ++i)
            {
                HomeClientSlotData slot = slots[i];
                HomeClientBuildingData building = ResolveBuildingInSlot(slot, buildings);
                Button item = CloneButtonTemplate(template, $"BuildingItem_{slot.SlotId}");
                SetCardButtonContent(item, BuildHomeSlotTitle(slot, building), BuildHomeSlotSubtitle(slot, building));
                SetCardButtonSelected(item, selectedSlot != null && selectedSlot.SlotId == slot.SlotId, GetBuildingItemColor(slot, building));

                int slotId = slot.SlotId;
                long buildingId = building?.BuildingId ?? 0;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() =>
                {
                    HomePanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.SelectSceneTarget(slotId, buildingId);
                });

                if (!inHome)
                {
                    item.interactable = false;
                }
            }
        }

        private static void RefreshDetailCard(
            this HomePanelComponent self,
            HomeClientSlotData slot,
            HomeClientBuildingData building,
            bool inHome,
            HomeClientComponent runtime)
        {
            if (slot == null)
            {
                SetText(self.u_ComDetailNameText, "未选中建筑");
                SetText(self.u_ComDetailStatusText, "状态：等待选择");
                SetText(self.u_ComDetailHintText, inHome ? "请先点击中间场景中的建筑或槽位。" : "当前不在家园场景，正式页仅保留展示。");
                SetButtonText(self.u_ComDetailPrimaryButton, "查看");
                SetButtonText(self.u_ComDetailUpgradeButton, "升级");
                SetButtonText(self.u_ComDetailDemolishButton, "拆除");
                SetButtonInteractable(self.u_ComDetailPrimaryButton, false);
                SetButtonInteractable(self.u_ComDetailUpgradeButton, false);
                SetButtonInteractable(self.u_ComDetailDemolishButton, false);
                return;
            }

            if (building == null)
            {
                SetText(self.u_ComDetailNameText, BuildHomeSlotTitle(slot, null));
                SetText(self.u_ComDetailStatusText, $"状态：{GetHomeSlotStatus(slot)}");
                SetText(self.u_ComDetailHintText, BuildHomeEmptySlotDetailHint(slot, inHome));
                SetButtonText(self.u_ComDetailPrimaryButton, "查看建造");
                SetButtonText(self.u_ComDetailUpgradeButton, "升级");
                SetButtonText(self.u_ComDetailDemolishButton, "拆除");
                SetButtonInteractable(self.u_ComDetailPrimaryButton, inHome && slot.Unlocked && slot.SlotType == HomeSlotType.Buildable);
                SetButtonInteractable(self.u_ComDetailUpgradeButton, false);
                SetButtonInteractable(self.u_ComDetailDemolishButton, false);
                return;
            }

            int buildingType = GetHomeBuildingType(building.ConfigId);
            bool canUpgrade = inHome && CanUpgradeHomeBuilding(building, buildingType, runtime);
            bool canDemolish = inHome && CanDemolishHomeBuilding(building, buildingType, runtime);

            SetText(self.u_ComDetailNameText, $"槽位 {building.SlotId} / {GetHomeBuildingName(building.ConfigId)}");
            SetText(self.u_ComDetailStatusText, BuildHomeDetailStatus(building, buildingType, runtime));
            SetText(self.u_ComDetailHintText, BuildHomeDetailHint(building, buildingType, runtime));

            SetButtonText(self.u_ComDetailPrimaryButton, GetHomePrimaryButtonText(buildingType));
            SetButtonText(self.u_ComDetailUpgradeButton, buildingType == HomeBuildingType.MainCity ? "升级主城" : "升级建筑");
            SetButtonText(self.u_ComDetailDemolishButton, buildingType == HomeBuildingType.MainCity ? "主城不可拆" : "拆除建筑");
            SetButtonInteractable(self.u_ComDetailPrimaryButton, true);
            SetButtonInteractable(self.u_ComDetailUpgradeButton, canUpgrade);
            SetButtonInteractable(self.u_ComDetailDemolishButton, canDemolish);
        }

        private static void RenderOverviewPage(this HomePanelComponent self, HomeClientComponent runtime, HomeClientBuildingData selectedBuilding, bool inHome)
        {
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            List<int> configIds = runtime != null ? runtime.GetBuildablePrototypeConfigIds() : new List<int>();
            int buildableCount = 0;

            SetText(self.u_ComBuildHintText, BuildHomeBuildEntryHint(selectedSlot, selectedBuilding, inHome));

            RectTransform content = self.u_ComBuildOptionContent;
            Button template = self.u_ComBuildOptionTemplate;
            if (content == null || template == null)
            {
                return;
            }

            ClearTemplateChildren(content, template.transform);

            bool canShowBuildOptions = inHome &&
                selectedBuilding == null &&
                selectedSlot != null &&
                selectedSlot.Unlocked &&
                selectedSlot.SlotType == HomeSlotType.Buildable &&
                selectedSlot.BuildingId <= 0;

            if (canShowBuildOptions)
            {
                EntityRef<HomePanelComponent> selfRef = self;
                for (int i = 0; i < configIds.Count; ++i)
                {
                    int configId = configIds[i];
                    if (!CanBuildInSlot(selectedSlot, configId))
                    {
                        continue;
                    }

                    ++buildableCount;
                    HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
                    Button item = CloneButtonTemplate(template, $"BuildOption_{configId}");
                    SetCardButtonContent(
                        item,
                        GetHomeBuildingName(configId),
                        config != null && config.BuildGoldCost > 0
                            ? $"建造消耗 {config.BuildGoldCost} 金币"
                            : "当前可建");
                    SetCardButtonSelected(item, false, GetBuildOptionColor(configId));

                    int capturedConfigId = configId;
                    item.onClick.RemoveAllListeners();
                    item.onClick.AddListener(() =>
                    {
                        HomePanelComponent panel = selfRef;
                        if (panel == null || panel.IsDisposed)
                        {
                            return;
                        }

                        panel.BuildHomePrototypeAsync(capturedConfigId).Coroutine();
                    });
                }
            }

            bool showEmpty = buildableCount <= 0;
            SetVisible(self.u_ComOverviewEmptyText, showEmpty);
            if (self.u_ComOverviewEmptyText != null)
            {
                self.u_ComOverviewEmptyText.text = canShowBuildOptions
                    ? "当前没有符合该槽位条件的可建建筑。"
                    : selectedBuilding != null
                        ? "当前已选中建筑。可使用上方详情卡或左侧页签进入对应管理页。"
                        : BuildHomeEmptySlotDetailHint(selectedSlot, inHome);
            }
        }

        private static void RenderTaskPage(this HomePanelComponent self, HomeClientComponent runtime)
        {
            HomeClientBuildingData mainCity = FindFirstBuildingByType(BuildSortedHomeBuildings(runtime), HomeBuildingType.MainCity);
            SetText(
                self.u_ComTaskProgressText,
                mainCity == null
                    ? "当前没有主城建筑。"
                    : $"任务进度 {runtime?.MainCitySummary?.TaskFinishedCount ?? 0}/{runtime?.MainCitySummary?.TaskTotalCount ?? 0} · {(runtime?.MainCitySummary?.CanUpgrade ?? false ? "满足升级条件" : "未完成全部任务")}");

            RectTransform content = self.u_ComTaskListContent;
            RectTransform template = self.u_ComTaskItemTemplate;
            if (content == null || template == null)
            {
                return;
            }

            ClearTemplateChildren(content, template);
            List<HomeClientMainCityTaskData> tasks = BuildSortedMainCityTasks(runtime);
            bool showEmpty = mainCity == null || tasks.Count == 0;
            SetVisible(self.u_ComTaskEmptyText, showEmpty);
            if (self.u_ComTaskEmptyText != null)
            {
                self.u_ComTaskEmptyText.text = mainCity == null ? "主城还未初始化。" : "当前没有主城任务。";
            }

            for (int i = 0; i < tasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = tasks[i];
                RectTransform item = CloneRectTemplate(template, $"TaskItem_{task.TaskId}");
                SetTemplateText(item, "TitleText", task.Title ?? $"任务 {task.TaskId}");
                SetTemplateText(item, "ProgressText", $"{task.Progress}/{task.Target}");
                SetTemplateText(item, "DescText", string.IsNullOrWhiteSpace(task.Desc) ? "暂无任务说明。" : task.Desc);
                Image image = item.GetComponent<Image>();
                if (image != null)
                {
                    image.color = task.Completed
                        ? new Color32(50, 92, 60, 255)
                        : new Color32(73, 63, 42, 255);
                }
            }
        }

        private static void RenderMuseumPage(this HomePanelComponent self, HomeClientComponent runtime, LoadoutComponent loadout, bool inHome)
        {
            HomeClientBuildingData museum = FindFirstBuildingByType(BuildSortedHomeBuildings(runtime), HomeBuildingType.Museum);
            int capacity = GetMuseumCapacity(museum);
            int displayCount = CountMuseumDisplays(runtime, museum?.BuildingId ?? 0);
            SetText(
                self.u_ComMuseumCapacityText,
                museum == null
                    ? "展示容量：尚未建造收藏馆"
                    : $"展示容量：{displayCount}/{capacity}");

            RectTransform gridContent = self.u_ComMuseumGridContent;
            Button gridTemplate = self.u_ComMuseumGridTemplate;
            RectTransform warehouseContent = self.u_ComMuseumWarehouseContent;
            Button warehouseTemplate = self.u_ComMuseumWarehouseTemplate;
            if (gridContent == null || gridTemplate == null || warehouseContent == null || warehouseTemplate == null)
            {
                return;
            }

            ClearTemplateChildren(gridContent, gridTemplate.transform);
            ClearTemplateChildren(warehouseContent, warehouseTemplate.transform);
            List<HomeClientMuseumDisplayData> displays = BuildSortedMuseumDisplays(runtime, museum?.BuildingId ?? 0);
            List<LoadoutWarehouseItemInfo> warehouseItems = BuildSortedWarehouseItems(loadout);
            bool hasSelectedWarehouseItem = TryGetWarehouseItem(loadout, self.SelectedMuseumWarehouseItemUid, out LoadoutWarehouseItemInfo selectedWarehouseItem);

            if (museum != null)
            {
                EntityRef<HomePanelComponent> selfRef = self;
                for (int slotIndex = 1; slotIndex <= Math.Max(capacity, 1); ++slotIndex)
                {
                    HomeClientMuseumDisplayData display = FindMuseumDisplayBySlotIndex(displays, slotIndex);
                    Button item = CloneButtonTemplate(gridTemplate, $"MuseumGrid_{slotIndex}");
                    if (display != null)
                    {
                        SetCardButtonContent(item, $"展示位 {slotIndex}", GetHomeItemDisplayName(display.ItemConfigId));
                        SetCardButtonSelected(item, false, new Color32(110, 66, 74, 255));
                        long displayId = display.DisplayId;
                        long buildingId = museum.BuildingId;
                        item.onClick.RemoveAllListeners();
                        item.onClick.AddListener(() =>
                        {
                            HomePanelComponent panel = selfRef;
                            if (panel == null || panel.IsDisposed)
                            {
                                return;
                            }

                            panel.TakeDownMuseumDisplayAsync(displayId, buildingId).Coroutine();
                        });
                        item.interactable = inHome;
                    }
                    else
                    {
                        string subtitle = hasSelectedWarehouseItem
                            ? $"点击摆入 {GetHomeItemDisplayName(selectedWarehouseItem.ConfigId)}"
                            : "先在下方选择一个仓库物品";
                        SetCardButtonContent(item, $"空展示位 {slotIndex}", subtitle);
                        SetCardButtonSelected(item, false, new Color32(80, 58, 64, 255));
                        long buildingId = museum.BuildingId;
                        item.onClick.RemoveAllListeners();
                        item.onClick.AddListener(() =>
                        {
                            HomePanelComponent panel = selfRef;
                            if (panel == null || panel.IsDisposed)
                            {
                                return;
                            }

                            if (panel.SelectedMuseumWarehouseItemUid <= 0)
                            {
                                ShowHomeTips(panel.Root(), "请先在下方选择一个仓库物品。");
                                return;
                            }

                            panel.PlaceMuseumDisplayAsync(buildingId, panel.SelectedMuseumWarehouseItemUid).Coroutine();
                        });
                        item.interactable = inHome && hasSelectedWarehouseItem;
                    }
                }
            }

            EntityRef<HomePanelComponent> warehouseSelfRef = self;
            for (int i = 0; i < warehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo itemData = warehouseItems[i];
                Button item = CloneButtonTemplate(warehouseTemplate, $"MuseumWarehouse_{itemData.ItemUid}");
                SetCardButtonContent(
                    item,
                    $"{GetHomeItemDisplayName(itemData.ConfigId)} x{itemData.Count}",
                    $"占格 {itemData.GridWidth}x{itemData.GridHeight}");
                SetCardButtonSelected(item, self.SelectedMuseumWarehouseItemUid == itemData.ItemUid, new Color32(87, 68, 42, 255));

                long itemUid = itemData.ItemUid;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() =>
                {
                    HomePanelComponent panel = warehouseSelfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.SelectedMuseumWarehouseItemUid = panel.SelectedMuseumWarehouseItemUid == itemUid ? 0 : itemUid;
                    panel.RefreshHomeUiNow(true);
                });
                item.interactable = museum != null;
            }

            if (museum == null)
            {
                SetText(self.u_ComMuseumHintText, "尚未建造大红收藏馆，当前无法摆放展示单位。");
            }
            else if (!inHome)
            {
                SetText(self.u_ComMuseumHintText, "当前不在家园场景，只能查看收藏馆数据。");
            }
            else if (hasSelectedWarehouseItem)
            {
                SetText(self.u_ComMuseumHintText, $"当前已选择 {GetHomeItemDisplayName(selectedWarehouseItem.ConfigId)}，点击上方空展示位即可摆放。");
            }
            else
            {
                SetText(self.u_ComMuseumHintText, "点击下方仓库物品进行选择，再点击上方空展示位摆放；点击已摆放单位可取下。");
            }
        }

        private static void RenderRecyclePage(this HomePanelComponent self, HomeClientComponent runtime, LoadoutComponent loadout, bool inHome)
        {
            HomeClientBuildingData recycleRoom = FindFirstBuildingByType(BuildSortedHomeBuildings(runtime), HomeBuildingType.RecycleRoom);
            RectTransform slotContent = self.u_ComRecycleSlotContent;
            Button slotTemplate = self.u_ComRecycleSlotTemplate;
            RectTransform sourceContent = self.u_ComRecycleSourceContent;
            Button sourceTemplate = self.u_ComRecycleSourceTemplate;
            if (slotContent == null || slotTemplate == null || sourceContent == null || sourceTemplate == null)
            {
                return;
            }

            ClearTemplateChildren(slotContent, slotTemplate.transform);
            ClearTemplateChildren(sourceContent, sourceTemplate.transform);
            List<LoadoutWarehouseItemInfo> warehouseItems = BuildSortedWarehouseItems(loadout);
            bool hasSelectedSource = TryGetWarehouseItem(loadout, self.SelectedRecycleWarehouseItemUid, out LoadoutWarehouseItemInfo selectedSource);

            if (recycleRoom == null)
            {
                SetText(self.u_ComRecycleSummaryText, "回收位概览：尚未建造回收间");
                SetText(self.u_ComRecycleHintText, "先建造物品回收间，才能把仓库物品放进处理位。");
            }
            else
            {
                int capacity = GetRecycleSlotCapacity(recycleRoom);
                int readyCount = CountReadyRecycleOrders(runtime, recycleRoom.BuildingId);
                int runningCount = CountRunningRecycleOrders(runtime, recycleRoom.BuildingId);
                SetText(
                    self.u_ComRecycleSummaryText,
                    $"回收位概览：{readyCount} 个待收取，{runningCount}/{capacity} 个处理中");
                SetText(
                    self.u_ComRecycleHintText,
                    !inHome
                        ? "当前不在家园场景，只能查看回收间状态。"
                        : hasSelectedSource
                            ? $"当前已选材料 {GetHomeItemDisplayName(selectedSource.ConfigId)}，点击上方空回收位即可开工。"
                            : "先点下方仓库物品，再点上方空回收位开工；已完成回收位可直接点卡片收取。");

                List<HomeClientProductionOrderData> readyOrders = BuildRecycleOrders(runtime, recycleRoom.BuildingId, HomeProductionState.Completed);
                List<HomeClientProductionOrderData> runningOrders = BuildRecycleOrders(runtime, recycleRoom.BuildingId, HomeProductionState.InProgress);
                EntityRef<HomePanelComponent> selfRef = self;
                for (int i = 0; i < capacity; ++i)
                {
                    Button item = CloneButtonTemplate(slotTemplate, $"RecycleSlot_{i + 1}");
                    if (i < readyOrders.Count)
                    {
                        HomeClientProductionOrderData order = readyOrders[i];
                        SetCardButtonContent(
                            item,
                            $"回收位 {i + 1} / 已完成",
                            $"材料 {GetHomeItemDisplayName(order.RecipeId)} · 点击收取");
                        SetCardButtonSelected(item, false, new Color32(78, 110, 72, 255));
                        long orderId = order.OrderId;
                        long collectBuildingId = recycleRoom.BuildingId;
                        item.onClick.RemoveAllListeners();
                        item.onClick.AddListener(() =>
                        {
                            HomePanelComponent panel = selfRef;
                            if (panel == null || panel.IsDisposed)
                            {
                                return;
                            }

                            panel.CollectRecycleOrderAsync(orderId, collectBuildingId).Coroutine();
                        });
                        item.interactable = inHome;
                        continue;
                    }

                    int runningIndex = i - readyOrders.Count;
                    if (runningIndex >= 0 && runningIndex < runningOrders.Count)
                    {
                        HomeClientProductionOrderData order = runningOrders[runningIndex];
                        SetCardButtonContent(
                            item,
                            $"回收位 {i + 1} / 处理中",
                            $"材料 {GetHomeItemDisplayName(order.RecipeId)} · 完成 {FormatHomeDateTime(order.FinishTime)}");
                        SetCardButtonSelected(item, false, new Color32(62, 98, 110, 255));
                        item.interactable = false;
                        continue;
                    }

                    SetCardButtonContent(
                        item,
                        $"回收位 {i + 1} / 空闲",
                        hasSelectedSource ? $"点击放入 {GetHomeItemDisplayName(selectedSource.ConfigId)}" : "先在下方选择一个仓库物品");
                    SetCardButtonSelected(item, false, new Color32(66, 74, 82, 255));
                    long startBuildingId = recycleRoom.BuildingId;
                    item.onClick.RemoveAllListeners();
                    item.onClick.AddListener(() =>
                    {
                        HomePanelComponent panel = selfRef;
                        if (panel == null || panel.IsDisposed)
                        {
                            return;
                        }

                        if (panel.SelectedRecycleWarehouseItemUid <= 0)
                        {
                            ShowHomeTips(panel.Root(), "请先在下方选择一个仓库物品。");
                            return;
                        }

                        panel.StartRecycleAsync(startBuildingId).Coroutine();
                    });
                    item.interactable = inHome && hasSelectedSource;
                }
            }

            EntityRef<HomePanelComponent> sourceSelfRef = self;
            for (int i = 0; i < warehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo itemData = warehouseItems[i];
                Button item = CloneButtonTemplate(sourceTemplate, $"RecycleSource_{itemData.ItemUid}");
                SetCardButtonContent(
                    item,
                    $"{GetHomeItemDisplayName(itemData.ConfigId)} x{itemData.Count}",
                    $"占格 {itemData.GridWidth}x{itemData.GridHeight}");
                SetCardButtonSelected(item, self.SelectedRecycleWarehouseItemUid == itemData.ItemUid, new Color32(95, 82, 50, 255));
                long itemUid = itemData.ItemUid;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() =>
                {
                    HomePanelComponent panel = sourceSelfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.SelectedRecycleWarehouseItemUid = panel.SelectedRecycleWarehouseItemUid == itemUid ? 0 : itemUid;
                    panel.RefreshHomeUiNow(true);
                });
                item.interactable = recycleRoom != null;
            }
        }

        private static void RenderFarmPage(this HomePanelComponent self, HomeClientComponent runtime, bool inHome)
        {
            HomeClientBuildingData farm = FindFirstBuildingByType(BuildSortedHomeBuildings(runtime), HomeBuildingType.Farm);
            if (farm == null)
            {
                SetText(self.u_ComFarmSummaryText, "尚未建造农场。");
                SetText(self.u_ComFarmHintText, "建造农场后，会按时间累计金币。");
                SetButtonInteractable(self.u_ComFarmCollectButton, false);
                return;
            }

            HomeBuildingLevelConfig levelConfig = GetHomeBuildingLevelConfig(farm.ConfigId, farm.Level);
            int intervalMs = GetFarmCollectIntervalMs(farm);
            long wealth = PeekHomeFarmWealth(farm);
            SetText(
                self.u_ComFarmSummaryText,
                $"当前等级 {farm.Level}\n每轮产出 {levelConfig?.OutputValue ?? 0} 金币\n产出间隔 {FormatDuration(intervalMs)}\n待收金币 {wealth}");
            SetText(
                self.u_ComFarmHintText,
                inHome
                    ? wealth > 0 ? "当前已有金币可收取。" : "农场正在继续产出金币。"
                    : "当前不在家园场景，只能查看农场产出信息。");
            SetButtonInteractable(self.u_ComFarmCollectButton, inHome && wealth > 0);
        }

        private static void RenderWarehousePage(this HomePanelComponent self, HomeClientComponent runtime, LoadoutComponent loadout)
        {
            HomeClientBuildingData warehouse = FindFirstBuildingByType(BuildSortedHomeBuildings(runtime), HomeBuildingType.Warehouse);
            int capacity = runtime?.WarehouseSummary?.Capacity ?? 0;
            int occupied = runtime?.WarehouseSummary?.OccupiedCellCount ?? 0;
            int remain = Math.Max(capacity - occupied, 0);
            RectTransform content = self.u_ComWarehouseListContent;
            Button template = self.u_ComWarehouseListTemplate;
            if (content == null || template == null)
            {
                return;
            }

            ClearTemplateChildren(content, template.transform);
            SetText(
                self.u_ComWarehouseCapacityText,
                warehouse == null
                    ? "仓库容量：尚未建造仓库"
                    : $"仓库容量：{occupied}/{capacity} · 剩余 {remain}");

            List<LoadoutWarehouseItemInfo> items = warehouse == null ? new List<LoadoutWarehouseItemInfo>() : BuildSortedWarehouseItems(loadout);
            bool showEmpty = items.Count == 0;
            SetVisible(self.u_ComWarehouseEmptyText, showEmpty);
            if (self.u_ComWarehouseEmptyText != null)
            {
                self.u_ComWarehouseEmptyText.text = warehouse == null ? "尚未建造仓库。" : "仓库当前为空。";
            }

            EntityRef<HomePanelComponent> selfRef = self;
            for (int i = 0; i < items.Count; ++i)
            {
                LoadoutWarehouseItemInfo itemData = items[i];
                Button item = CloneButtonTemplate(template, $"WarehouseItem_{itemData.ItemUid}");
                SetCardButtonContent(
                    item,
                    $"{GetHomeItemDisplayName(itemData.ConfigId)} x{itemData.Count}",
                    $"占格 {itemData.GridWidth}x{itemData.GridHeight}");
                SetCardButtonSelected(item, self.SelectedWarehouseItemUid == itemData.ItemUid, new Color32(97, 86, 54, 255));

                long itemUid = itemData.ItemUid;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() =>
                {
                    HomePanelComponent panel = selfRef;
                    if (panel == null || panel.IsDisposed)
                    {
                        return;
                    }

                    panel.SelectedWarehouseItemUid = panel.SelectedWarehouseItemUid == itemUid ? 0 : itemUid;
                    panel.RefreshHomeUiNow(true);
                });
            }

            SetText(self.u_ComWarehouseDetailText, BuildWarehouseDetailText(loadout, self.SelectedWarehouseItemUid, warehouse != null, capacity, occupied));
        }

        private static async ETTask BuildHomePrototypeAsync(this HomePanelComponent self, int configId)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行建造。");
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            if (selectedSlot == null || !CanBuildInSlot(selectedSlot, configId))
            {
                ShowHomeTips(root, "当前槽位不能建造该建筑。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            int slotId = selectedSlot.SlotId;
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

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self.SelectSceneTarget(slotId, response.Building?.BuildingId ?? 0);
            ShowHomeTips(root, $"建造完成：{GetHomeBuildingName(configId)}。");
        }

        private static async ETTask UpgradeSelectedHomeBuildingAsync(this HomePanelComponent self)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行升级。");
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData selectedBuilding = ResolveBuildingInSlot(selectedSlot, buildings);
            if (selectedBuilding == null)
            {
                ShowHomeTips(root, "当前没有可升级的建筑。");
                return;
            }

            int buildingType = GetHomeBuildingType(selectedBuilding.ConfigId);
            if (!CanUpgradeHomeBuilding(selectedBuilding, buildingType, runtime))
            {
                ShowHomeTips(root, "当前不满足升级条件。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            long buildingId = selectedBuilding.BuildingId;
            int slotId = selectedBuilding.SlotId;
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

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectSceneTarget(slotId, buildingId);
        }

        private static async ETTask DemolishSelectedHomeBuildingAsync(this HomePanelComponent self)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行拆除。");
                return;
            }

            HomeClientComponent runtime = HomeClientHelper.GetOrAddRuntime(root);
            List<HomeClientBuildingData> buildings = BuildSortedHomeBuildings(runtime);
            HomeClientSlotData selectedSlot = ResolveSelectedHomeSlot(self, runtime, buildings);
            HomeClientBuildingData selectedBuilding = ResolveBuildingInSlot(selectedSlot, buildings);
            if (selectedBuilding == null)
            {
                ShowHomeTips(root, "当前没有可拆除的建筑。");
                return;
            }

            int buildingType = GetHomeBuildingType(selectedBuilding.ConfigId);
            if (!CanDemolishHomeBuilding(selectedBuilding, buildingType, runtime))
            {
                ShowHomeTips(root, "当前建筑暂时不能拆除。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            long buildingId = selectedBuilding.BuildingId;
            int slotId = selectedBuilding.SlotId;
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

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.CurrentPageMode = PAGE_OVERVIEW;
            self.SelectSceneTarget(slotId, 0);
        }

        private static async ETTask CollectFarmAsync(this HomePanelComponent self)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再执行收取。");
                return;
            }

            HomeClientBuildingData farm = FindFirstBuildingByType(BuildSortedHomeBuildings(HomeClientHelper.GetOrAddRuntime(root)), HomeBuildingType.Farm);
            if (farm == null)
            {
                ShowHomeTips(root, "当前没有农场可收取。");
                return;
            }

            long pendingWealth = PeekHomeFarmWealth(farm);
            if (pendingWealth <= 0)
            {
                ShowHomeTips(root, "当前没有可收取的农场金币。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeCollectResponse response;
            try
            {
                response = await HomeClientRequestHelper.Collect(root, farm.BuildingId);
            }
            catch (RpcException exception)
            {
                root = rootRef;
                if (root != null && !root.IsDisposed)
                {
                    Log.Error(exception);
                    ShowHomeTips(root, BuildHomeErrorText("收取失败", exception.Error, null));
                }

                return;
            }
            catch (Exception exception)
            {
                root = rootRef;
                if (root != null && !root.IsDisposed)
                {
                    Log.Error(exception);
                    ShowHomeTips(root, "收取失败：网络或状态异常，请稍后重试。");
                }

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

            HomeClientHelper.ApplyCollect(root, farm.BuildingId);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            self.SelectSceneTarget(farm.SlotId, farm.BuildingId);
            self.CurrentPageMode = PAGE_FARM;
            self.RefreshHomeUiNow(true);
            ShowHomeTips(root, BuildCollectSuccessText(response));
        }

        private static async ETTask StartRecycleAsync(this HomePanelComponent self, long buildingId)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再开始回收。");
                return;
            }

            LoadoutComponent loadout = root.GetComponent<LoadoutComponent>();
            if (!TryGetWarehouseItem(loadout, self.SelectedRecycleWarehouseItemUid, out LoadoutWarehouseItemInfo selectedSource) ||
                selectedSource.ConfigId <= 0)
            {
                ShowHomeTips(root, "请先选择一个仓库物品作为回收材料。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeStartProductionResponse response;
            try
            {
                response = await HomeClientRequestHelper.StartProduction(root, buildingId, selectedSource.ConfigId);
            }
            catch (RpcException exception)
            {
                root = rootRef;
                if (root != null && !root.IsDisposed)
                {
                    Log.Error(exception);
                    ShowHomeTips(root, BuildHomeErrorText("开始回收失败", exception.Error, null));
                }

                return;
            }
            catch (Exception exception)
            {
                root = rootRef;
                if (root != null && !root.IsDisposed)
                {
                    Log.Error(exception);
                    ShowHomeTips(root, "开始回收失败：网络或状态异常，请稍后重试。");
                }

                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                ShowHomeTips(root, "开始回收失败：未收到服务端响应。");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                ShowHomeTips(root, BuildHomeErrorText("开始回收失败", response.Error, response.Message));
                return;
            }

            HomeClientHelper.ApplyStartProductionResponse(root, response);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            HomeClientBuildingData building = HomeClientHelper.GetOrAddRuntime(root).GetBuilding(buildingId);
            self.SelectSceneTarget(building?.SlotId ?? self.SelectedHomeSlotId, buildingId);
            self.CurrentPageMode = PAGE_RECYCLE;
            self.RefreshHomeUiNow(true);
            ShowHomeTips(root, $"已放入 {GetHomeItemDisplayName(selectedSource.ConfigId)}，开始回收。");
        }

        private static async ETTask CollectRecycleOrderAsync(this HomePanelComponent self, long orderId, long buildingId)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再收取回收产物。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeCollectProductionResponse response;
            try
            {
                response = await HomeClientRequestHelper.CollectProduction(root, orderId);
            }
            catch (RpcException exception)
            {
                root = rootRef;
                if (root != null && !root.IsDisposed)
                {
                    Log.Error(exception);
                    ShowHomeTips(root, BuildHomeErrorText("收取失败", exception.Error, null));
                }

                return;
            }
            catch (Exception exception)
            {
                root = rootRef;
                if (root != null && !root.IsDisposed)
                {
                    Log.Error(exception);
                    ShowHomeTips(root, "收取失败：网络或状态异常，请稍后重试。");
                }

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

            HomeClientHelper.ApplyCollectProduction(root, orderId);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            HomeClientBuildingData building = HomeClientHelper.GetOrAddRuntime(root).GetBuilding(buildingId);
            self.SelectSceneTarget(building?.SlotId ?? self.SelectedHomeSlotId, buildingId);
            self.CurrentPageMode = PAGE_RECYCLE;
            self.RefreshHomeUiNow(true);
            ShowHomeTips(root, BuildCollectProductionSuccessText(response));
        }

        private static async ETTask PlaceMuseumDisplayAsync(this HomePanelComponent self, long buildingId, long itemUid)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再管理收藏馆。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeMuseumPlaceResponse response = await HomeClientRequestHelper.MuseumPlace(root, buildingId, itemUid);

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

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            HomeClientBuildingData building = HomeClientHelper.GetOrAddRuntime(root).GetBuilding(buildingId);
            self.SelectSceneTarget(building?.SlotId ?? self.SelectedHomeSlotId, buildingId);
            self.CurrentPageMode = PAGE_MUSEUM;
            self.RefreshHomeUiNow(true);
            ShowHomeTips(root, $"已将 {GetHomeItemDisplayName(response.ItemConfigId)} 摆入收藏馆。");
        }

        private static async ETTask TakeDownMuseumDisplayAsync(this HomePanelComponent self, long displayId, long buildingId)
        {
            Scene root = self.Root();
            if (!IsInHomeScene(root))
            {
                ShowHomeTips(root, "请先进入家园场景再管理收藏馆。");
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            M2C_HomeMuseumTakeDownResponse response = await HomeClientRequestHelper.MuseumTakeDown(root, displayId);

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

            await self.RefreshHomeLoadoutSnapshotAsync(false);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            HomeClientBuildingData building = HomeClientHelper.GetOrAddRuntime(root).GetBuilding(buildingId);
            self.SelectSceneTarget(building?.SlotId ?? self.SelectedHomeSlotId, buildingId);
            self.CurrentPageMode = PAGE_MUSEUM;
            self.RefreshHomeUiNow(true);
            ShowHomeTips(root, $"已将 {GetHomeItemDisplayName(response.ItemConfigId)} 取下并放回仓库。");
        }

        private static async ETTask RefreshHomeLoadoutSnapshotAsync(this HomePanelComponent self, bool refreshUi = true)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                return;
            }

            LoadoutComponent loadout = root.GetComponent<LoadoutComponent>() ?? root.AddComponent<LoadoutComponent>();
            ClientSenderComponent sender = root.GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                return;
            }

            EntityRef<HomePanelComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;

            C2G_GetHeroList request = C2G_GetHeroList.Create();
            request.WarehouseColumnCount = loadout.WarehouseColumnCount;
            G2C_GetHeroList response = await sender.Call(request) as G2C_GetHeroList;

            root = rootRef;
            if (root == null || root.IsDisposed || response == null || response.Error != ErrorCode.ERR_Success)
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            loadout = root.GetComponent<LoadoutComponent>() ?? root.AddComponent<LoadoutComponent>();
            loadout.Heroes.Clear();
            for (int i = 0; i < response.Heroes.Count; ++i)
            {
                HeroInfoData hero = response.Heroes[i];
                loadout.Heroes.Add(new HeroInfo
                {
                    HeroConfigId = hero.HeroConfigId,
                    Name = hero.Name,
                    UnitConfigId = hero.UnitConfigId,
                });
            }

            LoadoutClientStateHelper.ApplyGetHeroList(loadout, response);
            if (refreshUi)
            {
                self.RefreshHomeUiNow(true);
            }
        }

        private static void NormalizeSelectionForPage(
            this HomePanelComponent self,
            HomeClientComponent runtime,
            List<HomeClientBuildingData> buildings,
            ref HomeClientSlotData selectedSlot,
            ref HomeClientBuildingData selectedBuilding)
        {
            int expectedBuildingType = GetExpectedBuildingType(self.CurrentPageMode);
            if (expectedBuildingType <= 0)
            {
                return;
            }

            if (selectedBuilding != null && GetHomeBuildingType(selectedBuilding.ConfigId) == expectedBuildingType)
            {
                return;
            }

            HomeClientBuildingData building = FindFirstBuildingByType(buildings, expectedBuildingType);
            if (building == null)
            {
                return;
            }

            self.SelectedHomeBuildingId = building.BuildingId;
            self.SelectedHomeSlotId = building.SlotId;
            selectedBuilding = building;
            selectedSlot = runtime?.GetSlot(building.SlotId);
        }

        private static void NormalizeSelectedWarehouseItemState(this HomePanelComponent self, LoadoutComponent loadout)
        {
            if (!TryGetWarehouseItem(loadout, self.SelectedMuseumWarehouseItemUid, out _))
            {
                self.SelectedMuseumWarehouseItemUid = 0;
            }

            if (!TryGetWarehouseItem(loadout, self.SelectedRecycleWarehouseItemUid, out _))
            {
                self.SelectedRecycleWarehouseItemUid = 0;
            }

            if (!TryGetWarehouseItem(loadout, self.SelectedWarehouseItemUid, out _))
            {
                self.SelectedWarehouseItemUid = 0;
            }
        }

        private static void UpdateTimedRefreshState(this HomePanelComponent self, HomeClientBuildingData selectedBuilding)
        {
            bool needTimedRefresh = RequiresTimedRefresh(self.CurrentPageMode, selectedBuilding);
            self.NeedTimedRefresh = needTimedRefresh;
            self.NextTimedRefreshSecond = needTimedRefresh ? TimeInfo.Instance.ServerNow() / 1000 + 1 : 0;
        }

        private static void ForceRebuildLayouts(this HomePanelComponent self)
        {
            if (self.u_ComBuildingListContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComBuildingListContent);
            }

            switch (self.CurrentPageMode)
            {
                case PAGE_MAIN_CITY_TASK:
                    if (self.u_ComTaskListContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComTaskListContent);
                    }

                    break;
                case PAGE_MUSEUM:
                    if (self.u_ComMuseumGridContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComMuseumGridContent);
                    }

                    if (self.u_ComMuseumWarehouseContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComMuseumWarehouseContent);
                    }

                    break;
                case PAGE_RECYCLE:
                    if (self.u_ComRecycleSlotContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComRecycleSlotContent);
                    }

                    if (self.u_ComRecycleSourceContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComRecycleSourceContent);
                    }

                    break;
                case PAGE_WAREHOUSE:
                    if (self.u_ComWarehouseListContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComWarehouseListContent);
                    }

                    break;
                default:
                    if (self.u_ComBuildOptionContent != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(self.u_ComBuildOptionContent);
                    }

                    break;
            }
        }

        private static string BuildHomeSnapshot(
            HomePanelComponent self,
            HomeClientComponent runtime,
            LoadoutComponent loadout,
            HomeClientSlotData selectedSlot,
            HomeClientBuildingData selectedBuilding,
            bool inHome)
        {
            StringBuilder builder = new(1024);
            builder.Append(inHome ? '1' : '0').Append('|')
                .Append(self.CurrentPageMode).Append('|')
                .Append(self.SelectedHomeSlotId).Append('|')
                .Append(self.SelectedHomeBuildingId).Append('|')
                .Append(self.SelectedMuseumWarehouseItemUid).Append('|')
                .Append(self.SelectedRecycleWarehouseItemUid).Append('|')
                .Append(self.SelectedWarehouseItemUid).Append('|')
                .Append(runtime?.HomeVersion ?? 0).Append('|')
                .Append(runtime?.TotalWealth ?? 0).Append('|')
                .Append(runtime?.MainCitySummary?.Level ?? 0).Append(':')
                .Append(runtime?.MainCitySummary?.TaskFinishedCount ?? 0).Append(':')
                .Append(runtime?.MainCitySummary?.TaskTotalCount ?? 0).Append(':')
                .Append(runtime?.MainCitySummary?.CanUpgrade ?? false ? 1 : 0).Append('|')
                .Append(runtime?.WarehouseSummary?.Capacity ?? 0).Append(':')
                .Append(runtime?.WarehouseSummary?.OccupiedCellCount ?? 0).Append(':')
                .Append(runtime?.WarehouseSummary?.ItemCount ?? 0).Append('|')
                .Append(loadout?.WarehouseItems?.Count ?? 0).Append('|');

            if (runtime != null)
            {
                for (int i = 0; i < runtime.Slots.Count; ++i)
                {
                    HomeClientSlotData slot = runtime.Slots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    builder.Append(slot.SlotId).Append(':')
                        .Append(slot.Unlocked ? 1 : 0).Append(':')
                        .Append(slot.BuildingId).Append(':')
                        .Append(slot.BuildingConfigId).Append('|');
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
            }

            if (loadout?.WarehouseItems != null)
            {
                for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
                {
                    LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                    builder.Append(item.ItemUid).Append(':')
                        .Append(item.ConfigId).Append(':')
                        .Append(item.Count).Append('|');
                }
            }

            if (RequiresTimedRefresh(self.CurrentPageMode, selectedBuilding))
            {
                builder.Append(TimeInfo.Instance.ServerNow() / 1000);
            }

            return builder.ToString();
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

        private static HomeClientSlotData ResolveSelectedHomeSlot(HomePanelComponent self, HomeClientComponent runtime, List<HomeClientBuildingData> buildings)
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
                HomeClientSlotData slot = runtime?.GetSlot(self.SelectedHomeSlotId);
                if (slot != null)
                {
                    self.SelectedHomeBuildingId = slot.BuildingId;
                    return slot;
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

        private static HomeClientBuildingData ResolveSelectedBuilding(HomeClientComponent runtime, int slotId, long buildingId)
        {
            if (runtime == null)
            {
                return null;
            }

            if (buildingId > 0)
            {
                HomeClientBuildingData building = runtime.GetBuilding(buildingId);
                if (building != null)
                {
                    return building;
                }
            }

            HomeClientSlotData slot = runtime.GetSlot(slotId);
            if (slot != null && slot.BuildingId > 0)
            {
                return runtime.GetBuilding(slot.BuildingId);
            }

            return null;
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

        private static HomeClientBuildingData FindFirstBuildingByType(List<HomeClientBuildingData> buildings, int buildingType)
        {
            if (buildings == null)
            {
                return null;
            }

            for (int i = 0; i < buildings.Count; ++i)
            {
                HomeClientBuildingData building = buildings[i];
                if (building != null && GetHomeBuildingType(building.ConfigId) == buildingType)
                {
                    return building;
                }
            }

            return null;
        }

        private static int ResolvePageModeBySelection(HomeClientBuildingData building)
        {
            if (building == null)
            {
                return PAGE_OVERVIEW;
            }

            return GetHomeBuildingType(building.ConfigId) switch
            {
                HomeBuildingType.MainCity => PAGE_MAIN_CITY_TASK,
                HomeBuildingType.Museum => PAGE_MUSEUM,
                HomeBuildingType.RecycleRoom => PAGE_RECYCLE,
                HomeBuildingType.Farm => PAGE_FARM,
                HomeBuildingType.Warehouse => PAGE_WAREHOUSE,
                _ => PAGE_OVERVIEW,
            };
        }

        private static int GetExpectedBuildingType(int pageMode)
        {
            return pageMode switch
            {
                PAGE_MAIN_CITY_TASK => HomeBuildingType.MainCity,
                PAGE_MUSEUM => HomeBuildingType.Museum,
                PAGE_RECYCLE => HomeBuildingType.RecycleRoom,
                PAGE_FARM => HomeBuildingType.Farm,
                PAGE_WAREHOUSE => HomeBuildingType.Warehouse,
                _ => 0,
            };
        }

        private static bool RequiresTimedRefresh(int pageMode, HomeClientBuildingData selectedBuilding)
        {
            int selectedBuildingType = GetHomeBuildingType(selectedBuilding?.ConfigId ?? 0);
            return selectedBuildingType == HomeBuildingType.Farm ||
                selectedBuildingType == HomeBuildingType.RecycleRoom ||
                pageMode == PAGE_FARM ||
                pageMode == PAGE_RECYCLE;
        }

        private static string GetPageTitle(int pageMode, HomeClientBuildingData selectedBuilding)
        {
            return pageMode switch
            {
                PAGE_MAIN_CITY_TASK => "主城任务",
                PAGE_MUSEUM => "大红收藏馆",
                PAGE_RECYCLE => "物品回收间",
                PAGE_FARM => "农场",
                PAGE_WAREHOUSE => "仓库",
                _ => selectedBuilding != null ? GetHomeBuildingName(selectedBuilding.ConfigId) : "家园总览",
            };
        }

        private static string GetPageSubtitle(int pageMode, HomeClientComponent runtime, HomeClientBuildingData selectedBuilding)
        {
            return pageMode switch
            {
                PAGE_MAIN_CITY_TASK => BuildMainCityTaskGroupHint(runtime),
                PAGE_MUSEUM => "展示真实仓库物品，每个展示单位都可以摆上去或取下来。",
                PAGE_RECYCLE => "把仓库物品放进处理位，等待完成后收取更高品质产物。",
                PAGE_FARM => "农场按时间累积金币，收取后会同步刷新顶部财富。",
                PAGE_WAREHOUSE => "查看 Home 使用的真实仓库容量、占用和物品详情。",
                _ => selectedBuilding == null ? "选中空槽位后，会在这里显示当前可建建筑。" : "点击上方详情卡和下方正式页完成家园管理。",
            };
        }

        private static string BuildTopTodoHint(HomeClientComponent runtime)
        {
            List<HomeClientMainCityTaskData> tasks = BuildSortedMainCityTasks(runtime);
            for (int i = 0; i < tasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = tasks[i];
                if (task != null && !task.Completed)
                {
                    return $"待办 {task.Progress}/{task.Target}";
                }
            }

            return tasks.Count > 0 ? "待办 已完成" : "待办 --";
        }

        private static string GetHomePrimaryButtonText(int buildingType)
        {
            return buildingType switch
            {
                HomeBuildingType.MainCity => "查看任务",
                HomeBuildingType.Museum => "管理展示",
                HomeBuildingType.RecycleRoom => "查看回收",
                HomeBuildingType.Farm => "查看农场",
                HomeBuildingType.Warehouse => "查看仓库",
                _ => "查看",
            };
        }

        private static bool CanUpgradeHomeBuilding(HomeClientBuildingData building, int buildingType, HomeClientComponent runtime)
        {
            if (building == null)
            {
                return false;
            }

            if (buildingType == HomeBuildingType.MainCity)
            {
                return HasNextMainCityLevel(building.Level + 1) && (runtime?.MainCitySummary?.CanUpgrade ?? false);
            }

            return HasNextHomeBuildingLevel(building.ConfigId, building.Level + 1) &&
                building.Level < (runtime?.MainCitySummary?.OtherBuildingMaxLevel ?? 0);
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

        private static int GetHomeBuildingType(int configId)
        {
            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            return config?.BuildingType ?? 0;
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

        private static string BuildHomeBuildEntryHint(HomeClientSlotData selectedSlot, HomeClientBuildingData selectedBuilding, bool inHome)
        {
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
                return "请先在左侧列表中选择一个空槽位，再从这里挑选建筑。";
            }

            if (!selectedSlot.Unlocked)
            {
                return $"{GetHomeSlotLockText(selectedSlot)}，解锁后才能建造。";
            }

            if (selectedSlot.SlotType != HomeSlotType.Buildable)
            {
                return "当前选中的是主城保留槽位，不能新建其他建筑。";
            }

            return $"当前选中槽位 {selectedSlot.SlotId}，可建：{GetHomeSlotBuildTypeSummary(selectedSlot)}。";
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

            return inHome
                ? $"这是一个可建空槽位。当前兼容：{GetHomeSlotBuildTypeSummary(slot)}。请从右侧建造入口选择建筑。"
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

            StringBuilder builder = new(32);
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

        private static string BuildMainCityTaskGroupHint(HomeClientComponent runtime)
        {
            List<HomeClientMainCityTaskData> tasks = BuildSortedMainCityTasks(runtime);
            for (int i = 0; i < tasks.Count; ++i)
            {
                HomeClientMainCityTaskData task = tasks[i];
                if (task != null && !task.Completed)
                {
                    return $"当前待完成：{task.Title}（{task.Progress}/{task.Target}）";
                }
            }

            return tasks.Count > 0 ? "当前主城任务已全部完成，可升级主城。" : "当前没有主城任务。";
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

        private static HomeClientMuseumDisplayData FindMuseumDisplayBySlotIndex(List<HomeClientMuseumDisplayData> displays, int slotIndex)
        {
            if (displays == null || slotIndex <= 0)
            {
                return null;
            }

            for (int i = 0; i < displays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = displays[i];
                if (display != null && display.SlotIndex == slotIndex)
                {
                    return display;
                }
            }

            return null;
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

        private static List<HomeClientProductionOrderData> BuildRecycleOrders(HomeClientComponent runtime, long buildingId, int expectedState)
        {
            List<HomeClientProductionOrderData> result = new();
            if (runtime == null || buildingId <= 0)
            {
                return result;
            }

            for (int i = 0; i < runtime.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = runtime.ProductionOrders[i];
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                if (GetHomeRecycleOrderState(order) == expectedState)
                {
                    result.Add(order);
                }
            }

            result.Sort(static (a, b) =>
            {
                int finishCompare = a.FinishTime.CompareTo(b.FinishTime);
                return finishCompare != 0 ? finishCompare : a.OrderId.CompareTo(b.OrderId);
            });
            return result;
        }

        private static int CountReadyRecycleOrders(HomeClientComponent runtime, long buildingId)
        {
            return BuildRecycleOrders(runtime, buildingId, HomeProductionState.Completed).Count;
        }

        private static int CountRunningRecycleOrders(HomeClientComponent runtime, long buildingId)
        {
            return BuildRecycleOrders(runtime, buildingId, HomeProductionState.InProgress).Count;
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

        private static bool HasNextMainCityLevel(int nextLevel)
        {
            return HomeMainCityLevelConfigCategory.Instance.GetOrDefault(nextLevel) != null;
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
                return $"当前展示容量：{capacity}，已摆放：{displayCount}\n点击“管理展示”后，可把仓库中的真实物品摆入收藏馆，也可以随时取下。";
            }

            if (buildingType == HomeBuildingType.RecycleRoom)
            {
                int capacity = GetRecycleSlotCapacity(building);
                int intervalMs = GetRecycleIntervalMs(building);
                int readyCount = CountReadyRecycleOrders(runtime, building.BuildingId);
                string sourceItem = GetHomeItemDisplayName(ResolveFirstWarehouseItemConfig(runtime?.Root()));
                return $"处理位：{capacity}，单次回收时长：{FormatDuration(intervalMs)}\n待收取产物：{readyCount}\n开始回收会消耗仓库中的一个真实物品，当前候选：{sourceItem}";
            }

            if (buildingType == HomeBuildingType.Warehouse)
            {
                int capacity = runtime?.WarehouseSummary?.Capacity ?? 0;
                int occupied = runtime?.WarehouseSummary?.OccupiedCellCount ?? 0;
                int remain = Math.Max(capacity - occupied, 0);
                return $"仓库占用：{occupied}/{capacity}，剩余空间：{remain}\n当前物品堆：{runtime?.WarehouseSummary?.ItemCount ?? 0}\n回收间产物会直接尝试入仓，满仓时会被阻塞。";
            }

            if (buildingType == HomeBuildingType.Farm)
            {
                int intervalMs = GetFarmCollectIntervalMs(building);
                long wealth = PeekHomeFarmWealth(building);
                return $"每轮产出：{levelConfig?.OutputValue ?? 0} 金币，产出间隔：{FormatDuration(intervalMs)}\n当前待收金币：{wealth}\n上次收取：{FormatHomeDateTime(building.LastCollectTime)}";
            }

            return $"上次收取：{FormatHomeDateTime(building.LastCollectTime)}\n最近生产：{FormatHomeDateTime(building.LastProductionTime)}";
        }

        private static string BuildWarehouseDetailText(LoadoutComponent loadout, long selectedItemUid, bool hasWarehouse, int capacity, int occupied)
        {
            if (!hasWarehouse)
            {
                return "尚未建造仓库。建造仓库后，这里才会显示正式仓库详情。";
            }

            if (!TryGetWarehouseItem(loadout, selectedItemUid, out LoadoutWarehouseItemInfo selectedItem))
            {
                return $"当前仓库占用 {occupied}/{capacity}。\n选择下方物品可查看详情。";
            }

            return
                $"{GetHomeItemDisplayName(selectedItem.ConfigId)}\n" +
                $"数量：{selectedItem.Count}\n" +
                $"占格：{selectedItem.GridWidth}x{selectedItem.GridHeight}\n" +
                $"仓库位置锚点：{selectedItem.AnchorSlotIndex}";
        }

        private static int ResolveFirstWarehouseItemConfig(Scene root)
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

        private static List<LoadoutWarehouseItemInfo> BuildSortedWarehouseItems(LoadoutComponent loadout)
        {
            List<LoadoutWarehouseItemInfo> result = new();
            if (loadout?.WarehouseItems == null)
            {
                return result;
            }

            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ConfigId > 0 && item.Count > 0)
                {
                    result.Add(item);
                }
            }

            result.Sort(static (a, b) =>
            {
                int configCompare = a.ConfigId.CompareTo(b.ConfigId);
                return configCompare != 0 ? configCompare : a.ItemUid.CompareTo(b.ItemUid);
            });
            return result;
        }

        private static bool TryGetWarehouseItem(LoadoutComponent loadout, long itemUid, out LoadoutWarehouseItemInfo itemInfo)
        {
            itemInfo = default;
            if (loadout?.WarehouseItems == null || itemUid <= 0)
            {
                return false;
            }

            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ItemUid == itemUid)
                {
                    itemInfo = item;
                    return true;
                }
            }

            return false;
        }

        private static string BuildHomeErrorText(string prefix, int error, string message)
        {
            string detail = error switch
            {
                ErrorCode.ERR_HomePrerequisiteNotMet => string.IsNullOrWhiteSpace(message) ? "前置条件未满足，请先完成主城任务或补足资源。" : message,
                ErrorCode.ERR_HomeSlotOccupied => "槽位已被占用。",
                ErrorCode.ERR_HomeBuildingMaxLevel => "该建筑已达当前最高等级。",
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

        private static string GetHomeBuildingName(int configId)
        {
            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            return config?.Name ?? $"建筑 {configId}";
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

        private static void ResolveDisplayInfo(int configId, out string name, out string icon, out int sortCategory)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                name = itemConfig.Name;
                icon = itemConfig.Icon;
                sortCategory = itemConfig.LoadoutShopCategory;
                return;
            }

            EquipmentConfig equipmentConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            if (equipmentConfig != null)
            {
                name = $"装备({configId})";
                icon = string.Empty;
                sortCategory = equipmentConfig.EquipSlot;
                return;
            }

            name = $"Item({configId})";
            icon = string.Empty;
            sortCategory = int.MaxValue;
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

        private static string FormatHomeBuildingState(int state)
        {
            return state switch
            {
                HomeBuildingState.Idle => "空闲",
                HomeBuildingState.Collecting => "可收取",
                _ => $"未知({state})",
            };
        }

        private static bool IsInHomeScene(Scene root)
        {
            Scene currentScene = root?.CurrentScene();
            return currentScene != null && !currentScene.IsDisposed && currentScene.Name.GetSceneConfigName() == "Home";
        }

        private static void ShowHomeTips(Scene root, string text)
        {
            if (root == null || root.IsDisposed || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            TipsHelper.OpenSync<TipsTextViewComponent>(root, text);
        }

        private static Button CloneButtonTemplate(Button template, string name)
        {
            Button clone = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
            clone.name = name;
            clone.gameObject.SetActive(true);
            return clone;
        }

        private static RectTransform CloneRectTemplate(RectTransform template, string name)
        {
            RectTransform clone = UnityEngine.Object.Instantiate(template, template.parent, false);
            clone.name = name;
            clone.gameObject.SetActive(true);
            return clone;
        }

        private static void ClearTemplateChildren(RectTransform parent, Transform templateTransform)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; --i)
            {
                Transform child = parent.GetChild(i);
                if (templateTransform != null && child == templateTransform)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static void SetCardButtonContent(Button button, string title, string subtitle)
        {
            if (button == null)
            {
                return;
            }

            SetTemplateText(button.transform, "TitleText", title);
            SetTemplateText(button.transform, "SubTitleText", subtitle);
        }

        private static void SetTemplateText(Component root, string childName, string text)
        {
            if (root == null)
            {
                return;
            }

            Transform child = root.transform.Find(childName);
            TMP_Text tmp = child != null ? child.GetComponent<TMP_Text>() : null;
            if (tmp != null)
            {
                tmp.text = text ?? string.Empty;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static void SetVisible(Component component, bool visible)
        {
            if (component != null)
            {
                component.gameObject.SetActive(visible);
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
        }

        private static void SetNavSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic is Graphic graphic)
            {
                graphic.color = selected ? new Color32(121, 147, 193, 255) : new Color32(72, 86, 110, 255);
            }
        }

        private static void SetCardButtonSelected(Button button, bool selected, Color32 baseColor)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected
                    ? Color32.Lerp(baseColor, new Color32(235, 220, 152, 255), 120)
                    : baseColor;
            }
        }

        private static Color32 GetBuildingItemColor(HomeClientSlotData slot, HomeClientBuildingData building)
        {
            if (building != null)
            {
                return new Color32(61, 72, 94, 255);
            }

            if (slot != null && !slot.Unlocked)
            {
                return new Color32(53, 56, 68, 255);
            }

            return new Color32(56, 78, 62, 255);
        }

        private static Color32 GetBuildOptionColor(int configId)
        {
            return GetHomeBuildingType(configId) switch
            {
                HomeBuildingType.Museum => new Color32(105, 74, 54, 255),
                HomeBuildingType.RecycleRoom => new Color32(63, 89, 116, 255),
                HomeBuildingType.Farm => new Color32(67, 106, 67, 255),
                HomeBuildingType.Warehouse => new Color32(109, 95, 56, 255),
                _ => new Color32(74, 87, 108, 255),
            };
        }
    }
}
