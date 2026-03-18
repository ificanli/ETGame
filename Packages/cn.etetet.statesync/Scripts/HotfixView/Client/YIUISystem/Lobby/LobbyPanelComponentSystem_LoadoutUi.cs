using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private const float LOADOUT_GRID_FALLBACK_CELL = 96f;

        [EntitySystem]
        private static void LateUpdate(this LobbyPanelComponent self)
        {
            self.TryRefreshLoadoutUi(false);
        }

        private static void TryRefreshLoadoutUi(this LobbyPanelComponent self, bool force)
        {
            LoadoutComponent loadout = self.Root()?.GetComponent<LoadoutComponent>();
            string snapshot = BuildLoadoutSnapshot(loadout);
            if (!force && self.LastLoadoutSnapshot == snapshot)
            {
                return;
            }

            self.LastLoadoutSnapshot = snapshot;
            self.RefreshLoadoutView();
        }

        private static string BuildLoadoutSnapshot(LoadoutComponent loadout)
        {
            if (loadout == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(256);
            builder.Append(loadout.SelectedHeroConfigId).Append('|');
            builder.Append(loadout.MainWeaponConfigId).Append('|');
            builder.Append(loadout.SubWeaponConfigId).Append('|');
            builder.Append(loadout.ArmorConfigId).Append('|');
            builder.Append(loadout.BackpackConfigId).Append('|');
            builder.Append(loadout.BagWidth).Append('x').Append(loadout.BagHeight).Append('|');
            builder.Append(loadout.SecureWidth).Append('x').Append(loadout.SecureHeight).Append('|');
            builder.Append(loadout.TotalWealth).Append('|');
            builder.Append(loadout.IsConfirmed ? 1 : 0).Append('|');
            builder.Append(loadout.ConfirmedAt).Append('|');
            builder.Append(loadout.WarehouseColumnCount).Append('|');

            foreach ((int configId, int count) in loadout.StorageItemCounts)
            {
                builder.Append(configId).Append(':').Append(count).Append('|');
            }

            AppendGridItems(builder, loadout.CarriedBagItems);
            AppendGridItems(builder, loadout.CarriedSecureItems);
            AppendWarehouseItems(builder, loadout.WarehouseItems);
            return builder.ToString();
        }

        private static void AppendGridItems(StringBuilder builder, List<LoadoutGridItemInfo> items)
        {
            builder.Append("grid(").Append(items?.Count ?? 0).Append(')');
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                LoadoutGridItemInfo item = items[i];
                builder.Append('|')
                        .Append(item.ConfigId)
                        .Append(':')
                        .Append(item.Count)
                        .Append(':')
                        .Append(item.AnchorSlotIndex)
                        .Append(':')
                        .Append(item.GridWidth)
                        .Append('x')
                        .Append(item.GridHeight);
            }
        }

        private static void AppendWarehouseItems(StringBuilder builder, List<LoadoutWarehouseItemInfo> items)
        {
            builder.Append("warehouse(").Append(items?.Count ?? 0).Append(')');
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = items[i];
                builder.Append('|')
                        .Append(item.ItemUid)
                        .Append(':')
                        .Append(item.ConfigId)
                        .Append(':')
                        .Append(item.Count)
                        .Append(':')
                        .Append(item.AnchorSlotIndex)
                        .Append(':')
                        .Append(item.GridWidth)
                        .Append('x')
                        .Append(item.GridHeight);
            }
        }

        private static List<LoadoutWarehouseItemViewData> BuildWarehouseItemList(LoadoutComponent loadout)
        {
            List<LoadoutWarehouseItemViewData> result = new();
            if (loadout == null)
            {
                return result;
            }

            Dictionary<int, int> countMap = new();
            for (int i = 0; i < loadout.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = loadout.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                if (countMap.TryGetValue(item.ConfigId, out int existed))
                {
                    countMap[item.ConfigId] = existed + item.Count;
                }
                else
                {
                    countMap[item.ConfigId] = item.Count;
                }
            }

            foreach ((int configId, int count) in countMap)
            {
                ResolveDisplayInfo(configId, out string name, out string icon, out int sortCategory);
                result.Add(new LoadoutWarehouseItemViewData
                {
                    ConfigId = configId,
                    Count = count,
                    Name = name,
                    Icon = icon,
                    SortCategory = sortCategory,
                });
            }

            result.Sort(static (a, b) =>
            {
                int categoryCompare = a.SortCategory.CompareTo(b.SortCategory);
                if (categoryCompare != 0)
                {
                    return categoryCompare;
                }

                int nameCompare = string.CompareOrdinal(a.Name, b.Name);
                if (nameCompare != 0)
                {
                    return nameCompare;
                }

                return a.ConfigId.CompareTo(b.ConfigId);
            });
            return result;
        }

        [EntitySystem]
        private static void YIUILoopRenderer(
            this LobbyPanelComponent self,
            EquipSelectItemComponent item,
            LoadoutWarehouseItemViewData data,
            int index,
            bool select)
        {
            item.u_DataEquipName.SetValue($"{data.Name} x{data.Count}");
            item.SetSelected(self.SelectedWarehouseConfigId == data.ConfigId);
            item.SetItemIcon(data.Icon);
        }

        [EntitySystem]
        private static void YIUILoopOnClick(
            this LobbyPanelComponent self,
            EquipSelectItemComponent item,
            LoadoutWarehouseItemViewData data,
            int index,
            bool select)
        {
            self.SelectedWarehouseConfigId = self.SelectedWarehouseConfigId == data.ConfigId ? 0 : data.ConfigId;
            self.SelectedWarehouseItemUid = 0;
            self.RefreshWarehouseArea(self.Root()?.GetComponent<LoadoutComponent>());
        }

        private static void RefreshLoadoutExtraUi(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            if (self.u_DataTotalWealthText != null)
            {
                self.u_DataTotalWealthText.SetValue(loadout != null ? loadout.TotalWealth.ToString() : "0");
            }

            self.RefreshWarehouseArea(loadout);

            if (loadout == null)
            {
                self.RenderOwnedGridArea(
                    self.u_ComCurrentBagBoardRoot,
                    self.u_ComCurrentBagItemsLayer,
                    self.u_ComCurrentBagGridRoot,
                    self.u_ComCurrentBagItemTemplate,
                    null,
                    0,
                    0,
                    self.CurrentBagItemViews,
                    self.CurrentBagGridCellViews,
                    false,
                    LoadoutAreaType.Bag);
                self.RenderOwnedGridArea(
                    self.u_ComSecureBoardRoot,
                    self.u_ComSecureItemsLayer,
                    self.u_ComSecureGridRoot,
                    self.u_ComSecureItemTemplate,
                    null,
                    0,
                    0,
                    self.SecureItemViews,
                    self.SecureGridCellViews,
                    true,
                    LoadoutAreaType.Secure);
                self.RenderWarehouseArea(null);
                return;
            }

            self.RenderOwnedGridArea(
                self.u_ComCurrentBagBoardRoot,
                self.u_ComCurrentBagItemsLayer,
                self.u_ComCurrentBagGridRoot,
                self.u_ComCurrentBagItemTemplate,
                loadout.CarriedBagItems,
                loadout.BagWidth,
                loadout.BagHeight,
                self.CurrentBagItemViews,
                self.CurrentBagGridCellViews,
                false,
                LoadoutAreaType.Bag);
            self.RenderOwnedGridArea(
                self.u_ComSecureBoardRoot,
                self.u_ComSecureItemsLayer,
                self.u_ComSecureGridRoot,
                self.u_ComSecureItemTemplate,
                loadout.CarriedSecureItems,
                loadout.SecureWidth,
                loadout.SecureHeight,
                self.SecureItemViews,
                self.SecureGridCellViews,
                true,
                LoadoutAreaType.Secure);
            self.RenderWarehouseArea(loadout);
        }

        private static void RenderOwnedGridArea(
            this LobbyPanelComponent self,
            RectTransform boardRoot,
            RectTransform itemsLayer,
            RectTransform gridRoot,
            RectTransform template,
            List<LoadoutGridItemInfo> items,
            int cols,
            int rows,
            Dictionary<long, RectTransform> itemViews,
            Dictionary<int, RectTransform> gridCellViews,
            bool isSecure,
            LoadoutAreaType areaType)
        {
            EnsureBoardReceiver(boardRoot);
            SetTemplateActive(template, false);

            RenderGrid(gridRoot, gridCellViews, cols, rows, boardRoot, self.GridSpacing, self.GridPadding, isSecure);

            if (cols <= 0 || rows <= 0 || boardRoot == null || itemsLayer == null || template == null)
            {
                ReleaseViews(itemViews);
                return;
            }

            Vector2 cellSize = CalcGridCellSize(boardRoot, cols, rows, self.GridSpacing, self.GridPadding);
            HashSet<long> alive = new();
            for (int i = 0; i < items.Count; ++i)
            {
                LoadoutGridItemInfo item = items[i];
                long viewId = ComposeGridItemViewId(areaType, item.AnchorSlotIndex);
                alive.Add(viewId);

                RectTransform view = GetOrCreateGridView(itemViews, template, itemsLayer, viewId);
                if (view == null)
                {
                    continue;
                }

                GridItemFootprint footprint = new GridItemFootprint
                {
                    ItemId = viewId,
                    X = item.AnchorSlotIndex % cols,
                    Y = item.AnchorSlotIndex / cols,
                    Width = Math.Max(1, item.GridWidth),
                    Height = Math.Max(1, item.GridHeight),
                };

                ApplyFootprint(view, footprint, cellSize, self.GridSpacing, self.GridPadding);
                BindOwnedGridItemView(self, view, areaType, item);
            }

            RemoveDeadViews(itemViews, alive);
        }

        private static RectTransform GetOrCreateGridView(
            Dictionary<long, RectTransform> map,
            RectTransform template,
            RectTransform layer,
            long viewId)
        {
            if (map.TryGetValue(viewId, out RectTransform view) && view != null)
            {
                view.gameObject.SetActive(true);
                return view;
            }

            if (template == null || layer == null)
            {
                return null;
            }

            view = UnityEngine.Object.Instantiate(template, layer);
            view.gameObject.SetActive(true);
            map[viewId] = view;
            return view;
        }

        private static void BindOwnedGridItemView(
            this LobbyPanelComponent self,
            RectTransform view,
            LoadoutAreaType areaType,
            LoadoutGridItemInfo item)
        {
            ResolveDisplayInfo(item.ConfigId, out string name, out string icon, out _);
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(item.ConfigId);
            string itemDesc = !string.IsNullOrWhiteSpace(itemConfig?.Desc) ? itemConfig.Desc : name;
            view.name = $"{areaType}_{item.AnchorSlotIndex}_{item.ConfigId}";

            LoadoutGridItemViewProxy proxy = view.GetComponent<LoadoutGridItemViewProxy>();
            if (proxy == null)
            {
                proxy = view.gameObject.AddComponent<LoadoutGridItemViewProxy>();
            }

            proxy.PanelRef = self;
            proxy.IsWarehouse = false;
            proxy.ItemUid = 0;
            proxy.AreaType = (int)areaType;
            proxy.FixedSlotType = 0;
            proxy.AnchorSlotIndex = item.AnchorSlotIndex;
            proxy.ConfigId = item.ConfigId;
            proxy.IconImage = FindBestIconImage(view);
            proxy.TmpTexts ??= view.GetComponentsInChildren<TMP_Text>(true);
            proxy.Texts ??= view.GetComponentsInChildren<Text>(true);

            ApplyOwnedGridItemTexts(proxy, itemDesc, item.Count);
            ItemQualityBgViewHelper.UpdateQualityBgByConfigId(view, item.ConfigId);
            UpdateOwnedGridItemIcon(proxy, icon).Coroutine();
            BindLoadoutGridItemInteract(self, view, proxy);
        }

        private static void ApplyOwnedGridItemTexts(LoadoutGridItemViewProxy proxy, string text, int count)
        {
            _ = count;

            if (proxy.TmpTexts != null && proxy.TmpTexts.Length > 0)
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

            if (proxy.Texts != null && proxy.Texts.Length > 0)
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

        private static async ETTask UpdateOwnedGridItemIcon(LoadoutGridItemViewProxy proxy, string iconName)
        {
            if (proxy == null || proxy.IconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconName))
            {
                proxy.IconImage.enabled = false;
                return;
            }

            if (proxy.LoadedSprite != null && proxy.LoadedIconName == iconName)
            {
                proxy.IconImage.sprite = proxy.LoadedSprite;
                proxy.IconImage.enabled = true;
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

            if (proxy.LoadedSprite != null && proxy.LoadedSprite != sprite)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = proxy.LoadedSprite });
            }

            proxy.LoadedSprite = sprite;
            proxy.LoadedIconName = iconName;
            proxy.IconImage.sprite = sprite;
            proxy.IconImage.enabled = sprite != null;
            proxy.IconImage.preserveAspect = true;
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

                if (string.Equals(current.name, "QualityBg", StringComparison.OrdinalIgnoreCase))
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

            return fallback ?? view.GetComponent<Image>();
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

        private static void BindGridItemClick(
            this LobbyPanelComponent self,
            RectTransform view,
            LoadoutAreaType areaType,
            int anchorSlotIndex)
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

                panel.HandleOwnedGridItemClickAsync(areaType, anchorSlotIndex).Coroutine();
            });
            trigger.triggers.Add(entry);
        }

        private static async ETTask HandleOwnedGridItemClickAsync(
            this LobbyPanelComponent self,
            LoadoutAreaType areaType,
            int anchorSlotIndex)
        {
            C2G_LoadoutPutToWarehouse request = C2G_LoadoutPutToWarehouse.Create();
            request.SourceAreaType = (int)areaType;
            request.SourceSlotType = (int)LoadoutFixedSlotType.None;
            request.SourceAnchorSlotIndex = anchorSlotIndex;
            request.Count = 1;
            request.WarehouseColumnCount = self.GetWarehouseRequestColumnCount(self.Root()?.GetComponent<LoadoutComponent>());

            G2C_LoadoutPutToWarehouse response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_LoadoutPutToWarehouse;
            if (response == null || response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[LoadoutUI] PutToWarehouse failed: area={areaType}, anchor={anchorSlotIndex}, error={response?.Error}, message={response?.Message}");
            }
        }

        private static void BindOwnedAreaBoard(this LobbyPanelComponent self, RectTransform boardRoot, LoadoutAreaType areaType)
        {
            if (boardRoot == null)
            {
                return;
            }

            EnsureRaycastGraphic(boardRoot);
            EventTrigger trigger = boardRoot.GetComponent<EventTrigger>() ?? boardRoot.gameObject.AddComponent<EventTrigger>();
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

                panel.HandleOwnedAreaBoardClickAsync(areaType).Coroutine();
            });
            trigger.triggers.Add(entry);
        }

        private static async ETTask HandleOwnedAreaBoardClickAsync(this LobbyPanelComponent self, LoadoutAreaType areaType)
        {
            EntityRef<LobbyPanelComponent> selfRef = self;
            if (await self.TryTakeSelectedWarehouseToAreaAsync(areaType))
            {
                return;
            }

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (areaType == LoadoutAreaType.Bag)
            {
                await self.OpenEquipSelectView(EquipSlotType.BagContent);
            }
        }

        private static void EnsureBoardReceiver(RectTransform boardRoot)
        {
            if (boardRoot == null)
            {
                return;
            }

            EnsureRaycastGraphic(boardRoot);
        }

        private static void EnsureRaycastGraphic(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            Graphic graphic = rectTransform.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = rectTransform.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.001f);
                image.raycastTarget = true;
                return;
            }

            graphic.raycastTarget = true;
        }

        private static void RenderGrid(
            RectTransform gridRoot,
            Dictionary<int, RectTransform> cellMap,
            int cols,
            int rows,
            RectTransform boardRoot,
            Vector2 spacing,
            Vector2 padding,
            bool isSecure)
        {
            if (gridRoot == null || boardRoot == null)
            {
                return;
            }

            if (cols <= 0 || rows <= 0)
            {
                ReleaseViews(cellMap);
                return;
            }

            Vector2 cellSize = CalcGridCellSize(boardRoot, cols, rows, spacing, padding);
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

                    Image image = cell.GetComponent<Image>();
                    if (image != null)
                    {
                        ApplyGridCellVisual(
                            image,
                            gridRoot,
                            isSecure ? new Color(0.25f, 0.65f, 0.95f, 0.18f) : new Color(1f, 1f, 1f, 0.08f));
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
                styleSource.enabled = false;
                styleSource.raycastTarget = false;
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
            if (styleImage != null)
            {
                return styleImage;
            }

            return gridRoot.GetComponent<Image>();
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
            targetImage.color = sourceImage.color;
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

        private static Vector2 CalcGridCellSize(RectTransform boardRoot, int cols, int rows, Vector2 spacing, Vector2 padding)
        {
            if (boardRoot == null || cols <= 0 || rows <= 0)
            {
                return new Vector2(LOADOUT_GRID_FALLBACK_CELL, LOADOUT_GRID_FALLBACK_CELL);
            }

            float width = boardRoot.rect.width;
            float height = boardRoot.rect.height;
            if (width <= 0f || height <= 0f)
            {
                return new Vector2(LOADOUT_GRID_FALLBACK_CELL, LOADOUT_GRID_FALLBACK_CELL);
            }

            float availableWidth = Mathf.Max(1f, width - padding.x * 2f - Mathf.Max(0, cols - 1) * spacing.x);
            float availableHeight = Mathf.Max(1f, height - padding.y * 2f - Mathf.Max(0, rows - 1) * spacing.y);
            return new Vector2(availableWidth / cols, availableHeight / rows);
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

            float width = footprint.Width * cellSize.x + Mathf.Max(0, footprint.Width - 1) * spacing.x;
            float height = footprint.Height * cellSize.y + Mathf.Max(0, footprint.Height - 1) * spacing.y;
            float posX = padding.x + footprint.X * (cellSize.x + spacing.x);
            float posY = -padding.y - footprint.Y * (cellSize.y + spacing.y);

            view.sizeDelta = new Vector2(width, height);
            view.anchoredPosition = new Vector2(posX, posY);
        }

        private static void RemoveDeadViews(Dictionary<long, RectTransform> map, HashSet<long> alive)
        {
            List<long> removeIds = null;
            foreach ((long id, RectTransform view) in map)
            {
                if (alive.Contains(id))
                {
                    continue;
                }

                if (view != null)
                {
                    UnityEngine.Object.Destroy(view.gameObject);
                }

                removeIds ??= new List<long>();
                removeIds.Add(id);
            }

            if (removeIds == null)
            {
                return;
            }

            for (int i = 0; i < removeIds.Count; ++i)
            {
                map.Remove(removeIds[i]);
            }
        }

        private static void RemoveDeadViews(Dictionary<int, RectTransform> map, HashSet<int> alive)
        {
            List<int> removeIds = null;
            foreach ((int id, RectTransform view) in map)
            {
                if (alive.Contains(id))
                {
                    continue;
                }

                if (view != null)
                {
                    UnityEngine.Object.Destroy(view.gameObject);
                }

                removeIds ??= new List<int>();
                removeIds.Add(id);
            }

            if (removeIds == null)
            {
                return;
            }

            for (int i = 0; i < removeIds.Count; ++i)
            {
                map.Remove(removeIds[i]);
            }
        }

        private static void ReleaseViews(Dictionary<long, RectTransform> map)
        {
            foreach ((_, RectTransform view) in map)
            {
                if (view != null)
                {
                    UnityEngine.Object.Destroy(view.gameObject);
                }
            }

            map.Clear();
        }

        private static void ReleaseViews(Dictionary<int, RectTransform> map)
        {
            foreach ((_, RectTransform view) in map)
            {
                if (view != null)
                {
                    UnityEngine.Object.Destroy(view.gameObject);
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

        private static long ComposeGridItemViewId(LoadoutAreaType areaType, int anchorSlotIndex)
        {
            return ((long)areaType << 32) | (uint)Math.Max(anchorSlotIndex, 0);
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
    }
}
