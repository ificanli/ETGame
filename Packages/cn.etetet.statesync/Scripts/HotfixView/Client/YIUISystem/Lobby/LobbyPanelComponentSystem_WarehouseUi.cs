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
            if (boardRoot == null)
            {
                return;
            }

            self.WarehouseGridRoot ??= CreateStretchChild(boardRoot, "WarehouseGridRoot");
            self.WarehouseItemsLayer ??= CreateStretchChild(boardRoot, "WarehouseItemsLayer");

            if (self.WarehouseItemTemplate == null)
            {
                RectTransform templateSource = self.u_ComCurrentBagItemTemplate != null
                    ? self.u_ComCurrentBagItemTemplate
                    : self.u_ComSecureItemTemplate;
                if (templateSource != null && self.WarehouseItemsLayer != null)
                {
                    self.WarehouseItemTemplate = UnityEngine.Object.Instantiate(templateSource, self.WarehouseItemsLayer);
                    self.WarehouseItemTemplate.name = "WarehouseItemTemplate";
                    self.WarehouseItemTemplate.gameObject.SetActive(false);
                }
            }
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
            self.InitWarehouseArea();

            if (boardRoot == null || self.WarehouseGridRoot == null || self.WarehouseItemsLayer == null || self.WarehouseItemTemplate == null)
            {
                ReleaseViews(self.WarehouseItemViews);
                ReleaseViews(self.WarehouseGridCellViews);
                return;
            }

            SetTemplateActive(self.WarehouseItemTemplate, false);

            if (loadout == null || loadout.WarehouseItems == null || loadout.WarehouseItems.Count == 0)
            {
                RenderGrid(self.WarehouseGridRoot, self.WarehouseGridCellViews, 0, 0, boardRoot, self.GridSpacing, self.GridPadding, false);
                ReleaseViews(self.WarehouseItemViews);
                return;
            }

            List<LoadoutWarehouseRenderItemInfo> packedItems = BuildWarehouseRenderItems(loadout.WarehouseItems, boardRoot, out int cols, out int rows);
            RenderGrid(self.WarehouseGridRoot, self.WarehouseGridCellViews, cols, rows, boardRoot, self.GridSpacing, self.GridPadding, false);
            if (cols <= 0 || rows <= 0)
            {
                ReleaseViews(self.WarehouseItemViews);
                return;
            }

            Vector2 cellSize = CalcGridCellSize(boardRoot, cols, rows, self.GridSpacing, self.GridPadding);
            HashSet<long> alive = new();
            for (int i = 0; i < packedItems.Count; ++i)
            {
                LoadoutWarehouseRenderItemInfo item = packedItems[i];
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
                BindWarehouseGridItemView(self, view, item);
            }

            RemoveDeadViews(self.WarehouseItemViews, alive);
        }

        private static void BindWarehouseGridItemView(this LobbyPanelComponent self, RectTransform view, LoadoutWarehouseRenderItemInfo item)
        {
            ResolveDisplayInfo(item.ConfigId, out string name, out string icon, out _);
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
            proxy.IconImage ??= FindBestIconImage(view);
            proxy.TmpTexts ??= view.GetComponentsInChildren<TMP_Text>(true);
            proxy.Texts ??= view.GetComponentsInChildren<Text>(true);

            ApplyOwnedGridItemTexts(proxy, name, item.Count);
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

        private static RectTransform GetWarehouseBoardRoot(this LobbyPanelComponent self)
        {
            return self.u_ComWarehouseLoopScroll != null ? self.u_ComWarehouseLoopScroll : self.u_ComWarehouseRoot;
        }

        private static RectTransform CreateStretchChild(RectTransform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return rectTransform;
        }

        private static List<LoadoutWarehouseRenderItemInfo> BuildWarehouseRenderItems(
            List<LoadoutWarehouseItemInfo> source,
            RectTransform boardRoot,
            out int cols,
            out int rows)
        {
            cols = 0;
            rows = 0;
            List<LoadoutWarehouseRenderItemInfo> result = new();
            if (source == null || source.Count == 0)
            {
                return result;
            }

            int totalArea = 0;
            int maxWidth = 1;
            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = source[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                int gridWidth = Math.Max(1, item.GridWidth);
                int gridHeight = Math.Max(1, item.GridHeight);
                totalArea += gridWidth * gridHeight;
                if (gridWidth > maxWidth)
                {
                    maxWidth = gridWidth;
                }
            }

            if (totalArea <= 0)
            {
                return result;
            }

            float aspect = 1f;
            if (boardRoot != null && boardRoot.rect.height > 0.01f)
            {
                aspect = Mathf.Max(0.05f, boardRoot.rect.width / boardRoot.rect.height);
            }

            cols = Math.Max(maxWidth, Mathf.CeilToInt(Mathf.Sqrt(totalArea * aspect)));
            List<GridPlacementItemInfo> placements = new();
            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = source[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                int gridWidth = Math.Max(1, item.GridWidth);
                int gridHeight = Math.Max(1, item.GridHeight);
                int anchorSlotIndex = FindWarehouseAnchorSlot(placements, cols, rows, gridWidth, gridHeight, out int candidateRows);

                placements.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = anchorSlotIndex,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                });

                rows = Math.Max(rows, candidateRows);
                result.Add(new LoadoutWarehouseRenderItemInfo
                {
                    ItemUid = item.ItemUid,
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                    AnchorSlotIndex = anchorSlotIndex,
                });
            }

            return result;
        }

        private static int FindWarehouseAnchorSlot(
            List<GridPlacementItemInfo> placements,
            int cols,
            int currentRows,
            int gridWidth,
            int gridHeight,
            out int candidateRows)
        {
            candidateRows = Math.Max(1, currentRows);
            for (int anchorSlotIndex = 0;; ++anchorSlotIndex)
            {
                int anchorX = anchorSlotIndex % cols;
                if (anchorX + gridWidth > cols)
                {
                    continue;
                }

                candidateRows = Math.Max(currentRows, anchorSlotIndex / cols + gridHeight);
                GridPlacementItemInfo candidate = new GridPlacementItemInfo
                {
                    ConfigId = 1,
                    Count = 1,
                    AnchorSlotIndex = anchorSlotIndex,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                };

                placements.Add(candidate);
                bool valid = LoadoutGridPlacementHelper.ArePlacementsValid(placements, cols, candidateRows);
                placements.RemoveAt(placements.Count - 1);
                if (valid)
                {
                    return anchorSlotIndex;
                }
            }
        }
    }
}
