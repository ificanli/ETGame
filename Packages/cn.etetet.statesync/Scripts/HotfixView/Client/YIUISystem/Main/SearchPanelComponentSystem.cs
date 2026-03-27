using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using YIUIFramework;
using ET;

namespace ET.Client
{
    [FriendOf(typeof(SearchPanelComponent))]
    public static partial class SearchPanelComponentSystem
    {
        private const int FallbackCols = 8;
        private const int ContainerFixedCols = 4;
        private const int ContainerFixedRows = 3;
        private const int MinRows = 4;

        [EntitySystem]
        private static void YIUIInitialize(this SearchPanelComponent self)
        {
            self.LastContainerSnapshot = null;
            self.OpenMode = SearchPanelOpenMode.Unknown;
            self.CorpseSubType = SearchPanelCorpseSubType.Unknown;
            self.CurrentPointId = null;
            self.TitleText = null;
            self.SubTitleText = null;
            InitLayout(self);
            CacheGridRoots(self);
            CacheSecureWidgets(self);
            CacheModeWidgets(self);
            self.QuickChooseMinQuality = ReadQuickChooseMinQuality(self);
            SetTemplateActive(self.u_ComContainerItemTemplate, false);
            SetTemplateActive(self.u_ComBagItemTemplate, false);
            SetTemplateActive(self.SecureItemTemplate, false);
            
            // 初始化搜索动效配置
            self.CurrentSearchingPointId = null;
        }

        [EntitySystem]
        private static void Destroy(this SearchPanelComponent self)
        {
            ReleaseViews(self.ContainerItemViews);
            ReleaseViews(self.BagItemViews);
            ReleaseViews(self.SecureItemViews);
            ReleaseViews(self.ContainerGridCellViews);
            ReleaseViews(self.BagGridCellViews);
            ReleaseViews(self.SecureGridCellViews);
            self.ContainerSolver = null;
            self.BagSolver = null;
            self.SecureSolver = null;
            self.LastContainerSnapshot = null;
            self.ContainerGridRoot = null;
            self.BagGridRoot = null;
            self.SecureBoardRoot = null;
            self.SecureItemsLayer = null;
            self.SecureItemTemplate = null;
            self.SecureGridRoot = null;
            self.QuickChooseDropdown = null;
            self.QuickChooseButtonRoot = null;
            self.QuickChooseButtonLabel = null;
            self.QuickChooseButtonImage = null;
            self.ModeTitleText = null;
            self.ModeSubTitleText = null;
            self.ContainerTitleText = null;
            self.BagTitleText = null;
            self.ModeAccentImage = null;
            self.IsDragging = false;
            self.DraggingView = null;
            
            // 清理搜索动效相关数据
            ClearSearchEffects(self);
            self.SlotSearchStartTimes.Clear();
            self.SlotSearchDurations.Clear();
            self.SearchedSlots.Clear();
            self.CurrentSearchingPointId = null;
            self.OpenMode = SearchPanelOpenMode.Unknown;
            self.CorpseSubType = SearchPanelCorpseSubType.Unknown;
            self.CurrentPointId = null;
            self.TitleText = null;
            self.SubTitleText = null;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SearchPanelComponent self)
        {
            self.LastContainerSnapshot = null;
            self.QuickChooseMinQuality = ReadQuickChooseMinQuality(self);
            TryRefreshView(self, true);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void LateUpdate(this SearchPanelComponent self)
        {
            if (self.IsDragging)
            {
                return;
            }

            TryRefreshView(self, false);
            
            // 更新搜索动效状态
            if (SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode))
            {
                UpdateSearchEffects(self);
            }
        }

        #region YIUIEvent开始

        [YIUIInvoke(SearchPanelComponent.OnEventOpenSearchingViewInvoke)]
        private static async ETTask OnEventOpenSearchingViewInvoke(this SearchPanelComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                Log.Info("[ECAClient][SearchPanel] click search button -> TryInteractFocus");
                ECAInteractHelper.TryInteractFocus(root).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        private static void TryRefreshView(this SearchPanelComponent self, bool force)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }

            TrySyncOpenContext(self, root);
            SyncSearchStateByMode(self);
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            ApplyModeStyle(self, runtime);
            ItemComponent itemComponent = root.GetComponent<ItemComponent>();
            string snapshot = BuildSnapshot(self, runtime, itemComponent);
            if (!force && self.LastContainerSnapshot == snapshot)
            {
                return;
            }

            self.LastContainerSnapshot = snapshot;
            if (SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode))
            {
                RenderContainer(self, runtime);
            }

            RefreshLoadoutSlots(self, root.GetComponent<LoadoutComponent>());
            RenderBag(self, itemComponent);
            RenderSecure(self, root.GetComponent<LoadoutComponent>());

