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
            SetTemplateActive(self.u_ComContainerItemTemplate, false);
            SetTemplateActive(self.u_ComBagItemTemplate, false);
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
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SearchPanelComponent self)
        {
            self.LastContainerSnapshot = null;
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
            }

            RemoveDeadViews(self.ContainerItemViews, alive);
            ResizeBoard(self.u_ComContainerBoardRoot, self.u_ComContainerItemsLayer, self.ContainerCols, self.ContainerRows, self.CellSize, self.CellSpacing, self.CellPadding);
            RenderGrid(self.ContainerGridRoot, self.ContainerGridCellViews, self.ContainerCols, self.ContainerRows, self.CellSize, self.CellSpacing, self.CellPadding);
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
            RenderGrid(self.BagGridRoot, self.BagGridCellViews, self.BagCols, self.BagRows, self.CellSize, self.CellSpacing, self.CellPadding);
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
            string itemName = config?.Name ?? $"Item({configId})";
            view.name = $"Item_{slotIndex}_{configId}";

            TMP_Text[] tmps = view.GetComponentsInChildren<TMP_Text>(true);
            if (tmps.Length == 1)
            {
                tmps[0].text = $"{itemName}\nX{count}";
            }
            else if (tmps.Length > 1)
            {
                tmps[0].text = itemName;
                tmps[1].text = $"X{count}";
            }

            Text[] texts = view.GetComponentsInChildren<Text>(true);
            if (texts.Length == 1)
            {
                texts[0].text = $"{itemName}\nX{count}";
            }
            else if (texts.Length > 1)
            {
                texts[0].text = itemName;
                texts[1].text = $"X{count}";
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

            float strideX = self.CellSize.x + self.CellSpacing.x;
            float strideY = self.CellSize.y + self.CellSpacing.y;
            int targetX = Mathf.RoundToInt((view.anchoredPosition.x - self.CellPadding.x) / Mathf.Max(1f, strideX));
            int targetY = Mathf.RoundToInt(((-view.anchoredPosition.y) - self.CellPadding.y) / Mathf.Max(1f, strideY));
            targetX = Mathf.Clamp(targetX, 0, Mathf.Max(0, solver.Cols - 1));
            targetY = Mathf.Clamp(targetY, 0, Mathf.Max(0, solver.Rows - 1));

            int maxRadius = Math.Max(solver.Cols, solver.Rows);
            GridDropResult dropResult = solver.ResolveDropNearest(itemId, targetX, targetY, maxRadius, allowSwap: true);
            bool applied = solver.ApplyDropResult(itemId, dropResult);
            if (!applied)
            {
                ApplyFootprint(view, oldFootprint, self.CellSize, self.CellSpacing, self.CellPadding);
                return;
            }

            ApplyFootprintFromSolver(self, isBag, itemId);
            if (dropResult.ResultType == GridDropResultType.Swapped && dropResult.SwapItemId > 0)
            {
                ApplyFootprintFromSolver(self, isBag, dropResult.SwapItemId);
            }

            Log.Info(
                $"[SearchDrag] drop {(isBag ? "bag" : "container")} item={itemId}, result={dropResult.ResultType}, target=({targetX},{targetY})");
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
            Vector2 padding)
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
                        Image image = go.GetComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.08f);
                        image.raycastTarget = false;
                        cellMap[id] = cell;
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
        #endregion YIUIEvent结束
    }
}
