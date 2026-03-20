using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(LobbyPanelComponent))]
    public static partial class LobbyPanelComponentSystem
    {
        private const string LOADOUT_SOURCE_TOGGLE_ROOT_NAME = "LoadoutSourceToggleRoot";
        private const string LOADOUT_SOURCE_SHOP_BUTTON_NAME = "LoadoutShopButton";
        private const string LOADOUT_SOURCE_WAREHOUSE_BUTTON_NAME = "LoadoutWarehouseButton";

        private static void RefreshLoadoutSourceUi(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            self.EnsureLoadoutSourceToggleUi();
            self.RefreshLoadoutSourceToggleVisual();
            self.RefreshLoadoutSourceListAsync(loadout).Coroutine();
        }
        
        private static async ETTask RefreshLoadoutSourceListAsync(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            if (self.EquipBagLoop == null)
            {
                return;
            }

            List<LoadoutWarehouseItemViewData> sourceItems = self.BuildLoadoutSourceItemList(loadout);
            int selectedIndex = self.ResolveSelectedLoadoutSourceIndex(sourceItems);
            await self.EquipBagLoop.SetDataRefresh(sourceItems, selectedIndex);
        }

        private static List<LoadoutWarehouseItemViewData> BuildLoadoutSourceItemList(this LobbyPanelComponent self, LoadoutComponent loadout)
        {
            return self.BuildLoadoutSourceItemList(loadout, self.CurrentItemSourceMode);
        }

        private static List<LoadoutWarehouseItemViewData> BuildLoadoutSourceItemList(
            this LobbyPanelComponent self,
            LoadoutComponent loadout,
            LoadoutItemSourceMode sourceMode)
        {
            return sourceMode == LoadoutItemSourceMode.Shop
                ? BuildShopItemList(loadout)
                : BuildWarehouseItemList(loadout);
        }

        private static List<LoadoutWarehouseItemViewData> BuildShopItemList(LoadoutComponent loadout)
        {
            List<LoadoutWarehouseItemViewData> result = new();
            long wealth = loadout?.TotalWealth ?? 0;

            foreach (ItemConfig itemConfig in ItemConfigCategory.Instance.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0 || !itemConfig.LoadoutShopVisible)
                {
                    continue;
                }

                result.Add(new LoadoutWarehouseItemViewData
                {
                    ConfigId = itemConfig.Id,
                    Count = 0,
                    Name = itemConfig.Name,
                    Icon = itemConfig.Icon,
                    SortCategory = itemConfig.LoadoutShopCategory,
                    Price = itemConfig.LoadoutBuyPrice,
                    Affordable = wealth >= itemConfig.LoadoutBuyPrice,
                    SourceMode = LoadoutItemSourceMode.Shop,
                });
            }

            SortLoadoutSourceItems(result);
            return result;
        }

        private static void SortLoadoutSourceItems(List<LoadoutWarehouseItemViewData> result)
        {
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
        }

        private static int ResolveSelectedLoadoutSourceIndex(this LobbyPanelComponent self, List<LoadoutWarehouseItemViewData> sourceItems)
        {
            if (sourceItems == null || sourceItems.Count == 0)
            {
                if (self.CurrentItemSourceMode == LoadoutItemSourceMode.Shop)
                {
                    self.SelectedShopConfigId = 0;
                }
                else
                {
                    self.SelectedWarehouseConfigId = 0;
                }

                return 0;
            }

            int selectedConfigId = self.CurrentItemSourceMode == LoadoutItemSourceMode.Shop
                ? self.SelectedShopConfigId
                : self.SelectedWarehouseConfigId;
            int selectedIndex = -1;
            for (int i = 0; i < sourceItems.Count; ++i)
            {
                if (sourceItems[i].ConfigId == selectedConfigId)
                {
                    selectedIndex = i;
                    break;
                }
            }
            if (selectedIndex >= 0)
            {
                return selectedIndex;
            }

            LoadoutWarehouseItemViewData firstItem = sourceItems[0];
            if (firstItem.SourceMode == LoadoutItemSourceMode.Shop)
            {
                self.SelectedShopConfigId = firstItem.ConfigId;
            }
            else
            {
                self.SelectedWarehouseConfigId = firstItem.ConfigId;
            }

            return 0;
        }

        private static string FormatLoadoutSourceItemText(LoadoutWarehouseItemViewData data)
        {
            if (data.SourceMode == LoadoutItemSourceMode.Shop)
            {
                return data.Affordable
                    ? $"{data.Name} ￥{data.Price}"
                    : $"{data.Name} ￥{data.Price} [不足]";
            }

            return $"{data.Name} x{data.Count}";
        }

        private static bool IsLoadoutSourceItemSelected(this LobbyPanelComponent self, LoadoutWarehouseItemViewData data)
        {
            return data.SourceMode switch
            {
                LoadoutItemSourceMode.Shop => self.CurrentItemSourceMode == LoadoutItemSourceMode.Shop && self.SelectedShopConfigId == data.ConfigId,
                _ => self.CurrentItemSourceMode == LoadoutItemSourceMode.Warehouse && self.SelectedWarehouseConfigId == data.ConfigId,
            };
        }

        private static void SelectLoadoutSourceMode(this LobbyPanelComponent self, LoadoutItemSourceMode sourceMode)
        {
            if (self == null || self.IsDisposed || self.CurrentItemSourceMode == sourceMode)
            {
                return;
            }

            self.CurrentItemSourceMode = sourceMode;
            self.RefreshLoadoutView();
        }

        private static void EnsureLoadoutSourceToggleUi(this LobbyPanelComponent self)
        {
            if (self.LoadoutShopButton != null && self.LoadoutWarehouseButton != null && self.LoadoutSourceToggleRoot != null)
            {
                self.BindLoadoutSourceToggleButtons();
                return;
            }

            RectTransform root = FindDescendantRectTransform(self.u_ComEquipPanelRectTransform, LOADOUT_SOURCE_TOGGLE_ROOT_NAME);
            if (root == null)
            {
                root = self.CreateFallbackLoadoutSourceToggleUi();
            }

            if (root == null)
            {
                return;
            }

            self.LoadoutSourceToggleRoot = root;
            RectTransform shopButtonRect = FindDescendantRectTransform(root, LOADOUT_SOURCE_SHOP_BUTTON_NAME);
            RectTransform warehouseButtonRect = FindDescendantRectTransform(root, LOADOUT_SOURCE_WAREHOUSE_BUTTON_NAME);
            if (shopButtonRect == null || warehouseButtonRect == null)
            {
                return;
            }

            self.LoadoutShopButton = shopButtonRect.GetComponent<Button>();
            self.LoadoutWarehouseButton = warehouseButtonRect.GetComponent<Button>();
            self.LoadoutShopButtonGraphic = shopButtonRect.GetComponent<Graphic>();
            self.LoadoutWarehouseButtonGraphic = warehouseButtonRect.GetComponent<Graphic>();
            self.LoadoutShopButtonText = shopButtonRect.GetComponentInChildren<TMP_Text>(true);
            self.LoadoutWarehouseButtonText = warehouseButtonRect.GetComponentInChildren<TMP_Text>(true);
            self.BindLoadoutSourceToggleButtons();
        }

        private static RectTransform CreateFallbackLoadoutSourceToggleUi(this LobbyPanelComponent self)
        {

            GameObject rootGo = new GameObject(LOADOUT_SOURCE_TOGGLE_ROOT_NAME, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(8f, -8f);
            root.sizeDelta = new Vector2(208f, 36f);

            Image rootImage = rootGo.GetComponent<Image>();
            rootImage.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);
            rootImage.raycastTarget = true;

            CreateFallbackLoadoutSourceButton(self, root, LOADOUT_SOURCE_SHOP_BUTTON_NAME, "商店", new Vector2(0f, 0f));
            CreateFallbackLoadoutSourceButton(self, root, LOADOUT_SOURCE_WAREHOUSE_BUTTON_NAME, "仓库", new Vector2(104f, 0f));
            return root;
        }

        private static void CreateFallbackLoadoutSourceButton(
            this LobbyPanelComponent self,
            RectTransform parent,
            string buttonName,
            string label,
            Vector2 anchoredPosition)
        {
            GameObject buttonGo = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(96f, 36f);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.23f, 0.23f, 0.23f, 1f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            TMP_Text templateText = self.u_ComEquipPanelRectTransform != null
                ? self.u_ComEquipPanelRectTransform.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (templateText != null)
            {
                text.font = templateText.font;
                text.fontSharedMaterial = templateText.fontSharedMaterial;
            }

            text.text = label;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
        }
        

        private static void BindLoadoutSourceToggleButtons(this LobbyPanelComponent self)
        {
            if (self.LoadoutShopButton == null || self.LoadoutWarehouseButton == null)
            {
                return;
            }

            EntityRef<LobbyPanelComponent> selfRef = self;

            self.LoadoutShopButton.onClick.RemoveAllListeners();
            self.LoadoutShopButton.onClick.AddListener(() =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                panel.SelectLoadoutSourceMode(LoadoutItemSourceMode.Shop);
            });

            self.LoadoutWarehouseButton.onClick.RemoveAllListeners();
            self.LoadoutWarehouseButton.onClick.AddListener(() =>
            {
                LobbyPanelComponent panel = selfRef;
                if (panel == null || panel.IsDisposed)
                {
                    return;
                }

                panel.SelectLoadoutSourceMode(LoadoutItemSourceMode.Warehouse);
            });
        }

        private static void RefreshLoadoutSourceToggleVisual(this LobbyPanelComponent self)
        {
            ApplyLoadoutSourceButtonState(
                self.LoadoutShopButtonGraphic,
                self.LoadoutShopButtonText,
                self.CurrentItemSourceMode == LoadoutItemSourceMode.Shop);
            ApplyLoadoutSourceButtonState(
                self.LoadoutWarehouseButtonGraphic,
                self.LoadoutWarehouseButtonText,
                self.CurrentItemSourceMode == LoadoutItemSourceMode.Warehouse);
        }

        private static void ApplyLoadoutSourceButtonState(Graphic graphic, TMP_Text text, bool selected)
        {
            if (graphic != null)
            {
                graphic.color = selected
                    ? new Color(0.75f, 0.61f, 0.23f, 0.96f)
                    : new Color(0.22f, 0.22f, 0.22f, 0.94f);
            }

            if (text != null)
            {
                text.color = selected ? Color.black : Color.white;
            }
        }
    }
}
