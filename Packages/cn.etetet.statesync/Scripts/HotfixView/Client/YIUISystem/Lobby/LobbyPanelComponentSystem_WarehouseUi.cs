using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private static void InitWarehouseArea(this LobbyPanelComponent self)
        {
            RectTransform boardRoot = self.GetWarehouseBoardRoot();
            RectTransform contentRoot = self.GetWarehouseContentRoot();
            if (boardRoot == null || contentRoot == null)
            {
                return;
            }

            self.BindWarehouseBoardInteract(boardRoot);
            self.ConfigureWarehouseContentRoot(contentRoot);
            self.ResolveWarehousePrefabNodes(contentRoot);
        }

        private static void RefreshWarehouseArea(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            self.InitWarehouseArea();

            bool hasWarehouseItems = loadout != null && loadout.WarehouseItems != null && loadout.WarehouseItems.Count > 0;
            if (!hasWarehouseItems)
            {
                self.SelectedWarehouseConfigId = 0;
                self.SelectedWarehouseItemUid = 0;
            }
            else
            {
                bool configExists = self.SelectedWarehouseConfigId <= 0;
                bool uidExists = self.SelectedWarehouseItemUid <= 0;
                for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
                {
                    LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                    if (item.ConfigId == self.SelectedWarehouseConfigId)
                    {
                        configExists = true;
                    }

                    if (item.ItemUid == self.SelectedWarehouseItemUid)
                    {
                        uidExists = true;
                    }
                }

                if (!configExists)
                {
                    self.SelectedWarehouseConfigId = 0;
                }

                if (!uidExists)
                {
                    self.SelectedWarehouseItemUid = 0;
                }
            }

            if (self.u_DataWarehouseEmptyText != null)
            {
                self.u_DataWarehouseEmptyText.SetValue(hasWarehouseItems ? string.Empty : "仓库为空");
            }

            self.RenderWarehouseArea(loadout);
        }

        private static void RenderWarehouseArea(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            RectTransform boardRoot = self.GetWarehouseBoardRoot();
            RectTransform contentRoot = self.GetWarehouseContentRoot();
            self.InitWarehouseArea();

            if (boardRoot == null || contentRoot == null || self.WarehouseGridRoot == null || self.WarehouseItemsLayer == null || self.WarehouseItemTemplate == null)
            {
                ReleaseViews(self.WarehouseItemViews);
                ReleaseViews(self.WarehouseGridCellViews);
                return;
            }

            int cols = self.GetWarehouseRenderColumnCount(loadout);
            int rows = GetWarehouseRenderRows(loadout, cols);
            float cellEdge = CalcWarehouseCellSize(boardRoot, cols, self.GridSpacing, self.GridPadding);
            self.RefreshWarehouseContentLayout(contentRoot, boardRoot, rows, cellEdge);

            SetTemplateActive(self.WarehouseItemTemplate, false);
            RenderWarehouseGrid(self.WarehouseGridRoot, self.WarehouseGridCellViews, cols, rows, cellEdge, self.GridSpacing, self.GridPadding);

            if (loadout == null || loadout.WarehouseItems == null || loadout.WarehouseItems.Count == 0 || cols <= 0 || rows <= 0)
            {
                ReleaseViews(self.WarehouseItemViews);
                return;
            }

            Vector2 cellSize = new Vector2(cellEdge, cellEdge);
            HashSet<long> alive = new();
            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0 || item.AnchorSlotIndex < 0)
                {
                    continue;
                }

                alive.Add(item.ItemUid);

                RectTransform view = GetOrCreateGridView(self.WarehouseItemViews, self.WarehouseItemTemplate, self.WarehouseItemsLayer, item.ItemUid);
                if (view == null)
                {
                    continue;
                }

                GridItemFootprint footprint = new GridItemFootprint
                {
                    ItemId = item.ItemUid,
                    X = item.AnchorSlotIndex % cols,
                    Y = item.AnchorSlotIndex / cols,
                    Width = Math.Max(1, item.GridWidth),
                    Height = Math.Max(1, item.GridHeight),
                };

                ApplyFootprint(view, footprint, cellSize, self.GridSpacing, self.GridPadding);
                BindWarehouseGridItemView(self, view, new LoadoutWarehouseRenderItemInfo
                {
                    ItemUid = item.ItemUid,
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    GridWidth = Math.Max(1, item.GridWidth),
                    GridHeight = Math.Max(1, item.GridHeight),
                    AnchorSlotIndex = item.AnchorSlotIndex,
                });
            }

            RemoveDeadViews(self.WarehouseItemViews, alive);
        }

        private static void BindWarehouseGridItemView(this LobbyPanelComponent self, RectTransform view, LoadoutWarehouseRenderItemInfo item)
        {
            ResolveDisplayInfo(item.ConfigId, out string name, out string icon, out _);
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(item.ConfigId);
            string itemDesc = !string.IsNullOrWhiteSpace(itemConfig?.Desc) ? itemConfig.Desc : name;
            view.name = $"Warehouse_{item.ItemUid}_{item.ConfigId}";

            LoadoutGridItemViewProxy proxy = view.GetComponent<LoadoutGridItemViewProxy>();
            if (proxy == null)
            {
                proxy = view.gameObject.AddComponent<LoadoutGridItemViewProxy>();
            }

            proxy.PanelRef = self;
            proxy.IsWarehouse = true;
            proxy.ItemUid = item.ItemUid;
            proxy.AreaType = 0;
            proxy.FixedSlotType = 0;
            proxy.AnchorSlotIndex = item.AnchorSlotIndex;
            proxy.ConfigId = item.ConfigId;
            proxy.IconImage = FindBestIconImage(view);
            proxy.TmpTexts ??= view.GetComponentsInChildren<TMP_Text>(true);
            proxy.Texts ??= view.GetComponentsInChildren<Text>(true);

            ApplyOwnedGridItemTexts(proxy, itemDesc, item.Count);
            ItemQualityBgViewHelper.UpdateQualityBgByConfigId(view, item.ConfigId);
            UpdateOwnedGridItemIcon(proxy, icon).Coroutine();
            ApplyWarehouseSelectionVisual(view, self.SelectedWarehouseItemUid == item.ItemUid);
            BindLoadoutGridItemInteract(self, view, proxy);
        }

        private static void BindWarehouseGridItemClick(this LobbyPanelComponent self, RectTransform view, long itemUid, int configId)
        {
            if (view == null)
            {
                return;
            }

            EnsureRaycastGraphic(view);
            EventTrigger trigger = view.GetComponent<EventTrigger>() ?? view.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            trigger.triggers.Clear();

            EntityRef<LobbyPanelComponent> selfRef = self;
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick,
            };
            entry.callback.AddListener(_ =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                panel.OnWarehouseGridItemClicked(itemUid, configId);
            });
            trigger.triggers.Add(entry);
        }

        private static void OnWarehouseGridItemClicked(this LobbyPanelComponent self, long itemUid, int configId)
        {
            self.ClearWarehousePressState();
            if (self.SelectedWarehouseItemUid == itemUid)
            {
                self.SelectedWarehouseItemUid = 0;
                self.SelectedWarehouseConfigId = 0;
            }
            else
            {
                self.SelectedWarehouseItemUid = itemUid;
                self.SelectedWarehouseConfigId = configId;
            }

            self.RefreshWarehouseArea(self.Root()?.GetComponent<LoadoutComponent>());
        }

        private static void ApplyWarehouseSelectionVisual(RectTransform view, bool selected)
        {
            if (view == null)
            {
                return;
            }

            Image background = view.GetComponent<Image>();
            if (background != null)
            {
                background.color = selected ? new Color(1f, 0.88f, 0.35f, 0.95f) : Color.white;
            }
        }

        private static LoopScrollRect GetWarehouseScrollRect(this LobbyPanelComponent self)
        {
            if (self.u_ComWarehouseLoopScroll == null)
            {
                return null;
            }

            LoopScrollRect scrollRect = self.u_ComWarehouseLoopScroll.GetComponent<LoopScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = self.u_ComWarehouseLoopScroll.GetComponentInChildren<LoopScrollRect>(true);
            }

            if (scrollRect == null)
            {
                return null;
            }

            if (scrollRect.viewport == null)
            {
                RectTransform viewport = FindDirectChildRectTransform(self.u_ComWarehouseLoopScroll, "Viewport");
                viewport ??= FindDirectChildRectTransform(scrollRect.transform as RectTransform, "Viewport");
                scrollRect.viewport = viewport;
            }

            if (scrollRect.content == null)
            {
                RectTransform content = null;
                if (scrollRect.viewport != null)
                {
                    content = FindDirectChildRectTransform(scrollRect.viewport, "Content");
                }

                content ??= FindDirectChildRectTransform(scrollRect.transform as RectTransform, "Content");
                content ??= FindDescendantRectTransform(self.u_ComWarehouseLoopScroll, "Content");
                scrollRect.content = content;
            }

            return scrollRect;
        }

        private static RectTransform GetWarehouseBoardRoot(this LobbyPanelComponent self)
        {
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect != null)
            {
                return scrollRect.viewport != null
                    ? scrollRect.viewport
                    : scrollRect.transform as RectTransform ?? self.u_ComWarehouseLoopScroll;
            }

            return self.u_ComWarehouseLoopScroll != null ? self.u_ComWarehouseLoopScroll : self.u_ComWarehouseRoot;
        }

        private static RectTransform GetWarehouseContentRoot(this LobbyPanelComponent self)
        {
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect != null && scrollRect.content != null)
            {
                return scrollRect.content;
            }

            return self.GetWarehouseBoardRoot();
        }

        private static void ConfigureWarehouseContentRoot(this LobbyPanelComponent self, RectTransform contentRoot)
        {
            LoopScrollRect scrollRect = self.GetWarehouseScrollRect();
            if (scrollRect == null || contentRoot == null)
            {
                return;
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.content = contentRoot;
            if (scrollRect.viewport == null)
            {
                scrollRect.viewport = self.GetWarehouseBoardRoot();
            }

            DisableWarehouseAutoLayout(contentRoot);
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(0f, 1f);
            contentRoot.pivot = new Vector2(0f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
        }

        private static void ResolveWarehousePrefabNodes(this LobbyPanelComponent self, RectTransform contentRoot)
        {
            if (contentRoot == null)
            {
                self.WarehouseGridRoot = null;
                self.WarehouseItemsLayer = null;
                self.WarehouseItemTemplate = null;
                return;
            }

            self.WarehouseGridRoot = FindDirectChildRectTransform(contentRoot, "WarehouseGridRoot", "WarhouseGridRoot");
            self.WarehouseItemsLayer = FindDirectChildRectTransform(contentRoot, "WarehouseItemsLayer", "WarhouseItemsLayer");
            self.WarehouseItemTemplate = FindDescendantRectTransform(
                contentRoot,
                "WarehouseItemTemplate",
                "WarhouseItemTemplate",
                "WarhouseITemTemplate");

            if (self.WarehouseGridRoot != null)
            {
                StretchToParent(self.WarehouseGridRoot);
                self.WarehouseGridRoot.SetAsFirstSibling();
            }

            if (self.WarehouseItemsLayer != null)
            {
                StretchToParent(self.WarehouseItemsLayer);
                self.WarehouseItemsLayer.SetAsLastSibling();
            }

            if (self.WarehouseItemTemplate != null)
            {
                NormalizeWarehouseItemTemplate(
                    self.WarehouseItemTemplate,
                    self.u_ComCurrentBagItemTemplate,
                    self.u_ComSecureItemTemplate);
                self.WarehouseItemTemplate.gameObject.SetActive(false);
            }
        }

        private static void DisableWarehouseAutoLayout(RectTransform contentRoot)
        {
            if (contentRoot == null)
            {
                return;
            }

            ContentSizeFitter contentSizeFitter = contentRoot.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter != null && contentSizeFitter.enabled)
            {
                contentSizeFitter.enabled = false;
            }

            LayoutGroup layoutGroup = contentRoot.GetComponent<LayoutGroup>();
            if (layoutGroup != null && layoutGroup.enabled)
            {
                layoutGroup.enabled = false;
            }
        }

        private static int GetWarehouseRenderColumnCount(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            if (loadout != null && loadout.WarehouseColumnCount > 0)
            {
                return loadout.WarehouseColumnCount;
            }

            return self.GetWarehouseSuggestedColumnCount();
        }

        private static int GetWarehouseRequestColumnCount(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            int suggestedColumnCount = self.GetWarehouseSuggestedColumnCount();
            if (loadout == null || loadout.WarehouseColumnCount <= 0)
            {
                return suggestedColumnCount;
            }

            return Math.Max(loadout.WarehouseColumnCount, suggestedColumnCount);
        }

        private static int GetWarehouseSuggestedColumnCount(this LobbyPanelComponent self)
        {
            float width = GetWarehouseRectWidth(self.GetWarehouseBoardRoot());
            float availableWidth = Mathf.Max(1f, width - self.GridPadding.x * 2f);
            float preferredCellSize = Mathf.Max(1f, self.WarehousePreferredCellSize);
            int cols = Mathf.CeilToInt((availableWidth + self.GridSpacing.x) / Mathf.Max(1f, preferredCellSize + self.GridSpacing.x));
            return Mathf.Max(1, cols);
        }

        private static bool TryResolveWarehouseSlot(
            this LobbyPanelComponent self,
            Vector2 screenPosition,
            Camera eventCamera,
            out int slotIndex)
        {
            slotIndex = -1;
            RectTransform boardRoot = self.GetWarehouseBoardRoot();
            RectTransform gridRoot = self.WarehouseGridRoot;
            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            int cols = self.GetWarehouseRenderColumnCount(loadout);
            int rows = GetWarehouseRenderRows(loadout, cols);
            if (boardRoot == null || gridRoot == null || cols <= 0 || rows <= 0)
            {
                return false;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(boardRoot, screenPosition, eventCamera))
            {
                return false;
            }

            float cellEdge = CalcWarehouseCellSize(boardRoot, cols, self.GridSpacing, self.GridPadding);
            return TryResolveLoadoutBoardSlot(
                gridRoot,
                cols,
                rows,
                new Vector2(cellEdge, cellEdge),
                self.GridSpacing,
                self.GridPadding,
                screenPosition,
                eventCamera,
                out slotIndex);
        }

        private static void RefreshWarehouseContentLayout(
            this LobbyPanelComponent self,
            RectTransform contentRoot,
            RectTransform boardRoot,
            int rows,
            float cellEdge)
        {
            if (contentRoot == null || boardRoot == null)
            {
                return;
            }

            rows = Mathf.Max(1, rows);
            float boardWidth = GetWarehouseRectWidth(boardRoot);
            float boardHeight = GetWarehouseRectHeight(boardRoot);
            float requiredHeight = self.GridPadding.y * 2f + rows * cellEdge + Mathf.Max(0, rows - 1) * self.GridSpacing.y;
            float contentHeight = Mathf.Max(boardHeight, requiredHeight);

            contentRoot.sizeDelta = new Vector2(boardWidth, contentHeight);
            if (contentRoot == self.WarehouseGridRoot || contentRoot == self.WarehouseItemsLayer)
            {
                return;
            }

            if (self.WarehouseGridRoot != null)
            {
                self.WarehouseGridRoot.offsetMin = Vector2.zero;
                self.WarehouseGridRoot.offsetMax = Vector2.zero;
            }

            if (self.WarehouseItemsLayer != null)
            {
                self.WarehouseItemsLayer.offsetMin = Vector2.zero;
                self.WarehouseItemsLayer.offsetMax = Vector2.zero;
            }
        }

        private static RectTransform FindDirectChildRectTransform(RectTransform parent, params string[] names)
        {
            if (parent == null || names == null || names.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; ++i)
            {
                if (parent.GetChild(i) is not RectTransform rectTransform)
                {
                    continue;
                }

                for (int j = 0; j < names.Length; ++j)
                {
                    if (rectTransform.name == names[j])
                    {
                        return rectTransform;
                    }
                }
            }

            return null;
        }

        private static RectTransform FindDescendantRectTransform(RectTransform parent, params string[] names)
        {
            if (parent == null || names == null || names.Length == 0)
            {
                return null;
            }

            RectTransform[] rectTransforms = parent.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; ++i)
            {
                RectTransform rectTransform = rectTransforms[i];
                if (rectTransform == parent)
                {
                    continue;
                }

                for (int j = 0; j < names.Length; ++j)
                {
                    if (rectTransform.name == names[j])
                    {
                        return rectTransform;
                    }
                }
            }

            return null;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void RenderWarehouseGrid(
            RectTransform gridRoot,
            Dictionary<int, RectTransform> cellMap,
            int cols,
            int rows,
            float cellEdge,
            Vector2 spacing,
            Vector2 padding)
        {
            if (gridRoot == null)
            {
                return;
            }

            if (cols <= 0 || rows <= 0)
            {
                ReleaseViews(cellMap);
                return;
            }

            Vector2 cellSize = new Vector2(cellEdge, cellEdge);
            HashSet<int> alive = new();
            for (int y = 0; y < rows; ++y)
            {
                for (int x = 0; x < cols; ++x)
                {
                    int id = y * cols + x;
                    alive.Add(id);
                    if (!cellMap.TryGetValue(id, out RectTransform cell) || cell == null)
                    {
                        GameObject go = new GameObject($"WarehouseCell_{x}_{y}", typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(gridRoot, false);
                        cell = go.GetComponent<RectTransform>();
                        cellMap[id] = cell;
                    }

                    Image image = cell.GetComponent<Image>();
                    if (image != null)
                    {
                        ApplyGridCellVisual(image, gridRoot, new Color(1f, 1f, 1f, 0.08f));
                    }

                    ApplyFootprint(
                        cell,
                        new GridItemFootprint
                        {
                            ItemId = 0,
                            X = x,
                            Y = y,
                            Width = 1,
                            Height = 1,
                        },
                        cellSize,
                        spacing,
                        padding);
                }
            }

            RemoveDeadViews(cellMap, alive);
        }

        private static int GetWarehouseRenderRows(LoadoutComponent loadout, int columnCount)
        {
            if (columnCount <= 0)
            {
                return 0;
            }

            int rows = 1;
            int extraRows = 1;
            if (loadout?.WarehouseItems == null)
            {
                return rows;
            }

            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                int gridHeight = Math.Max(1, item.GridHeight);
                extraRows = Math.Max(extraRows, gridHeight);
                if (item.AnchorSlotIndex < 0)
                {
                    continue;
                }

                rows = Math.Max(rows, item.AnchorSlotIndex / columnCount + gridHeight);
            }

            int bufferRows = Math.Max(1, extraRows / 2);
            return rows + bufferRows;
        }

        private static float CalcWarehouseCellSize(RectTransform boardRoot, int cols, Vector2 spacing, Vector2 padding)
        {
            if (boardRoot == null || cols <= 0)
            {
                return LOADOUT_GRID_FALLBACK_CELL;
            }

            float width = GetWarehouseRectWidth(boardRoot);
            float availableWidth = Mathf.Max(1f, width - padding.x * 2f - Mathf.Max(0, cols - 1) * spacing.x);
            return availableWidth / cols;
        }

        private static float GetWarehouseRectWidth(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return LOADOUT_GRID_FALLBACK_CELL;
            }

            float width = rectTransform.rect.width;
            if (width > 0.01f)
            {
                return width;
            }

            width = rectTransform.sizeDelta.x;
            return width > 0.01f ? width : LOADOUT_GRID_FALLBACK_CELL;
        }

        private static float GetWarehouseRectHeight(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return LOADOUT_GRID_FALLBACK_CELL;
            }

            float height = rectTransform.rect.height;
            if (height > 0.01f)
            {
                return height;
            }

            height = rectTransform.sizeDelta.y;
            return height > 0.01f ? height : LOADOUT_GRID_FALLBACK_CELL;
        }

        private static void NormalizeWarehouseItemTemplate(RectTransform template, params RectTransform[] fallbackTemplates)
        {
            if (template == null)
            {
                return;
            }

            Vector2 templateSize = ResolveTemplatePreferredSize(template, fallbackTemplates);
            template.anchorMin = new Vector2(0.5f, 0.5f);
            template.anchorMax = new Vector2(0.5f, 0.5f);
            template.pivot = new Vector2(0.5f, 0.5f);
            template.anchoredPosition = Vector2.zero;
            template.sizeDelta = templateSize;

            LayoutElement layoutElement = template.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = templateSize.x;
                layoutElement.preferredHeight = templateSize.y;
                layoutElement.flexibleWidth = -1f;
                layoutElement.flexibleHeight = -1f;
            }
        }

        private static Vector2 ResolveTemplatePreferredSize(RectTransform template, params RectTransform[] fallbacks)
        {
            return new Vector2(
                ResolveTemplatePreferredWidth(template, fallbacks),
                ResolveTemplatePreferredHeight(template, fallbacks));
        }

        private static float ResolveTemplatePreferredWidth(RectTransform template, params RectTransform[] fallbacks)
        {
            return ResolveTemplatePreferredDimension(true, template, fallbacks);
        }

        private static float ResolveTemplatePreferredHeight(RectTransform template, params RectTransform[] fallbacks)
        {
            return ResolveTemplatePreferredDimension(false, template, fallbacks);
        }

        private static float ResolveTemplatePreferredDimension(bool width, RectTransform template, params RectTransform[] fallbacks)
        {
            float value = TryResolveTemplateDimension(width, template);
            if (value > 1f)
            {
                return value;
            }

            if (fallbacks != null)
            {
                for (int i = 0; i < fallbacks.Length; ++i)
                {
                    value = TryResolveTemplateDimension(width, fallbacks[i]);
                    if (value > 1f)
                    {
                        return value;
                    }
                }
            }

            return LOADOUT_GRID_FALLBACK_CELL;
        }

        private static float TryResolveTemplateDimension(bool width, RectTransform template)
        {
            if (template == null)
            {
                return 0f;
            }

            LayoutElement layoutElement = template.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                float preferred = width ? layoutElement.preferredWidth : layoutElement.preferredHeight;
                if (preferred > 1f)
                {
                    return preferred;
                }
            }

            bool fixedAnchor = width
                ? Mathf.Abs(template.anchorMax.x - template.anchorMin.x) < 0.001f
                : Mathf.Abs(template.anchorMax.y - template.anchorMin.y) < 0.001f;
            if (!fixedAnchor)
            {
                return 0f;
            }

            float rectSize = width ? template.rect.width : template.rect.height;
            if (rectSize > 1f)
            {
                return rectSize;
            }

            float sizeDelta = width ? template.sizeDelta.x : template.sizeDelta.y;
            return sizeDelta > 1f ? sizeDelta : 0f;
        }
    }
}
