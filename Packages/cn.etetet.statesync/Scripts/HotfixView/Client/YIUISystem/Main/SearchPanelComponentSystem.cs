using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(SearchPanelComponent))]
    public static partial class SearchPanelComponentSystem
    {
        private const int FallbackCols = 8;
        private const int MinRows = 4;

        [EntitySystem]
        private static void YIUIInitialize(this SearchPanelComponent self)
        {
            self.LastContainerSnapshot = null;
            InitLayout(self);
            CacheGridRoots(self);
            self.QuickChooseMinQuality = ReadQuickChooseMinQuality(self);
            SetTemplateActive(self.u_ComContainerItemTemplate, false);
            SetTemplateActive(self.u_ComBagItemTemplate, false);
            
            // 初始化搜索动效配置
            self.CurrentSearchingPointId = null;
        }

        [EntitySystem]
        private static void Destroy(this SearchPanelComponent self)
        {
            ReleaseViews(self.ContainerItemViews);
            ReleaseViews(self.BagItemViews);
            ReleaseViews(self.ContainerGridCellViews);
            ReleaseViews(self.BagGridCellViews);
            self.ContainerSolver = null;
            self.BagSolver = null;
            self.LastContainerSnapshot = null;
            self.ContainerGridRoot = null;
            self.BagGridRoot = null;
            self.IsDragging = false;
            self.DraggingView = null;
            
            // 清理搜索动效相关数据
            ClearSearchEffects(self);
            self.SlotSearchStartTimes.Clear();
            self.SlotSearchDurations.Clear();
            self.SearchedSlots.Clear();
            self.CurrentSearchingPointId = null;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SearchPanelComponent self)
        {
            self.LastContainerSnapshot = null;
            self.QuickChooseMinQuality = ReadQuickChooseMinQuality(self);
            
            // 检查是否切换了容器，如果是则重置搜索状态
            Scene root = self.Root();
            ECAInteractClientComponent runtime = root?.GetComponent<ECAInteractClientComponent>();
            string currentPointId = runtime?.OpenContainerPointId;
            if (!string.IsNullOrEmpty(currentPointId) && currentPointId != self.CurrentSearchingPointId)
            {
                // 切换了容器，重置搜索状态
                ClearSearchEffects(self);
                self.SlotSearchStartTimes.Clear();
                self.SlotSearchDurations.Clear();
                self.SearchedSlots.Clear();
                self.CurrentSearchingPointId = currentPointId;
            }
            
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
            UpdateSearchEffects(self);
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

            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            ItemComponent itemComponent = root.GetComponent<ItemComponent>();
            string snapshot = BuildSnapshot(runtime, itemComponent);
            if (!force && self.LastContainerSnapshot == snapshot)
            {
                return;
            }

            self.LastContainerSnapshot = snapshot;
            RenderContainer(self, runtime);
            RenderBag(self, itemComponent);

            Log.Info(
                $"[ECAClient][SearchPanel] refresh point={runtime?.OpenContainerPointId ?? "null"}, container={runtime?.ContainerItems.Count ?? 0}, bag={itemComponent?.GetUsedSlotCount() ?? 0}");
        }

        private static void InitLayout(SearchPanelComponent self)
        {
            Vector2 defaultCell = new Vector2(96, 96);
            Vector2 containerCell = GetRectSize(self.u_ComContainerItemTemplate, defaultCell);
            Vector2 bagCell = GetRectSize(self.u_ComBagItemTemplate, defaultCell);
            self.CellSize = bagCell.x > 0 && bagCell.y > 0 ? bagCell : containerCell;
            self.CellSpacing = new Vector2(8, 8);
            self.CellPadding = new Vector2(8, 8);

            self.ContainerCols = CalcCols(self.u_ComContainerBoardRoot, self.CellSize, self.CellSpacing, FallbackCols);
            self.BagCols = CalcCols(self.u_ComBagBoardRoot, self.CellSize, self.CellSpacing, FallbackCols);
            self.ContainerRows = MinRows;
            self.BagRows = MinRows;

            EnsureContainerSolver(self, self.ContainerRows);
            EnsureBagSolver(self, self.BagRows);
        }

        private static void CacheGridRoots(SearchPanelComponent self)
        {
            self.ContainerGridRoot = self.u_ComContainerBoardRoot?.Find("GridRoot") as RectTransform;
            self.BagGridRoot = self.u_ComBagBoardRoot?.Find("GridRoot") as RectTransform;
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

            int needRows = CalcRowsBySlot(maxSlot + 1, self.ContainerCols, MinRows);
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
                BindDrag(self, view, viewId, false);
                
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
            int capacity = itemComponent?.Capacity ?? 0;
            int needRows = CalcRowsBySlot(capacity, self.BagCols, MinRows);
            EnsureBagSolver(self, needRows);
            self.BagSolver.Clear();

            HashSet<long> alive = new();
            if (itemComponent != null)
            {
                for (int slot = 0; slot < capacity; ++slot)
                {
                    Item item = itemComponent.GetItemBySlot(slot);
                    if (item == null || item.Count <= 0)
                    {
                        continue;
                    }

                    alive.Add(item.Id);
                    GridItemFootprint footprint = new GridItemFootprint
                    {
                        ItemId = item.Id,
                        X = slot % self.BagCols,
                        Y = slot / self.BagCols,
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
                    BindDrag(self, view, item.Id, true);
                }
            }

            RemoveDeadViews(self.BagItemViews, alive);
            ResizeBoard(self.u_ComBagBoardRoot, self.u_ComBagItemsLayer, self.BagCols, self.BagRows, self.CellSize, self.CellSpacing, self.CellPadding);
            RenderGrid(self.BagGridRoot, self.BagGridCellViews, self.BagCols, self.BagRows, self.CellSize, self.CellSpacing, self.CellPadding, true);
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
            ItemConfig config = ItemConfigCategory.Instance.GetOrDefault(configId);
            string itemDesc = !string.IsNullOrWhiteSpace(config?.Desc) ? config.Desc : config?.Name ?? $"Item({configId})";
            view.name = $"Item_{slotIndex}_{configId}";
            ItemQualityBgViewHelper.UpdateQualityBg(view, config?.Quality ?? 1);

            TMP_Text[] tmps = view.GetComponentsInChildren<TMP_Text>(true);
            if (tmps.Length == 1)
            {
                tmps[0].text = itemDesc;
            }
            else if (tmps.Length > 1)
            {
                tmps[0].text = itemDesc;
                tmps[1].text = string.Empty;
            }

            Text[] texts = view.GetComponentsInChildren<Text>(true);
            if (texts.Length == 1)
            {
                texts[0].text = itemDesc;
            }
            else if (texts.Length > 1)
            {
                texts[0].text = itemDesc;
                texts[1].text = string.Empty;
            }
        }

        private static void BindDrag(SearchPanelComponent self, RectTransform view, long itemId, bool isBag)
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

            EventTrigger trigger = view.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = view.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            AddTrigger(trigger, EventTriggerType.BeginDrag, OnBeginDragEvent);
            AddTrigger(trigger, EventTriggerType.Drag, OnDragEvent);
            AddTrigger(trigger, EventTriggerType.EndDrag, OnEndDragEvent);
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
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out PointerEventData eventData))
            {
                return;
            }

            OnItemBeginDrag(self, view, itemId, isBag, eventData);
        }

        private static void OnDragEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out PointerEventData eventData))
            {
                return;
            }

            OnItemDrag(self, view, itemId, isBag, eventData);
        }

        private static void OnEndDragEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out PointerEventData eventData))
            {
                return;
            }

            OnItemEndDrag(self, view, itemId, isBag, eventData);
        }

        private static void OnClickEvent(BaseEventData data)
        {
            if (!TryGetDragContext(data, out SearchPanelComponent self, out RectTransform view, out long itemId, out bool isBag, out PointerEventData eventData))
            {
                return;
            }

            OnItemClick(self, view, itemId, isBag, eventData).Coroutine();
        }

        private static bool TryGetDragContext(
            BaseEventData data,
            out SearchPanelComponent self,
            out RectTransform view,
            out long itemId,
            out bool isBag,
            out PointerEventData eventData)
        {
            self = null;
            view = null;
            itemId = 0;
            isBag = false;
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

            SearchItemDragProxy proxy = go.GetComponent<SearchItemDragProxy>();
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
            PointerEventData eventData)
        {
            if (self == null || self.IsDisposed || view == null || eventData == null)
            {
                return;
            }

            if (!isBag || eventData.dragging)
            {
                await ETTask.CompletedTask;
                return;
            }

            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            await TacticalItemClientHelper.TryUseBagItem(root, itemId);
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

        private static void EnsureBagSolver(SearchPanelComponent self, int rows)
        {
            rows = Math.Max(rows, MinRows);
            self.BagRows = rows;
            if (self.BagSolver == null || self.BagSolver.Cols != self.BagCols || self.BagSolver.Rows != rows)
            {
                self.BagSolver = new GridPlacementSolver(self.BagCols, rows);
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
            for (int y = 0; y < rows; ++y)
            {
                for (int x = 0; x < cols; ++x)
                {
                    int id = y * cols + x;
                    alive.Add(id);
                    if (!cellMap.TryGetValue(id, out RectTransform cell) || cell == null)
                    {
                        GameObject go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(gridRoot, false);
                        cell = go.GetComponent<RectTransform>();
                        cellMap[id] = cell;
                    }

                    Image cellImage = cell.GetComponent<Image>();
                    if (cellImage != null)
                    {
                        cellImage.color = GetGridCellColor(isBag, id);
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
            ItemConfig config = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (config == null || config.GridWidth <= 0)
            {
                return 1;
            }

            return config.GridWidth;
        }

        private static int GetItemHeight(int configId)
        {
            ItemConfig config = ItemConfigCategory.Instance.GetOrDefault(configId);
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

        private static string BuildSnapshot(ECAInteractClientComponent runtime, ItemComponent itemComponent)
        {
            StringBuilder builder = new();
            builder.Append(runtime?.OpenContainerPointId ?? string.Empty);
            builder.Append('|');

            int containerCount = runtime?.ContainerItems.Count ?? 0;
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

            return builder.ToString();
        }

        private static int ReadQuickChooseMinQuality(SearchPanelComponent self)
        {
            Dropdown dropdown = self.UIBase?.OwnerGameObject?.GetComponentInChildren<Dropdown>(true);
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
            if (TryResolveBoardSlot(self.u_ComBagBoardRoot, self.BagCols, self.BagRows, self.CellSize, self.CellSpacing, self.CellPadding, eventData.position, eventCamera, out targetSlot))
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
            if (boardRoot == null || cols <= 0 || rows <= 0)
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

        private static async ETTask TakeQualifiedItems(SearchPanelComponent self)
        {
            Scene root = self.Root();
            if (root == null || root.IsDisposed)
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
                ItemConfig config = ItemConfigCategory.Instance.GetOrDefault(item.ConfigId);
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
                if (runtime != null && !string.IsNullOrWhiteSpace(runtime.OpenContainerPointId))
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
            if (self == null || self.IsDisposed)
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

        #endregion 搜索动效相关方法
    }
}
