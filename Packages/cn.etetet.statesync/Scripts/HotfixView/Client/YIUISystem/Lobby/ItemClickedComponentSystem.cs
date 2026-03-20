using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.19
    /// Desc
    /// </summary>
    [FriendOf(typeof(ItemClickedComponent))]
    public static partial class ItemClickedComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ItemClickedComponent self)
        {
            self.IconImage = self.u_ComImage?.GetComponent<Image>();
            self.SetEquipVisible(false);
            self.u_DataTitle?.SetValue(string.Empty, true);
            self.u_DataDesc?.SetValue(string.Empty, true);
            self.u_DataValue?.SetValue(string.Empty, true);
        }

        [EntitySystem]
        private static void Destroy(this ItemClickedComponent self)
        {
            self.ReleaseItemIconSprite();
            self.IconImage = null;
            self.LoadedIconName = string.Empty;
            self.ConfigId = 0;
            self.ItemUid = 0;
            self.AllowEquipAction = false;
            self.TargetEquipSlot = EquipSlotType.BagContent;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this ItemClickedComponent self, ItemClickedOpenData openData)
        {
            if (openData.ConfigId <= 0)
            {
                Log.Warning("[ItemClicked] Open failed: invalid configId");
                return false;
            }

            LobbyPanelComponent lobbyPanel = openData.LobbyPanelRef;
            self.LobbyPanelRef = openData.LobbyPanelRef;
            self.ConfigId = openData.ConfigId;
            self.ItemUid = openData.ItemUid;
            self.AllowEquipAction = openData.AllowEquipAction && lobbyPanel != null && !lobbyPanel.IsDisposed;
            self.TargetEquipSlot = self.ResolveTargetEquipSlot(openData.ConfigId);

            ResolveDisplayInfo(openData.ConfigId, out string title, out string iconName, out string desc);
            self.u_DataTitle?.SetValue(title, true);
            self.u_DataDesc?.SetValue(desc, true);
            self.u_DataValue?.SetValue(BuildValueText(openData.ConfigId), true);
            self.SetEquipVisible(self.AllowEquipAction);

            await self.ChangeItemIcon(iconName);
            return true;
        }

        private static void SetEquipVisible(this ItemClickedComponent self, bool visible)
        {
            if (self.u_ComEquipRectTransform != null)
            {
                self.u_ComEquipRectTransform.gameObject.SetActive(visible);
            }
        }

        private static Image CacheIconImage(this ItemClickedComponent self)
        {
            if (self.IconImage != null)
            {
                return self.IconImage;
            }

            if (self.u_ComImage == null)
            {
                return null;
            }

            self.IconImage = self.u_ComImage.GetComponent<Image>();
            return self.IconImage;
        }

        private static void ResolveDisplayInfo(int configId, out string title, out string iconName, out string desc)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(configId);
            EquipmentConfig equipmentConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);

            title = itemConfig?.Name;
            iconName = itemConfig?.Icon ?? string.Empty;
            desc = null;

            if (weaponConfig != null && !string.IsNullOrWhiteSpace(weaponConfig.Desc))
            {
                desc = weaponConfig.Desc;
            }

            if (string.IsNullOrWhiteSpace(desc) && itemConfig != null)
            {
                desc = itemConfig.Desc;
            }

            if (string.IsNullOrWhiteSpace(title) && equipmentConfig != null)
            {
                title = $"装备({configId})";
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Item({configId})";
            }

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = title;
            }
        }

        private static string BuildValueText(int configId)
        {
            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(configId);
            if (weaponConfig != null)
            {
                return $"伤害 {weaponConfig.Damage:0.##}  射程 {weaponConfig.AttackRange:0.#}m  弹夹 {weaponConfig.MagazineSize}";
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig == null)
            {
                return string.Empty;
            }

            if (itemConfig.CanEquipBackpack || itemConfig.IsBackpack || (itemConfig.BackpackWidth > 0 && itemConfig.BackpackHeight > 0))
            {
                return $"背包容量 {Math.Max(1, itemConfig.BackpackWidth)}x{Math.Max(1, itemConfig.BackpackHeight)}";
            }

            if (itemConfig.GridWidth > 0 || itemConfig.GridHeight > 0)
            {
                return $"占用格子 {Math.Max(1, itemConfig.GridWidth)}x{Math.Max(1, itemConfig.GridHeight)}";
            }

            if (itemConfig.LoadoutBuyPrice > 0)
            {
                return $"价格 {itemConfig.LoadoutBuyPrice}";
            }

            if (itemConfig.Level > 0)
            {
                return $"等级 {itemConfig.Level}";
            }

            if (itemConfig.Quality > 0)
            {
                return $"品质 {itemConfig.Quality}";
            }

            return string.Empty;
        }

        private static EquipSlotType ResolveTargetEquipSlot(this ItemClickedComponent self, int configId)
        {
            LobbyPanelComponent lobbyPanel = self.LobbyPanelRef;
            LoadoutComponent loadout = lobbyPanel?.Root()?.GetComponent<LoadoutComponent>();
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            EquipmentConfig equipmentConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);

            bool canEquipMainWeapon = itemConfig?.CanEquipMainWeapon ?? false;
            bool canEquipSubWeapon = itemConfig?.CanEquipSubWeapon ?? false;
            if (equipmentConfig != null)
            {
                if (equipmentConfig.EquipSlot == (int)EquipmentSlotType.MainHand)
                {
                    canEquipMainWeapon = true;
                    canEquipSubWeapon = true;
                }
                else if (equipmentConfig.EquipSlot == (int)EquipmentSlotType.OffHand)
                {
                    canEquipSubWeapon = true;
                }
            }

            if (canEquipMainWeapon || canEquipSubWeapon)
            {
                bool mainWeaponEmpty = loadout == null || loadout.MainWeaponConfigId <= 0;
                bool subWeaponEmpty = loadout == null || loadout.SubWeaponConfigId <= 0;
                if (canEquipMainWeapon && mainWeaponEmpty)
                {
                    return EquipSlotType.Weapon;
                }

                if (canEquipSubWeapon && subWeaponEmpty)
                {
                    return EquipSlotType.Weapon2;
                }

                if (canEquipMainWeapon)
                {
                    return EquipSlotType.Weapon;
                }

                if (canEquipSubWeapon)
                {
                    return EquipSlotType.Weapon2;
                }
            }

            if ((itemConfig?.CanEquipArmor ?? false) || equipmentConfig?.EquipSlot == (int)EquipmentSlotType.Chest)
            {
                return EquipSlotType.Armor;
            }

            if (itemConfig != null && (itemConfig.CanEquipBackpack || itemConfig.IsBackpack || (itemConfig.BackpackWidth > 0 && itemConfig.BackpackHeight > 0)))
            {
                return EquipSlotType.Bag;
            }

            return EquipSlotType.BagContent;
        }

        private static async ETTask ChangeItemIcon(this ItemClickedComponent self, string iconName)
        {
            EntityRef<ItemClickedComponent> selfRef = self;
            int lockHash = self.GetHashCode();

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Image iconImage = self.CacheIconImage();
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconName))
            {
                self.ReleaseItemIconSprite();
                iconImage.enabled = false;
                self.LoadedIconName = string.Empty;
                return;
            }

            if (self.LoadedIconName == iconName && self.LoadedSprite != null)
            {
                iconImage.sprite = self.LoadedSprite;
                iconImage.enabled = true;
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = iconName });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                if (sprite != null)
                {
                    EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                        YIUISingletonHelper.YIUIMgr,
                        new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                }

                return;
            }

            iconImage = self.CacheIconImage();
            if (iconImage == null || sprite == null)
            {
                self.ReleaseItemIconSprite();
                self.LoadedIconName = string.Empty;
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                return;
            }

            self.ReleaseItemIconSprite();
            self.LoadedSprite = sprite;
            self.LoadedIconName = iconName;
            iconImage.sprite = sprite;
            iconImage.enabled = true;
        }

        private static void ReleaseItemIconSprite(this ItemClickedComponent self)
        {
            if (self.LoadedSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = self.LoadedSprite });

            if (self.IconImage != null && self.IconImage.sprite == self.LoadedSprite)
            {
                self.IconImage.sprite = null;
            }

            self.LoadedSprite = null;
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(ItemClickedComponent.OnEventEquipInvoke)]
        private static async ETTask OnEventEquipInvoke(this ItemClickedComponent self)
        {
            if (!self.AllowEquipAction || self.ConfigId <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            LobbyPanelComponent lobbyPanel = self.LobbyPanelRef;
            if (lobbyPanel == null || lobbyPanel.IsDisposed)
            {
                Log.Warning("[ItemClicked] Equip failed: LobbyPanel missing");
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<ItemClickedComponent> selfRef = self;
            bool success = await lobbyPanel.EquipItemAsync(self.ConfigId, self.TargetEquipSlot, LoadoutItemSourceMode.Warehouse, self.ItemUid);
            self = selfRef;
            if (self == null || self.IsDisposed || !success)
            {
                return;
            }

            YIUIViewComponent viewComponent = self.UIBase?.GetComponent<YIUIViewComponent>();
            if (viewComponent != null)
            {
                await viewComponent.CloseAsync();
            }
        }
        #endregion YIUIEvent结束
    }
}