            Log.Info(
                $"[ECAClient][SearchPanel] refresh mode={self.OpenMode}, point={self.CurrentPointId ?? "null"}, container={runtime?.ContainerItems.Count ?? 0}, bag={itemComponent?.GetUsedSlotCount() ?? 0}");
        }

        private static void TrySyncOpenContext(SearchPanelComponent self, Scene root)
        {
            SearchPanelOpenContextComponent openContext = root.GetComponent<SearchPanelOpenContextComponent>();
            if (openContext != null && openContext.OpenMode != SearchPanelOpenMode.Unknown)
            {
                ApplyOpenContext(
                    self,
                    openContext.OpenMode,
                    openContext.CorpseSubType,
                    openContext.CurrentPointId,
                    openContext.TitleText,
                    openContext.SubTitleText);
                openContext.ClearOpenContext();
                return;
            }

            if (self.OpenMode != SearchPanelOpenMode.Unknown)
            {
                return;
            }

            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime != null && !string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
            {
                SearchPanelModeResolveResult result = SearchPanelModeHelper.ResolveContainerMode(runtime.OpenContainerPointId);
                ApplyOpenContext(self, result.OpenMode, result.CorpseSubType, runtime.OpenContainerPointId, null, null);
                return;
            }

            ApplyOpenContext(self, SearchPanelOpenMode.BackpackInspect, SearchPanelCorpseSubType.Unknown, null, null, null);
        }

        private static void ApplyOpenContext(
            SearchPanelComponent self,
            SearchPanelOpenMode openMode,
            SearchPanelCorpseSubType corpseSubType,
            string pointId,
            string titleText,
            string subTitleText)
        {
            self.OpenMode = openMode;
            self.CorpseSubType = corpseSubType;
            self.CurrentPointId = pointId;
            self.TitleText = titleText;
            self.SubTitleText = subTitleText;
        }

        private static void SyncSearchStateByMode(SearchPanelComponent self)
        {
            string pointId = SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode) ? self.CurrentPointId : null;
            if (string.Equals(pointId, self.CurrentSearchingPointId, StringComparison.Ordinal))
            {
                return;
            }

            ClearSearchEffects(self);
            self.SlotSearchStartTimes.Clear();
            self.SlotSearchDurations.Clear();
            self.SearchedSlots.Clear();
            self.CurrentSearchingPointId = pointId;
        }

        private static void ApplyModeStyle(SearchPanelComponent self, ECAInteractClientComponent runtime)
        {
            bool showContainer = SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode);
            bool showQuickActions = SearchPanelModeHelper.ShouldShowQuickActionControls(self.OpenMode);
            bool useBagOnlyLayout = SearchPanelModeHelper.ShouldUseBagOnlyLayout(self.OpenMode);
            SearchPanelDisplayInfo displayInfo = SearchPanelDisplayHelper.Resolve(
                self.OpenMode,
                self.CorpseSubType,
                runtime?.ContainerOutputMode ?? ContainerOutputMode.ContainerPanel,
                self.TitleText,
                self.SubTitleText);

            SetGameObjectActive(self.u_ComContainerBoardRoot, showContainer);
            SetGameObjectActive(self.u_ComContainerItemsLayer, showContainer);
            SetGameObjectActive(self.ContainerGridRoot, showContainer);
            SetGameObjectActive(self.u_ComBagBoardRoot, true);
            SetGameObjectActive(self.u_ComBagItemsLayer, true);
            SetGameObjectActive(self.BagGridRoot, true);
            SetGameObjectActive(self.u_ComSecureBagRootRectTransform, true);
            SetGameObjectActive(self.SecureBoardRoot, true);
            SetGameObjectActive(self.SecureItemsLayer, true);
            SetGameObjectActive(self.SecureGridRoot, true);

            if (useBagOnlyLayout)
            {
                ApplyBagOnlyLayout(self);
            }
            else
            {
                RestoreBagBoardLayout(self);
            }

            if (self.QuickChooseDropdown != null)
            {
                self.QuickChooseDropdown.gameObject.SetActive(showQuickActions);
            }

            SetGameObjectActive(self.QuickChooseButtonRoot, showQuickActions);
            SetOwnerChildActive(self, "Arrow", showQuickActions);
            SetOwnerChildActive(self, "Label", showQuickActions);
            ApplyDisplayInfo(self, displayInfo);
        }

        private static void InitLayout(SearchPanelComponent self)
        {
            float unified = LobbyPanelComponent.UnifiedCellSize;
            Vector2 defaultCell = new Vector2(unified, unified);
            Vector2 containerCell = GetRectSize(self.u_ComContainerItemTemplate, defaultCell);
            Vector2 bagCell = GetRectSize(self.u_ComBagItemTemplate, defaultCell);
            self.CellSize = bagCell.x > 0 && bagCell.y > 0 ? bagCell : containerCell;
            self.CellSpacing = new Vector2(8, 8);
            self.CellPadding = new Vector2(8, 8);

            self.ContainerCols = ContainerFixedCols;
            self.BagCols = CalcCols(self.u_ComBagBoardRoot, self.CellSize, self.CellSpacing, FallbackCols);
            self.ContainerRows = ContainerFixedRows;
            self.BagRows = MinRows;
            self.SecureCols = Math.Max(1, ExtractionInventoryConfig.GetSafeSlotCount());
            self.SecureRows = 1;

            EnsureContainerSolver(self, self.ContainerRows);
            EnsureBagSolver(self, self.BagCols, self.BagRows);
            EnsureSecureSolver(self, self.SecureCols, self.SecureRows);
        }

        private static void CacheGridRoots(SearchPanelComponent self)
        {
            self.ContainerGridRoot = self.u_ComContainerBoardRoot?.Find("GridRoot") as RectTransform;
            self.BagGridRoot = self.u_ComBagBoardRoot?.Find("GridRoot") as RectTransform;
        }

        private static void CacheSecureWidgets(SearchPanelComponent self)
        {
            RectTransform secureRoot = self.u_ComSecureBagRootRectTransform;
            self.SecureBoardRoot = FindDirectChildRectTransform(secureRoot, "SecureBoardRoot");
            self.SecureItemsLayer = FindDirectChildRectTransform(secureRoot, "SecureItemsLayer");
            self.SecureItemTemplate = FindDirectChildRectTransform(secureRoot, "SecureItemTemplate");
            self.SecureGridRoot = FindDirectChildRectTransform(secureRoot, "SecureGridRoot");
        }

        private static void CacheModeWidgets(SearchPanelComponent self)
        {
            self.QuickChooseDropdown = self.UIBase?.OwnerGameObject?.GetComponentInChildren<Dropdown>(true);
            self.QuickChooseButtonRoot = FindOwnerChild<RectTransform>(self, "Button");
            self.QuickChooseButtonLabel = self.QuickChooseButtonRoot?.Find("Text (TMP)")?.GetComponent<TMP_Text>();
            self.QuickChooseButtonImage = self.QuickChooseButtonRoot?.GetComponent<Image>();
            self.ModeTitleText = FindOwnerChildRecursive<TMP_Text>(self, "ModeTitleText");
            self.ModeSubTitleText = FindOwnerChildRecursive<TMP_Text>(self, "ModeSubTitleText");
            self.ContainerTitleText = FindOwnerChildRecursive<TMP_Text>(self, "ContainerTitleText");
            self.BagTitleText = FindOwnerChildRecursive<TMP_Text>(self, "BagTitleText");
            self.ModeAccentImage = FindOwnerChildRecursive<Image>(self, "ModeAccentImage");

            if (self.u_ComBagBoardRoot != null)
            {
                self.BagBoardAnchorMin = self.u_ComBagBoardRoot.anchorMin;
                self.BagBoardAnchorMax = self.u_ComBagBoardRoot.anchorMax;
                self.BagBoardAnchoredPosition = self.u_ComBagBoardRoot.anchoredPosition;
                self.BagBoardSizeDelta = self.u_ComBagBoardRoot.sizeDelta;
            }

            if (self.u_ComContainerBoardRoot != null)
            {
                self.ContainerBoardAnchorMin = self.u_ComContainerBoardRoot.anchorMin;
            }
        }

        private static void RenderContainer(SearchPanelComponent self, ECAInteractClientComponent runtime)
        {
            int maxSlot = -1;
            int count = runtime?.ContainerItems.Count ?? 0;
            for (int i = 0; i < count; ++i)
            {
                int slot = runtime.ContainerItems[i].SlotIndex;
                if (slot > maxSlot)
                {
                    maxSlot = slot;
                }
            }

            int needRows = ContainerFixedRows;
            EnsureContainerSolver(self, needRows);
            self.ContainerSolver.Clear();

            HashSet<long> alive = new();
            long nowMs = TimeInfo.Instance.ClientNow();
            
            for (int i = 0; i < count; ++i)
            {
                ContainerClientItemData item = runtime.ContainerItems[i];
                int slot = Math.Max(item.SlotIndex, 0);
                long viewId = slot + 1L;
                alive.Add(viewId);

                GridItemFootprint footprint = new GridItemFootprint
                {
                    ItemId = viewId,
                    X = slot % self.ContainerCols,
                    Y = slot / self.ContainerCols,
                    Width = GetItemWidth(item.ConfigId),
                    Height = GetItemHeight(item.ConfigId)
                };

                if (!self.ContainerSolver.TryPlaceOrMove(footprint))
                {
                    continue;
                }

                RectTransform view = GetOrCreateView(self.ContainerItemViews, self.u_ComContainerItemTemplate, self.u_ComContainerItemsLayer, viewId);
                if (view == null)
                {
                    continue;
                }

                ApplyFootprint(view, footprint, self.CellSize, self.CellSpacing, self.CellPadding);
                BindItemView(view, item.ConfigId, item.Count, slot);
                BindItemInteract(self, view, viewId, false, item.ConfigId, true);
                
                // 处理搜索动效
                bool isSearched = self.SearchedSlots.Contains(slot);
                if (!isSearched)
                {
                    // 如果还没有开始搜索，记录搜索开始时间和持续时间
                    if (!self.SlotSearchStartTimes.ContainsKey(slot))
                    {
                        self.SlotSearchStartTimes[slot] = nowMs;
                        
                        // 根据物品品质获取搜索持续时间
                        ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(item.ConfigId);
                        int quality = itemConfig?.Quality ?? 1;
                        long durationMs = ExtractionInventoryConfig.GetItemSearchDurationMsByQuality(quality);
                        self.SlotSearchDurations[slot] = durationMs;
                    }
                    
                    // 创建或更新搜索动效
                    EnsureSearchEffect(self, view, slot, true);
                }
                else
                {
                    // 已搜索完成，隐藏动效
                    EnsureSearchEffect(self, view, slot, false);
                }
            }

            RemoveDeadViews(self.ContainerItemViews, alive);
            ResizeBoard(self.u_ComContainerBoardRoot, self.u_ComContainerItemsLayer, self.ContainerCols, self.ContainerRows, self.CellSize, self.CellSpacing, self.CellPadding);
            RenderGrid(self.ContainerGridRoot, self.ContainerGridCellViews, self.ContainerCols, self.ContainerRows, self.CellSize, self.CellSpacing, self.CellPadding, false);
        }

        private static void RenderBag(SearchPanelComponent self, ItemComponent itemComponent)
        {
            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            int capacity = itemComponent?.Capacity ?? 0;
            int visibleCapacity = GetVisibleBagSlotCount(capacity);
            ResolveBagLayout(self, itemComponent, loadout, visibleCapacity, out int cols, out int rows);
            EnsureBagSolver(self, cols, rows);
            self.BagSolver.Clear();

            HashSet<long> alive = new();
            if (itemComponent != null)
            {
                for (int actualSlot = 0; actualSlot < capacity; ++actualSlot)
                {
                    if (ExtractionInventoryConfig.IsSafeSlot(actualSlot))
                    {
                        continue;
                    }

                    int slot = ConvertActualBagSlotToDisplaySlot(actualSlot);
                    if (slot < 0)
                    {
                        continue;
                    }

                    Item item = itemComponent.GetItemBySlot(actualSlot);
                    if (item == null || item.Count <= 0)
                    {
                        continue;
                    }

                    alive.Add(item.Id);
                    GridItemFootprint footprint = new GridItemFootprint
                    {
                        ItemId = item.Id,
                        X = slot % cols,
                        Y = slot / cols,
                        Width = GetItemWidth(item.ConfigId),
                        Height = GetItemHeight(item.ConfigId)
                    };

                    if (!self.BagSolver.TryPlaceOrMove(footprint))
                    {
                        continue;
                    }

                    RectTransform view = GetOrCreateView(self.BagItemViews, self.u_ComBagItemTemplate, self.u_ComBagItemsLayer, item.Id);
                    if (view == null)
                    {
                        continue;
                    }

                    ApplyFootprint(view, footprint, self.CellSize, self.CellSpacing, self.CellPadding);
                    BindItemView(view, item.ConfigId, item.Count, slot);
                    BindItemInteract(self, view, item.Id, true, item.ConfigId, true);
                }
            }

            RemoveDeadViews(self.BagItemViews, alive);
            ResizeBoard(self.u_ComBagBoardRoot, self.u_ComBagItemsLayer, cols, rows, self.CellSize, self.CellSpacing, self.CellPadding);
            RenderGrid(self.BagGridRoot, self.BagGridCellViews, cols, rows, self.CellSize, self.CellSpacing, self.CellPadding, false);
        }

        private static void RenderSecure(SearchPanelComponent self, LoadoutComponent loadout)
        {
            if (self.SecureBoardRoot == null || self.SecureItemsLayer == null || self.SecureGridRoot == null)
            {
                ReleaseViews(self.SecureItemViews);
                ReleaseViews(self.SecureGridCellViews);
                return;
            }

            int cols = Math.Max(1, loadout?.SecureWidth ?? ExtractionInventoryConfig.GetSafeSlotCount());
            int rows = Math.Max(1, loadout?.SecureHeight ?? 1);
            EnsureSecureSolver(self, cols, rows);
            self.SecureSolver.Clear();

            HashSet<long> alive = new();
            List<LoadoutGridItemInfo> items = loadout?.CarriedSecureItems;
            if (items != null && self.SecureItemTemplate != null)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    LoadoutGridItemInfo item = items[i];
                    long viewId = item.AnchorSlotIndex + 1L;
                    alive.Add(viewId);

                    GridItemFootprint footprint = new GridItemFootprint
                    {
                        ItemId = viewId,
                        X = item.AnchorSlotIndex % cols,
                        Y = item.AnchorSlotIndex / cols,
                        Width = Math.Max(1, item.GridWidth),
                        Height = Math.Max(1, item.GridHeight)
                    };

                    if (!self.SecureSolver.TryPlaceOrMove(footprint))
                    {
                        continue;
                    }

                    RectTransform view = GetOrCreateView(self.SecureItemViews, self.SecureItemTemplate, self.SecureItemsLayer, viewId);
                    if (view == null)
                    {
                        continue;
                    }

                    ApplyFootprint(view, footprint, self.CellSize, self.CellSpacing, self.CellPadding);
                    BindItemView(view, item.ConfigId, item.Count, item.AnchorSlotIndex);
                    BindItemInteract(self, view, viewId, false, item.ConfigId, false);
                }
            }

            RemoveDeadViews(self.SecureItemViews, alive);
            ResizeBoard(self.SecureBoardRoot, self.SecureItemsLayer, cols, rows, self.CellSize, self.CellSpacing, self.CellPadding);
            RenderGrid(self.SecureGridRoot, self.SecureGridCellViews, cols, rows, self.CellSize, self.CellSpacing, self.CellPadding, false);
        }

        private static void RefreshLoadoutSlots(SearchPanelComponent self, LoadoutComponent loadout)
        {
            RefreshSlotView(self.UIEquipSlotItemWeapon, loadout?.MainWeaponConfigId ?? 0, "武器1", true);
            RefreshSlotView(self.UIEquipSlotItemWeapon2, loadout?.SubWeaponConfigId ?? 0, "武器2", true);
            RefreshSlotView(self.UIEquipSlotItemArmor, loadout?.ArmorConfigId ?? 0, "防具");
            RefreshSlotView(self.UIEquipSlotItemBag, loadout?.BackpackConfigId ?? 0, "背包");
        }

        private static void RefreshSlotView(
            EquipSlotItemComponent slotItem,
            int configId,
            string slotName,
            bool hideTextWhenEquipped = false)
        {
            if (slotItem == null)
            {
                return;
            }

            if (configId <= 0)
            {
                slotItem.u_DataSlotName.SetValue(slotName);
                slotItem.u_DataEquipName.SetValue(string.Empty);
                slotItem.u_DataIsEmpty.SetValue(true);
                slotItem.SetItemIcon(string.Empty);
                return;
            }

            ResolveDisplayInfo(configId, out string name, out string icon, out _);
            slotItem.u_DataSlotName.SetValue(hideTextWhenEquipped ? string.Empty : slotName);
            slotItem.u_DataEquipName.SetValue(hideTextWhenEquipped ? string.Empty : name);
            slotItem.u_DataIsEmpty.SetValue(false);
            slotItem.SetItemIcon(icon);
        }

        private static RectTransform GetOrCreateView(
            Dictionary<long, RectTransform> map,
            RectTransform template,
            RectTransform layer,
            long itemId)
        {
            if (map.TryGetValue(itemId, out RectTransform oldView) && oldView != null)
            {
                oldView.gameObject.SetActive(true);
                return oldView;
            }

            if (template == null || layer == null)
            {
                return null;
            }

            RectTransform view = UnityEngine.Object.Instantiate(template, layer);
            view.gameObject.SetActive(true);
            map[itemId] = view;
            return view;
        }

        private static void BindItemView(RectTransform view, int configId, int count, int slotIndex)
        {
            ItemConfig config = LegacyItemConfigCompatHelper.GetDisplayItemConfig(configId);
            ResolveDisplayInfo(configId, out string displayName, out string iconName, out _);
            view.name = $"Item_{slotIndex}_{configId}";
            ItemQualityBgViewHelper.UpdateQualityBg(view, config?.Quality ?? 1);

            SearchItemDragProxy proxy = view.GetComponent<SearchItemDragProxy>() ?? view.gameObject.AddComponent<SearchItemDragProxy>();
            proxy.ConfigId = configId;
            proxy.IconImage = FindBestIconImage(view);
            proxy.TmpTexts ??= view.GetComponentsInChildren<TMP_Text>(true);
            proxy.Texts ??= view.GetComponentsInChildren<Text>(true);

            ApplySearchGridItemTexts(proxy, !string.IsNullOrWhiteSpace(displayName) ? displayName : config?.Name ?? $"Item({configId})", count);
            UpdateSearchGridItemIcon(proxy, iconName).Coroutine();
        }

        private static void ApplySearchGridItemTexts(SearchItemDragProxy proxy, string text, int count)
        {
            _ = count;

            if (proxy?.TmpTexts != null && proxy.TmpTexts.Length > 0)
            {
                if (proxy.TmpTexts.Length == 1)
                {
                    proxy.TmpTexts[0].text = text;
                }
                else
                {
                    proxy.TmpTexts[0].text = text;
                    proxy.TmpTexts[1].text = string.Empty;
                }
            }

            if (proxy?.Texts != null && proxy.Texts.Length > 0)
            {
                if (proxy.Texts.Length == 1)
                {
                    proxy.Texts[0].text = text;
                }
                else
                {
                    proxy.Texts[0].text = text;
                    proxy.Texts[1].text = string.Empty;
                }
            }
        }

        private static async ETTask UpdateSearchGridItemIcon(SearchItemDragProxy proxy, string iconName)
        {
            if (proxy == null || proxy.IconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconName))
            {
                ReleaseSearchGridItemSprite(proxy);
                proxy.IconImage.sprite = null;
                proxy.IconImage.enabled = false;
                proxy.LoadedIconName = string.Empty;
                return;
            }

            if (proxy.LoadedSprite != null && proxy.LoadedIconName == iconName)
            {
                proxy.IconImage.sprite = proxy.LoadedSprite;
                proxy.IconImage.enabled = true;
                proxy.IconImage.preserveAspect = true;
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = iconName });

            if (proxy == null || proxy.IconImage == null)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            if (sprite == null)
            {
                ReleaseSearchGridItemSprite(proxy);
                proxy.IconImage.sprite = null;
                proxy.IconImage.enabled = false;
                proxy.LoadedIconName = string.Empty;
                return;
            }

            ReleaseSearchGridItemSprite(proxy);
            proxy.LoadedSprite = sprite;
            proxy.LoadedIconName = iconName;
            proxy.IconImage.sprite = sprite;
            proxy.IconImage.enabled = true;
            proxy.IconImage.preserveAspect = true;
        }

        private static void ReleaseSearchGridItemSprite(SearchItemDragProxy proxy)
        {
            if (proxy?.LoadedSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = proxy.LoadedSprite });

            if (proxy.IconImage != null && proxy.IconImage.sprite == proxy.LoadedSprite)
            {
                proxy.IconImage.sprite = null;
            }

            proxy.LoadedSprite = null;
        }

        private static Image FindBestIconImage(RectTransform view)
        {
            Image image = FindImageByExactName(view, "ItemImage");
            if (image != null)
            {
                return image;
            }

            image = FindImageByExactName(view, "ItemIcon");
            if (image != null)
            {
                return image;
            }

            image = FindImageByExactName(view, "Icon");
            if (image != null)
            {
                return image;
            }

            Image[] images = view.GetComponentsInChildren<Image>(true);
            Image fallback = null;
            for (int i = 0; i < images.Length; ++i)
            {
                Image current = images[i];
                if (current == null)
                {
                    continue;
                }

                if (current.gameObject == view.gameObject)
                {
                    continue;
                }

                if (string.Equals(current.name, "QualityBg", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(current.name, "Bg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = current;
                }

                if (current.name.IndexOf("Item", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    current.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return current;
                }
            }

            return fallback;
        }

        private static Image FindImageByExactName(RectTransform view, string imageName)
        {
            if (view == null || string.IsNullOrWhiteSpace(imageName))
            {
                return null;
            }

            Image[] images = view.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; ++i)
            {
                Image image = images[i];
                if (image != null && string.Equals(image.name, imageName, StringComparison.Ordinal))
                {
                    return image;
                }
            }

            return null;
        }

        private static void BindItemInteract(
            SearchPanelComponent self,
            RectTransform view,
            long itemId,
            bool isBag,
            int configId,
            bool enableDrag)
        {
            if (view == null)
            {
                return;
            }

            SearchItemDragProxy proxy = view.GetComponent<SearchItemDragProxy>();
            if (proxy == null)
            {
                proxy = view.gameObject.AddComponent<SearchItemDragProxy>();
            }
            proxy.PanelRef = self;
            proxy.ItemId = itemId;
            proxy.IsBag = isBag;
            proxy.ConfigId = configId;

            EventTrigger trigger = view.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = view.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            if (enableDrag)
            {
                AddTrigger(trigger, EventTriggerType.BeginDrag, OnBeginDragEvent);
                AddTrigger(trigger, EventTriggerType.Drag, OnDragEvent);
                AddTrigger(trigger, EventTriggerType.EndDrag, OnEndDragEvent);
            }

            AddTrigger(trigger, EventTriggerType.PointerClick, OnClickEvent);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType eventType, UnityAction<BaseEventData> handler)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType
            };
            entry.callback.AddListener(handler);
            trigger.triggers.Add(entry);
        }

        private static void OnBeginDragEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out _, out PointerEventData eventData))
            {
                return;
            }

            OnItemBeginDrag(self, view, itemId, isBag, eventData);
        }

        private static void OnDragEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out _, out PointerEventData eventData))
            {
                return;
            }

            OnItemDrag(self, view, itemId, isBag, eventData);
        }

        private static void OnEndDragEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out _, out PointerEventData eventData))
            {
                return;
            }

            OnItemEndDrag(self, view, itemId, isBag, eventData);
        }

        private static void OnClickEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out int configId, out PointerEventData eventData))
            {
                return;
            }

            OnItemClick(self, view, itemId, isBag, configId, eventData).Coroutine();
        }

        private static bool TryGetDragContext(
            BaseEventData data,
            out SearchPanelComponent self,
            out RectTransform view,
            out long itemId,
            out bool isBag,
            out int configId,
            out PointerEventData eventData)
        {
            self = null;
            view = null;
            itemId = 0;
            isBag = false;
            configId = 0;
            eventData = data as PointerEventData;
            if (eventData == null)
            {
                return false;
            }

            GameObject go = eventData.pointerDrag != null ? eventData.pointerDrag : eventData.pointerPress;
            if (go == null)
            {
                return false;
            }

            SearchItemDragProxy proxy = go.GetComponent<SearchItemDragProxy>() ?? go.GetComponentInParent<SearchItemDragProxy>();
            if (proxy == null)
            {
                return false;
            }

            self = proxy.Panel;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            view = go.GetComponent<RectTransform>();
            if (view == null)
            {
                return false;
            }

            itemId = proxy.ItemId;
            isBag = proxy.IsBag;
            configId = proxy.ConfigId;
            return true;
        }

        private static void OnItemBeginDrag(
            SearchPanelComponent self,
            RectTransform view,
            long itemId,
            bool isBag,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || eventData == null)
            {
                return;
            }

            GridPlacementSolver solver = isBag ? self.BagSolver : self.ContainerSolver;
            RectTransform layer = isBag ? self.u_ComBagItemsLayer : self.u_ComContainerItemsLayer;
            if (solver == null || layer == null || !solver.TryGetItem(itemId, out _))
            {
                return;
            }

            self.IsDragging = true;
            self.DraggingIsBag = isBag;
            self.DraggingItemId = itemId;
            self.DraggingView = view;
            view.SetAsLastSibling();

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(layer, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                self.DragWorldOffset = view.position - worldPoint;
            }
            else
            {
                self.DragWorldOffset = Vector3.zero;
            }

            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
        }

        private static void OnItemDrag(
            SearchPanelComponent self,
            RectTransform view,
            long itemId,
            bool isBag,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || !self.IsDragging || self.DraggingView != view || self.DraggingItemId != itemId || self.DraggingIsBag != isBag || eventData == null)
            {
                return;
            }

            RectTransform layer = isBag ? self.u_ComBagItemsLayer : self.u_ComContainerItemsLayer;
            if (layer == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(layer, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                view.position = worldPoint + self.DragWorldOffset;
            }
        }

        private static void OnItemEndDrag(
            SearchPanelComponent self,
            RectTransform view,
            long itemId,
            bool isBag,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null)
            {
                return;
            }

            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            if (!self.IsDragging || self.DraggingView != view || self.DraggingItemId != itemId || self.DraggingIsBag != isBag)
            {
                return;
            }

            self.IsDragging = false;
            self.DraggingView = null;

            GridPlacementSolver solver = isBag ? self.BagSolver : self.ContainerSolver;
            if (solver == null || !solver.TryGetItem(itemId, out GridItemFootprint oldFootprint))
            {
                return;
            }

            if (!TryGetSourceSlot(self, isBag, itemId, out int sourceSlot))
            {
                ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                return;
            }

            if (!TryGetDropArea(self, eventData, out bool targetIsBag, out int targetSlot))
            {
                ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                return;
            }

            if (targetIsBag == isBag)
            {
                int targetX = targetSlot % solver.Cols;
                int targetY = targetSlot / solver.Cols;
                int maxRadius = Math.Max(solver.Cols, solver.Rows);
                GridDropResult dropResult = solver.ResolveDropNearest(itemId, targetX, targetY, maxRadius, allowSwap: true);
                if (dropResult.ResultType == GridDropResultType.Failed)
                {
                    ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                    return;
                }

                targetSlot = dropResult.PlaceY * solver.Cols + dropResult.PlaceX;
                if (targetSlot == sourceSlot)
                {
                    ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                    return;
                }

                if (isBag)
                {
                    if (!CanMoveBagToSlot(self, targetSlot))
                    {
                        ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                        return;
                    }

                    MoveBagItem(self.Root(), itemId, targetSlot).Coroutine();
                }
                else
                {
                    MoveContainerItem(self.Root(), false, sourceSlot, 0, false, targetSlot).Coroutine();
                }

                ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                Log.Info(
                    $"[SearchDrag] request move {(isBag ? "bag" : "container")} item={itemId}, fromSlot={sourceSlot}, toSlot={targetSlot}, targetIsBag={targetIsBag}, result={dropResult.ResultType}");
                return;
            }

            if (targetIsBag)
            {
                if (!CanMoveBagToSlot(self, targetSlot))
                {
                    ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                    return;
                }
            }

            MoveContainerItem(self.Root(), isBag, sourceSlot, isBag ? itemId : 0, targetIsBag, targetSlot).Coroutine();
            ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
            Log.Info(
                $"[SearchDrag] request cross move {(isBag ? "bag" : "container")} item={itemId}, fromSlot={sourceSlot}, toSlot={targetSlot}, targetIsBag={targetIsBag}");
        }

        private static async ETTask OnItemClick(
            SearchPanelComponent self,
            RectTransform view,
            long itemId,
            bool isBag,
            int configId,
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || eventData == null)
            {
                return;
            }

            if (eventData.dragging || configId <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            await self.OpenItemClickedAsync(configId, isBag ? itemId : 0);
        }

        private static async ETTask OpenItemClickedAsync(this SearchPanelComponent self, int configId, long itemUid)
        {
            if (self == null || self.IsDisposed || configId <= 0)
            {
                return;
            }

            ItemClickedComponent itemClicked = self.GetOrCreateItemClickedCommon();
            if (itemClicked == null || itemClicked.IsDisposed)
            {
                return;
            }

            ItemClickedOpenData openData = new()
            {
                LobbyPanelRef = default,
                ConfigId = configId,
                ItemUid = itemUid,
                AllowEquipAction = false,
            };

            await YIUIEventSystem.Open(itemClicked, openData);
        }

        private static ItemClickedComponent GetOrCreateItemClickedCommon(this SearchPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return null;
            }

            ItemClickedComponent existing = self.ItemClickedCommon;
            if (existing != null && !existing.IsDisposed)
            {
                return existing;
            }

            RectTransform root = self.UIBase?.OwnerRectTransform;
            if (root == null)
            {
                return null;
            }

            Transform parent = root.FindChildByName($"{ItemClickedComponent.ResName}{YIUIConstHelper.Const.UIParentName}");
            if (parent == null)
            {
                Log.Warning("[SearchPanel] 未找到 ItemClickedParent");
                return null;
            }

            ItemClickedComponent created = YIUIFactory.Instantiate<ItemClickedComponent>(self.Scene(), self, parent) as ItemClickedComponent;
            if (created == null)
            {
                return null;
            }

            created.UIBase?.SetActive(false);
            self.ItemClickedCommon = created;
            return created;
        }

        private static void ApplyFootprintFromSolver(SearchPanelComponent self, bool isBag, long itemId)
        {
            GridPlacementSolver solver = isBag ? self.BagSolver : self.ContainerSolver;
            Dictionary<long, RectTransform> map = isBag ? self.BagItemViews : self.ContainerItemViews;
            if (solver == null || !solver.TryGetItem(itemId, out GridItemFootprint footprint))
            {
                return;
            }

            if (!map.TryGetValue(itemId, out RectTransform view) || view == null)
            {
                return;
            }

            ApplyFootprint(view, footprint, self.CellSize, self.CellSpacing, self.CellPadding);
        }

        private static void ApplyFootprint(
            RectTransform view,
            GridItemFootprint footprint,
            Vector2 cellSize,
            Vector2 spacing,
            Vector2 padding)
        {
            view.anchorMin = new Vector2(0, 1);
            view.anchorMax = new Vector2(0, 1);
            view.pivot = new Vector2(0, 1);

            float width = footprint.Width * cellSize.x + (footprint.Width - 1) * spacing.x;
            float height = footprint.Height * cellSize.y + (footprint.Height - 1) * spacing.y;
            float posX = padding.x + footprint.X * (cellSize.x + spacing.x);
            float posY = -padding.y - footprint.Y * (cellSize.y + spacing.y);

            view.sizeDelta = new Vector2(width, height);
            view.anchoredPosition = new Vector2(posX, posY);
        }

        private static void EnsureContainerSolver(SearchPanelComponent self, int rows)
        {
            rows = Math.Max(rows, MinRows);
            self.ContainerRows = rows;
            if (self.ContainerSolver == null || self.ContainerSolver.Cols != self.ContainerCols || self.ContainerSolver.Rows != rows)
            {
                self.ContainerSolver = new GridPlacementSolver(self.ContainerCols, rows);
            }
        }

        private static void EnsureBagSolver(SearchPanelComponent self, int cols, int rows)
        {
            cols = Math.Max(cols, 1);
            rows = Math.Max(rows, 1);
            self.BagCols = cols;
            self.BagRows = rows;
            if (self.BagSolver == null || self.BagSolver.Cols != cols || self.BagSolver.Rows != rows)
            {
                self.BagSolver = new GridPlacementSolver(cols, rows);
            }
        }

        private static void EnsureSecureSolver(SearchPanelComponent self, int cols, int rows)
        {
            cols = Math.Max(1, cols);
            rows = Math.Max(1, rows);
            self.SecureCols = cols;
            self.SecureRows = rows;
            if (self.SecureSolver == null || self.SecureSolver.Cols != cols || self.SecureSolver.Rows != rows)
            {
                self.SecureSolver = new GridPlacementSolver(cols, rows);
            }
        }

        private static void RenderGrid(
            RectTransform gridRoot,
            Dictionary<int, RectTransform> cellMap,
            int cols,
            int rows,
            Vector2 cellSize,
            Vector2 spacing,
            Vector2 padding,
            bool isBag)
        {
            if (gridRoot == null)
            {
                return;
            }

            HashSet<int> alive = new();
            RectTransform cellTemplate = FindDirectChildRectTransform(
                gridRoot,
                "CellTemplate",
                "GridCellTemplate",
                "CellStyleSource",
                "GridCellStyleSource");
            for (int y = 0; y < rows; ++y)
            {
                for (int x = 0; x < cols; ++x)
                {
                    int id = y * cols + x;
                    alive.Add(id);
                    if (!cellMap.TryGetValue(id, out RectTransform cell) || cell == null)
                    {
                        cell = CreateGridCellView(gridRoot, cellTemplate, $"Cell_{x}_{y}");
                        cellMap[id] = cell;
                    }

                    Image cellImage = cell.GetComponent<Image>();
                    if (cellImage != null)
                    {
                        ApplyGridCellVisual(cellImage, gridRoot, GetGridCellColor(isBag, id));
                        cellImage.raycastTarget = false;
                    }

                    GridItemFootprint footprint = new GridItemFootprint
                    {
                        ItemId = 0,
                        X = x,
                        Y = y,
                        Width = 1,
                        Height = 1
                    };
                    ApplyFootprint(cell, footprint, cellSize, spacing, padding);
                }
            }

            RemoveDeadViews(cellMap, alive);
        }

        private static RectTransform CreateGridCellView(RectTransform gridRoot, RectTransform cellTemplate, string cellName)
        {
            RectTransform cell = null;
            if (cellTemplate != null)
            {
                cell = UnityEngine.Object.Instantiate(cellTemplate, gridRoot);
            }

            if (cell == null)
            {
                GameObject go = new GameObject(cellName, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(gridRoot, false);
                cell = go.GetComponent<RectTransform>();
            }

            cell.name = cellName;
            cell.gameObject.SetActive(true);
            return cell;
        }

        private static void ApplyGridCellVisual(Image targetImage, RectTransform gridRoot, Color fallbackColor)
        {
            if (targetImage == null)
            {
                return;
            }

            Image styleSource = ResolveGridCellStyleSource(gridRoot);
            if (styleSource != null)
            {
                CopyGridCellImageStyle(styleSource, targetImage);
                // 用代码侧颜色覆盖，保留安全格子/普通格子差异。
                targetImage.color = fallbackColor;
                // styleSource 可能是 gridRoot 自己的 Image（作为整体背景）。这种情况下不要禁用它。
                // 只隐藏作为模板的子节点，避免在界面上多出一个“样式源”图片。
                if (styleSource.transform != gridRoot)
                {
                    styleSource.enabled = false;
                    styleSource.raycastTarget = false;
                }
            }
            else
            {
                targetImage.overrideSprite = null;
                targetImage.material = null;
                targetImage.type = Image.Type.Simple;
                targetImage.preserveAspect = false;
                targetImage.fillCenter = true;
                targetImage.fillMethod = Image.FillMethod.Radial360;
                targetImage.fillOrigin = 0;
                targetImage.fillClockwise = true;
                targetImage.fillAmount = 1f;
                targetImage.useSpriteMesh = false;
                targetImage.pixelsPerUnitMultiplier = 1f;
                targetImage.maskable = true;
                targetImage.color = fallbackColor;
                targetImage.enabled = true;
            }

            targetImage.raycastTarget = false;
        }

        private static Image ResolveGridCellStyleSource(RectTransform gridRoot)
        {
            if (gridRoot == null)
            {
                return null;
            }

            RectTransform styleRect = FindDirectChildRectTransform(
                gridRoot,
                "CellTemplate",
                "GridCellTemplate",
                "CellStyleSource",
                "GridCellStyleSource");
            Image styleImage = styleRect?.GetComponent<Image>();
            if (styleImage != null && styleImage.sprite != null)
            {
                return styleImage;
            }

            Image rootImage = gridRoot.GetComponent<Image>();
            return rootImage != null && rootImage.sprite != null ? rootImage : null;
        }

        private static void CopyGridCellImageStyle(Image sourceImage, Image targetImage)
        {
            if (sourceImage == null || targetImage == null)
            {
                return;
            }

            targetImage.sprite = sourceImage.sprite;
            targetImage.overrideSprite = sourceImage.overrideSprite;
            targetImage.material = sourceImage.material;
            targetImage.type = sourceImage.type;
            targetImage.preserveAspect = sourceImage.preserveAspect;
            targetImage.fillCenter = sourceImage.fillCenter;
            targetImage.fillMethod = sourceImage.fillMethod;
            targetImage.fillOrigin = sourceImage.fillOrigin;
            targetImage.fillClockwise = sourceImage.fillClockwise;
            targetImage.fillAmount = sourceImage.fillAmount;
            targetImage.useSpriteMesh = sourceImage.useSpriteMesh;
            targetImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
            targetImage.maskable = sourceImage.maskable;
            targetImage.enabled = true;
        }

        private static RectTransform FindDirectChildRectTransform(RectTransform parent, params string[] names)
        {
            if (parent == null || names == null || names.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < names.Length; ++i)
            {
                string name = names[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                Transform child = parent.Find(name);
                if (child is RectTransform rect)
                {
                    return rect;
                }
            }

            return null;
        }

        private static Color GetGridCellColor(bool isBag, int slotIndex)
        {
            if (isBag && ExtractionInventoryConfig.IsSafeSlot(slotIndex))
            {
                return new Color(0.22f, 0.58f, 0.95f, 0.18f);
            }

            return new Color(1f, 1f, 1f, 0.08f);
        }

        private static int CalcCols(RectTransform boardRoot, Vector2 cellSize, Vector2 spacing, int fallback)
        {
            if (boardRoot == null)
            {
                return fallback;
            }

            float width = boardRoot.rect.width;
            float denominator = Mathf.Max(1f, cellSize.x + spacing.x);
            if (width <= 0)
            {
                return fallback;
            }

            return Math.Max(1, Mathf.FloorToInt((width + spacing.x) / denominator));
        }

        private static int CalcRowsBySlot(int slotCount, int cols, int minRows)
        {
            if (cols <= 0)
            {
                return minRows;
            }

            int rows = (slotCount + cols - 1) / cols;
            return Math.Max(rows, minRows);
        }

        private static int GetItemWidth(int configId)
        {
            ItemConfig config = LegacyItemConfigCompatHelper.GetDisplayItemConfig(configId);
            if (config == null || config.GridWidth <= 0)
            {
                return 1;
            }

            return config.GridWidth;
        }

        private static int GetItemHeight(int configId)
        {
            ItemConfig config = LegacyItemConfigCompatHelper.GetDisplayItemConfig(configId);
            if (config == null || config.GridHeight <= 0)
            {
                return 1;
            }

            return config.GridHeight;
        }

        private static Vector2 GetRectSize(RectTransform rect, Vector2 fallback)
        {
            if (rect == null)
            {
                return fallback;
            }

            Vector2 size = rect.rect.size;
            if (size.x <= 0 || size.y <= 0)
            {
                return fallback;
            }

            return size;
        }

        private static void ResizeBoard(
            RectTransform boardRoot,
            RectTransform itemsLayer,
            int cols,
            int rows,
            Vector2 cellSize,
            Vector2 spacing,
            Vector2 padding)
        {
            float width = padding.x * 2 + cols * cellSize.x + Mathf.Max(0, cols - 1) * spacing.x;
            float height = padding.y * 2 + rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;

            if (boardRoot != null)
            {
                boardRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                boardRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            if (itemsLayer != null)
            {
                itemsLayer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                itemsLayer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        private static void RemoveDeadViews(Dictionary<long, RectTransform> map, HashSet<long> alive)
        {
            List<long> removeIds = null;
            foreach (KeyValuePair<long, RectTransform> pair in map)
            {
                if (alive.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }

                removeIds ??= new List<long>();
                removeIds.Add(pair.Key);
            }

            if (removeIds == null)
            {
                return;
            }

            foreach (long id in removeIds)
            {
                map.Remove(id);
            }
        }

        private static void RemoveDeadViews(Dictionary<int, RectTransform> map, HashSet<int> alive)
        {
            List<int> removeIds = null;
            foreach (KeyValuePair<int, RectTransform> pair in map)
            {
                if (alive.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }

                removeIds ??= new List<int>();
                removeIds.Add(pair.Key);
            }

            if (removeIds == null)
            {
                return;
            }

            foreach (int id in removeIds)
            {
                map.Remove(id);
            }
        }

        private static void ReleaseViews(Dictionary<long, RectTransform> map)
        {
            foreach (KeyValuePair<long, RectTransform> pair in map)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }
            }

            map.Clear();
        }

        private static void ReleaseViews(Dictionary<int, RectTransform> map)
        {
            foreach (KeyValuePair<int, RectTransform> pair in map)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.gameObject);
                }
            }

            map.Clear();
        }

        private static void SetTemplateActive(RectTransform template, bool active)
        {
            if (template != null)
            {
                template.gameObject.SetActive(active);
            }
        }

        private static void ResolveDisplayInfo(int configId, out string name, out string icon, out int sortCategory)
        {
            ItemConfig itemConfig = LegacyItemConfigCompatHelper.GetDisplayItemConfig(configId);
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

        private static string BuildSnapshot(SearchPanelComponent self, ECAInteractClientComponent runtime, ItemComponent itemComponent)
        {
            StringBuilder builder = new();
            builder.Append((int)self.OpenMode);
            builder.Append(':');
            builder.Append((int)self.CorpseSubType);
            builder.Append(':');
            builder.Append(self.CurrentPointId ?? string.Empty);
            builder.Append('|');

            int containerCount = SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode)
                ? runtime?.ContainerItems.Count ?? 0
                : 0;
            builder.Append(containerCount);
            for (int i = 0; i < containerCount; ++i)
            {
                ContainerClientItemData item = runtime.ContainerItems[i];
                builder.Append('|');
                builder.Append(item.SlotIndex);
                builder.Append(':');
                builder.Append(item.ConfigId);
                builder.Append(':');
                builder.Append(item.Count);
            }

            builder.Append('|');
            int capacity = itemComponent?.Capacity ?? 0;
            builder.Append(capacity);
            builder.Append(':');
            builder.Append(itemComponent?.Width ?? 0);
            builder.Append('x');
            builder.Append(itemComponent?.Height ?? 0);
            for (int slot = 0; slot < capacity; ++slot)
            {
                Item item = itemComponent.GetItemBySlot(slot);
                if (item == null || item.Count <= 0)
                {
                    continue;
                }

                builder.Append('|');
                builder.Append(item.Id);
                builder.Append(':');
                builder.Append(slot);
                builder.Append(':');
                builder.Append(item.ConfigId);
                builder.Append(':');
                builder.Append(item.Count);
            }

            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            builder.Append('|');
            builder.Append(loadout?.MainWeaponConfigId ?? 0);
            builder.Append(':');
            builder.Append(loadout?.SubWeaponConfigId ?? 0);
            builder.Append(':');
            builder.Append(loadout?.ArmorConfigId ?? 0);
            builder.Append(':');
            builder.Append(loadout?.BackpackConfigId ?? 0);
            builder.Append(':');
            builder.Append(loadout?.BagWidth ?? 0);
            builder.Append('x');
            builder.Append(loadout?.BagHeight ?? 0);
            builder.Append(':');
            builder.Append(loadout?.SecureWidth ?? 0);
            builder.Append('x');
            builder.Append(loadout?.SecureHeight ?? 0);

            if (loadout?.CarriedSecureItems != null)
            {
                for (int i = 0; i < loadout.CarriedSecureItems.Count; ++i)
                {
                    LoadoutGridItemInfo item = loadout.CarriedSecureItems[i];
                    builder.Append('|');
                    builder.Append(item.AnchorSlotIndex);
                    builder.Append(':');
                    builder.Append(item.ConfigId);
                    builder.Append(':');
                    builder.Append(item.Count);
                    builder.Append(':');
                    builder.Append(item.GridWidth);
                    builder.Append('x');
                    builder.Append(item.GridHeight);
                }
            }

            return builder.ToString();
        }

        private static int ReadQuickChooseMinQuality(SearchPanelComponent self)
        {
            Dropdown dropdown = self.QuickChooseDropdown;
            int dropdownValue = dropdown != null ? dropdown.value : 0;
            return ResolveMinQualityFromDropdownValue(dropdownValue);
        }

        private static int ResolveMinQualityFromDropdownValue(int dropdownValue)
        {
            return Math.Max(1, dropdownValue + 1);
        }

        private static bool TryGetSourceSlot(SearchPanelComponent self, bool isBag, long itemId, out int sourceSlot)
        {
            sourceSlot = -1;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            if (isBag)
            {
                ItemComponent itemComponent = self.Root()?.GetComponent<ItemComponent>();
                Item item = itemComponent?.GetItemById(itemId);
                if (item == null || item.IsDisposed)
                {
                    return false;
                }

                sourceSlot = item.SlotIndex;
                return true;
            }

            sourceSlot = (int)(itemId - 1);
            return sourceSlot >= 0;
        }

        private static bool CanMoveBagToSlot(SearchPanelComponent self, int targetSlot)
        {
            if (targetSlot < 0)
            {
                return false;
            }

            ItemComponent itemComponent = self.Root()?.GetComponent<ItemComponent>();
            return itemComponent != null && targetSlot < itemComponent.Capacity;
        }

        private static async ETTask MoveBagItem(Scene root, long itemId, int targetSlot)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            C2M_MoveItem request = C2M_MoveItem.Create();
            request.ItemId = itemId;
            request.ToSlot = targetSlot;

            EntityRef<Scene> rootRef = root;
            M2C_MoveItem response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_MoveItem;
            root = rootRef;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (response != null && response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[SearchDrag] bag move failed: itemId={itemId}, toSlot={targetSlot}, error={response.Error}, msg={response.Message}");
            }
        }

        private static async ETTask MoveContainerItem(
            Scene root,
            bool sourceIsBag,
            int sourceSlot,
            long sourceItemId,
            bool targetIsBag,
            int targetSlot)
        {
            if (root == null || root.IsDisposed || sourceSlot < 0 || targetSlot < 0)
            {
                return;
            }

            await ECAInteractHelper.MoveContainerItem(root, sourceIsBag, sourceSlot, sourceItemId, targetIsBag, targetSlot);
        }

        private static bool TryGetDropArea(
            SearchPanelComponent self,
            PointerEventData eventData,
            out bool targetIsBag,
            out int targetSlot)
        {
            targetIsBag = false;
            targetSlot = -1;
            if (self == null || self.IsDisposed || eventData == null)
            {
                return false;
            }

            Camera eventCamera = eventData.pressEventCamera;
            if (TryResolveBoardSlot(self.u_ComBagBoardRoot, self.BagCols, self.BagRows, self.CellSize, self.CellSpacing, self.CellPadding, eventData.position, eventCamera, out int bagDisplaySlot) &&
                TryMapBagDisplaySlotToActualSlot(self.Root()?.GetComponent<ItemComponent>(), bagDisplaySlot, out targetSlot))
            {
                targetIsBag = true;
                return true;
            }

            if (TryResolveBoardSlot(self.u_ComContainerBoardRoot, self.ContainerCols, self.ContainerRows, self.CellSize, self.CellSpacing, self.CellPadding, eventData.position, eventCamera, out targetSlot))
            {
                targetIsBag = false;
                return true;
            }

            return false;
        }

        private static bool TryResolveBoardSlot(
            RectTransform boardRoot,
            int cols,
            int rows,
            Vector2 cellSize,
            Vector2 spacing,
            Vector2 padding,
            Vector2 screenPosition,
            Camera eventCamera,
            out int slotIndex)
        {
            slotIndex = -1;
            if (boardRoot == null || !boardRoot.gameObject.activeInHierarchy || cols <= 0 || rows <= 0)
            {
                return false;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(boardRoot, screenPosition, eventCamera))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRoot, screenPosition, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            Vector2 rectSize = boardRoot.rect.size;
            float left = localPoint.x + rectSize.x * 0.5f;
            float top = rectSize.y * 0.5f - localPoint.y;
            float strideX = cellSize.x + spacing.x;
            float strideY = cellSize.y + spacing.y;
            int x = Mathf.FloorToInt((left - padding.x) / Mathf.Max(1f, strideX));
            int y = Mathf.FloorToInt((top - padding.y) / Mathf.Max(1f, strideY));
            x = Mathf.Clamp(x, 0, Mathf.Max(0, cols - 1));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, rows - 1));
            slotIndex = y * cols + x;
            return true;
        }

        private static int GetVisibleBagSlotCount(int capacity)
        {
            if (capacity <= 0)
            {
                return 0;
            }

            int count = 0;
            for (int actualSlot = 0; actualSlot < capacity; ++actualSlot)
            {
                if (!ExtractionInventoryConfig.IsSafeSlot(actualSlot))
                {
                    ++count;
                }
            }

            return count;
        }

        private static void ResolveBagLayout(
            SearchPanelComponent self,
            ItemComponent itemComponent,
            LoadoutComponent loadout,
            int visibleCapacity,
            out int cols,
            out int rows)
        {
            int configuredCols = loadout?.BagWidth ?? 0;
            int configuredRows = loadout?.BagHeight ?? 0;
            if (configuredCols <= 0 || configuredRows <= 0)
            {
                configuredCols = itemComponent?.Width ?? 0;
                configuredRows = itemComponent?.Height ?? 0;
            }

            if (configuredCols > 0 && configuredRows > 0)
            {
                cols = configuredCols;
                rows = configuredRows;
                return;
            }

            cols = CalcCols(self.u_ComBagBoardRoot, self.CellSize, self.CellSpacing, FallbackCols);
            rows = CalcRowsBySlot(visibleCapacity, cols, MinRows);
        }

        private static int ConvertActualBagSlotToDisplaySlot(int actualSlot)
        {
            if (actualSlot < 0 || ExtractionInventoryConfig.IsSafeSlot(actualSlot))
            {
                return -1;
            }

            int displaySlot = 0;
            for (int slot = 0; slot < actualSlot; ++slot)
            {
                if (!ExtractionInventoryConfig.IsSafeSlot(slot))
                {
                    ++displaySlot;
                }
            }

            return displaySlot;
        }

        private static bool TryMapBagDisplaySlotToActualSlot(ItemComponent itemComponent, int displaySlot, out int actualSlot)
        {
            actualSlot = -1;
            if (itemComponent == null || displaySlot < 0)
            {
                return false;
            }

            int currentDisplaySlot = 0;
            for (int slot = 0; slot < itemComponent.Capacity; ++slot)
            {
                if (ExtractionInventoryConfig.IsSafeSlot(slot))
                {
                    continue;
                }

                if (currentDisplaySlot == displaySlot)
                {
                    actualSlot = slot;
                    return true;
                }

                ++currentDisplaySlot;
            }

            return false;
        }

        private static async ETTask TakeQualifiedItems(SearchPanelComponent self)
        {
            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                return;
            }

            if (!SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode))
            {
                return;
            }

            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null || string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
            {
                return;
            }

            int minQuality = Math.Max(1, self.QuickChooseMinQuality);
            List<int> targetSlots = new();
            int count = runtime.ContainerItems.Count;
            for (int i = 0; i < count; ++i)
            {
                ContainerClientItemData item = runtime.ContainerItems[i];
                ItemConfig config = LegacyItemConfigCompatHelper.GetDisplayItemConfig(item.ConfigId);
                if (config == null || config.Quality < minQuality)
                {
                    continue;
                }

                targetSlots.Add(item.SlotIndex);
            }

            if (targetSlots.Count == 0)
            {
                Log.Info($"[ECAClient][SearchPanel] quick choose no items matched, minQuality={minQuality}");
                return;
            }

            targetSlots.Sort();
            EntityRef<Scene> rootRef = root;
            foreach (int slotIndex in targetSlots)
            {
                root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    return;
                }

                await ECAInteractHelper.TakeItem(root, slotIndex);
            }
        }
        
        [YIUIInvoke(SearchPanelComponent.OnEventClickQuickChooseInvoke)]
        private static async ETTask OnEventClickQuickChooseInvoke(this SearchPanelComponent self)
        {
            EntityRef<SearchPanelComponent> selfRef = self;
            await TakeQualifiedItems(self);
            self = selfRef;
        }
        
        [YIUIInvoke(SearchPanelComponent.OnEventExitInvoke)]
        private static async ETTask OnEventExitInvoke(this SearchPanelComponent self)
        {
            Scene root = self.Root();
            if (root != null && !root.IsDisposed)
            {
                ECAInteractClientComponent runtime = root.GetComponent<ECAInteractClientComponent>();
                if (SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode) &&
                    runtime != null &&
                    !string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
                {
                    ECAInteractHelper.CloseContainer(root);
                }
                else
                {
                    root.YIUIMgr()?.ClosePanel<SearchPanelComponent>();
                }
            }

            await ETTask.CompletedTask;
        }
        
        [YIUIInvoke(SearchPanelComponent.OnEventQuickChooseInvoke)]
        private static void OnEventQuickChooseInvoke(this SearchPanelComponent self, int p1)
        {
            self.QuickChooseMinQuality = ResolveMinQualityFromDropdownValue(p1);
        }
        #endregion YIUIEvent结束

        #region 搜索动效相关方法

        /// <summary>
        /// 更新搜索动效状态，在LateUpdate中调用
        /// </summary>
        private static void UpdateSearchEffects(SearchPanelComponent self)
        {
            if (self == null || self.IsDisposed || !SearchPanelModeHelper.ShouldUseContainerClose(self.OpenMode))
            {
                return;
            }

            long nowMs = TimeInfo.Instance.ClientNow();
            
            // 检查每个正在搜索的槽位
            List<int> completedSlots = null;
            foreach (KeyValuePair<int, long> pair in self.SlotSearchStartTimes)
            {
                int slot = pair.Key;
                long startTime = pair.Value;
                
                // 如果已经搜索完成，跳过
                if (self.SearchedSlots.Contains(slot))
                {
                    continue;
                }
                
                // 获取该槽位的搜索持续时间
                long durationMs = self.SlotSearchDurations.TryGetValue(slot, out long duration)
                    ? duration
                    : ExtractionInventoryConfig.GetDefaultItemSearchDurationMs();
                
                // 检查是否搜索完成
                if (nowMs - startTime >= durationMs)
                {
                    completedSlots ??= new List<int>();
                    completedSlots.Add(slot);
                }
            }
            
            // 处理搜索完成的槽位
            if (completedSlots != null)
            {
                foreach (int slot in completedSlots)
                {
                    self.SearchedSlots.Add(slot);
                    
                    // 隐藏搜索动效
                    if (self.SlotSearchingEffects.TryGetValue(slot, out GameObject effect) && effect != null)
                    {
                        effect.SetActive(false);
                    }
                    
                    Log.Info($"[ECAClient][SearchPanel] slot {slot} search completed");
                }
            }
        }

        /// <summary>
        /// 确保搜索动效存在或隐藏
        /// </summary>
        private static void EnsureSearchEffect(SearchPanelComponent self, RectTransform itemView, int slot, bool show)
        {
            if (itemView == null)
            {
                return;
            }

            // 尝试获取已存在的动效
            if (!self.SlotSearchingEffects.TryGetValue(slot, out GameObject effect) || effect == null)
            {
                if (!show)
                {
                    return;
                }
                
                // 创建搜索动效
                effect = CreateSearchEffect(itemView);
                if (effect != null)
                {
                    self.SlotSearchingEffects[slot] = effect;
                }
            }

            if (effect != null)
            {
                effect.SetActive(show);
            }
        }

        /// <summary>
        /// 创建搜索动效GameObject
        /// </summary>
        private static GameObject CreateSearchEffect(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            // 创建一个简单的转圈圈动效
            GameObject effectGo = new GameObject("SearchingEffect", typeof(RectTransform), typeof(Image));
            effectGo.transform.SetParent(parent, false);
            
            RectTransform effectRect = effectGo.GetComponent<RectTransform>();
            effectRect.anchorMin = Vector2.zero;
            effectRect.anchorMax = Vector2.one;
            effectRect.offsetMin = Vector2.zero;
            effectRect.offsetMax = Vector2.zero;
            
            Image effectImage = effectGo.GetComponent<Image>();
            effectImage.color = new Color(0f, 0f, 0f, 0.6f);
            effectImage.raycastTarget = false;
            
            // 创建旋转的图标
            GameObject spinnerGo = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
            spinnerGo.transform.SetParent(effectRect, false);
            
            RectTransform spinnerRect = spinnerGo.GetComponent<RectTransform>();
            spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.sizeDelta = new Vector2(32, 32);
            spinnerRect.anchoredPosition = Vector2.zero;
            
            Image spinnerImage = spinnerGo.GetComponent<Image>();
            spinnerImage.color = Color.white;
            spinnerImage.raycastTarget = false;
            
            // 添加旋转动画组件
            SearchEffectSpinner spinner = spinnerGo.AddComponent<SearchEffectSpinner>();
            spinner.RotationSpeed = 360f; // 每秒旋转360度
            
            return effectGo;
        }

        /// <summary>
        /// 清理所有搜索动效
        /// </summary>
        private static void ClearSearchEffects(SearchPanelComponent self)
        {
            if (self == null)
            {
                return;
            }

            foreach (KeyValuePair<int, GameObject> pair in self.SlotSearchingEffects)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value);
                }
            }

            self.SlotSearchingEffects.Clear();
        }

        private static void SetGameObjectActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static void SetGameObjectActive(GameObject gameObject, bool active)
        {
            if (gameObject != null && gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }

        private static void SetOwnerChildActive(SearchPanelComponent self, string childName, bool active)
        {
            Transform childTransform = FindOwnerChildRecursive<Transform>(self, childName);
            if (childTransform == null)
            {
                return;
            }

            SetGameObjectActive(childTransform.gameObject, active);
        }

        private static void ApplyBagOnlyLayout(SearchPanelComponent self)
        {
            RectTransform bagBoard = self.u_ComBagBoardRoot;
            if (bagBoard == null)
            {
                return;
            }

            bagBoard.anchorMin = new Vector2(self.BagBoardAnchorMin.x, self.ContainerBoardAnchorMin.y);
            bagBoard.anchorMax = self.BagBoardAnchorMax;
            bagBoard.anchoredPosition = self.BagBoardAnchoredPosition;
            bagBoard.sizeDelta = self.BagBoardSizeDelta;
        }

        private static void RestoreBagBoardLayout(SearchPanelComponent self)
        {
            RectTransform bagBoard = self.u_ComBagBoardRoot;
            if (bagBoard == null)
            {
                return;
            }

            bagBoard.anchorMin = self.BagBoardAnchorMin;
            bagBoard.anchorMax = self.BagBoardAnchorMax;
            bagBoard.anchoredPosition = self.BagBoardAnchoredPosition;
            bagBoard.sizeDelta = self.BagBoardSizeDelta;
        }

        private static void ApplyDisplayInfo(SearchPanelComponent self, SearchPanelDisplayInfo displayInfo)
        {
            TrySetText(self.ModeTitleText, displayInfo.TitleText);
            TrySetText(self.ModeSubTitleText, displayInfo.SubTitleText);
            TrySetText(self.ContainerTitleText, displayInfo.ContainerTitleText);
            TrySetText(self.BagTitleText, displayInfo.BagTitleText);
            TrySetText(self.QuickChooseButtonLabel, displayInfo.QuickActionText);

            Color accentColor = ResolveAccentColor(displayInfo.StyleKey);
            if (self.ModeAccentImage != null)
            {
                self.ModeAccentImage.color = accentColor;
            }

            if (self.QuickChooseButtonImage != null)
            {
                self.QuickChooseButtonImage.color = accentColor;
            }
        }

        private static void TrySetText(TMP_Text textComponent, string content)
        {
            if (textComponent == null || content == null || textComponent.text == content)
            {
                return;
            }

            textComponent.text = content;
        }

        private static Color ResolveAccentColor(string styleKey)
        {
            return styleKey switch
            {
                "container" => new Color(0.83f, 0.66f, 0.27f, 1f),
                "ground_drop" => new Color(0.38f, 0.76f, 0.42f, 1f),
                "corpse_player" => new Color(0.82f, 0.31f, 0.31f, 1f),
                "corpse_monster" => new Color(0.73f, 0.43f, 0.23f, 1f),
                "corpse_boss" => new Color(0.58f, 0.34f, 0.78f, 1f),
                "corpse" => new Color(0.70f, 0.39f, 0.39f, 1f),
                _ => new Color(0.28f, 0.56f, 0.88f, 1f),
            };
        }

        private static T FindOwnerChild<T>(SearchPanelComponent self, string childName) where T : Component
        {
            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Transform childTransform = rootTransform.Find(childName);
            return childTransform != null ? childTransform.GetComponent<T>() : null;
        }

        private static T FindOwnerChildRecursive<T>(SearchPanelComponent self, string childName) where T : Component
        {
            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            foreach (T component in rootTransform.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.name == childName)
                {
                    return component;
                }
            }

            return null;
        }

        #endregion 搜索动效相关方法
    }
}
