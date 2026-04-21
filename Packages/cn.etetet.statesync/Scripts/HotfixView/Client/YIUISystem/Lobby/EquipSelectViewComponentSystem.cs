using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.1
    /// Desc
    /// </summary>
    [FriendOf(typeof(EquipSelectViewComponent))]
    [FriendOf(typeof(LobbyPanelComponent))]
    [FriendOf(typeof(EquipSelectItemComponent))]
    public static partial class EquipSelectViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this EquipSelectViewComponent self)
        {
            EquipSelectViewPreviewHelper.BindPreviewReferences(self);

            // 初始化装备列表 LoopScroll
            var loopScroll = self.u_ComEquipSelectLoopScroll.GetComponentInChildren<LoopScrollRect>();
            if (loopScroll == null)
            {
                Log.Error("EquipSelectView: 未找到LoopScrollRect组件，请检查预制体配置");
                return;
            }
            self.m_EquipLoop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
                loopScroll,
                typeof(EquipSelectItemComponent),
                "u_EventSelect"
            );

            self.PendingItemConfigId = 0;
            EquipSelectViewPreviewHelper.ClearPreview(self);
        }

        [EntitySystem]
        private static void Destroy(this EquipSelectViewComponent self)
        {
            EquipSelectViewPreviewHelper.ReleaseSelectedIconSprite(self);
            self.GunDescData = null;
            self.SelectImage = null;
            self.LoadedSelectIconName = string.Empty;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this EquipSelectViewComponent self)
        {
            self.PendingItemConfigId = 0;
            self.PendingItemSourceMode = LoadoutItemSourceMode.Warehouse;
            self.CurrentItemSourceMode = LoadoutItemSourceMode.Warehouse;
            EquipSelectViewPreviewHelper.ClearPreview(self);
            await ETTask.CompletedTask;
            return true;
        }

        /// <summary>
        /// 装备选择项绑定回调
        /// </summary>
        [EntitySystem]
        private static void YIUILoopRenderer(
            this EquipSelectViewComponent self,
            EquipSelectItemComponent item,
            LoadoutWarehouseItemViewData data,
            int index,
            bool select)
        {
            item.u_DataEquipName.SetValue(FormatEquipSelectItemText(data));
            item.SetSelected(select);
            item.SetItemIcon(data.Icon);
        }

        /// <summary>
        /// 装备选择项点击回调
        /// </summary>
        [EntitySystem]
        private static void YIUILoopOnClick(
            this EquipSelectViewComponent self,
            EquipSelectItemComponent item,
            LoadoutWarehouseItemViewData data,
            int index,
            bool select)
        {
            item.SetSelected(select);
            if (!select)
            {
                return;
            }

            EquipSelectViewPreviewHelper.UpdatePreview(self, data, true);
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(EquipSelectViewComponent.OnEventClickPreparedInvoke)]
        private static async ETTask OnEventClickPreparedInvoke(this EquipSelectViewComponent self)
        {
            if (self.PendingItemConfigId <= 0)
            {
                Log.Warning("[EquipSelectView] Prepared ignored: no pending selection");
                await ETTask.CompletedTask;
                return;
            }

            LobbyPanelComponent lobbyPanel = self.LobbyPanel;
            if (lobbyPanel == null || lobbyPanel.IsDisposed)
            {
                Log.Warning("[EquipSelectView] Prepared failed: LobbyPanel missing");
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<EquipSelectViewComponent> selfRef = self;
            bool success = await lobbyPanel.EquipItemAsync(self.PendingItemConfigId, self.CurrentSlotType, self.PendingItemSourceMode);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (success)
            {
                await self.UIView.CloseAsync();
            }
        }

        private static string FormatEquipSelectItemText(LoadoutWarehouseItemViewData data)
        {
            return data.SourceMode == LoadoutItemSourceMode.Shop
                ? (data.Affordable ? $"{data.Name} ￥{data.Price}" : $"{data.Name} ￥{data.Price} [不足]")
                : $"{data.Name} x{data.Count}";
        }
        
        [YIUIInvoke(EquipSelectViewComponent.OnEventExitViewInvoke)]
        private static async ETTask OnEventExitViewInvoke(this EquipSelectViewComponent self)
        {
            await self.UIView.CloseAsync();
        }
        
        [YIUIInvoke(EquipSelectViewComponent.OnEventClickShopInvoke)]
        private static async ETTask OnEventClickShopInvoke(this EquipSelectViewComponent self)
        {
            await self.SwitchSourceModeAsync(LoadoutItemSourceMode.Shop);
        }
        
        [YIUIInvoke(EquipSelectViewComponent.OnEventClickWarhouseInvoke)]
        private static async ETTask OnEventClickWarhouseInvoke(this EquipSelectViewComponent self)
        {
            await self.SwitchSourceModeAsync(LoadoutItemSourceMode.Warehouse);
        }
        #endregion YIUIEvent结束

        private static async ETTask SwitchSourceModeAsync(this EquipSelectViewComponent self, LoadoutItemSourceMode sourceMode)
        {
            if (self == null || self.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            LobbyPanelComponent lobbyPanel = self.LobbyPanel;
            if (lobbyPanel == null || lobbyPanel.IsDisposed)
            {
                Log.Warning("[EquipSelectView] Switch source mode failed: LobbyPanel missing");
                await ETTask.CompletedTask;
                return;
            }

            await lobbyPanel.SwitchEquipSelectSourceModeAsync(self, sourceMode);
        }
    }

    [FriendOf(typeof(EquipSelectViewComponent))]
    public static class EquipSelectViewPreviewHelper
    {
        private const string SelectImageBindName = "u_ComSelectImage";
        private const string LegacySelectImageBindName = "SelectImage";

        public static void BindPreviewReferences(EquipSelectViewComponent self)
        {
            YIUIChild uiBase = self.UIBase;
            if (uiBase == null)
            {
                return;
            }

            if (self.GunDescData == null && uiBase.DataTable?.DataDic.ContainsKey("u_DataGunDesc") == true)
            {
                self.GunDescData = uiBase.DataTable.FindDataValue<UIDataValueString>("u_DataGunDesc");
            }

            if (self.SelectImage == null)
            {
                self.SelectImage = ResolvePreviewImage(uiBase);
                if (self.SelectImage != null)
                {
                    self.SelectImage.preserveAspect = true;
                }
            }

            if (self.PreviewRootRectTransform == null)
            {
                self.PreviewRootRectTransform = uiBase.OwnerRectTransform?.FindChildByName("GunBg") as RectTransform;
            }
        }

        public static void ClearPreview(EquipSelectViewComponent self)
        {
            BindPreviewReferences(self);
            self.u_DataGunName?.SetValue(string.Empty, true);
            self.GunDescData?.SetValue(string.Empty, true);
            ReleaseSelectedIconSprite(self);

            if (self.PreviewRootRectTransform != null)
            {
                self.PreviewRootRectTransform.gameObject.SetActive(false);
            }

            if (self.SelectImage != null)
            {
                self.SelectImage.enabled = false;
            }

            self.LoadedSelectIconName = string.Empty;
        }

        public static void UpdatePreview(EquipSelectViewComponent self, LoadoutWarehouseItemViewData itemData, bool updatePending)
        {
            if (itemData.ConfigId <= 0)
            {
                ClearPreview(self);
                if (updatePending)
                {
                    self.PendingItemConfigId = 0;
                    self.PendingItemSourceMode = self.CurrentItemSourceMode;
                }

                return;
            }

            if (updatePending)
            {
                self.PendingItemConfigId = itemData.ConfigId;
                self.PendingItemSourceMode = itemData.SourceMode;
            }

            if (self.PreviewRootRectTransform != null)
            {
                self.PreviewRootRectTransform.gameObject.SetActive(true);
            }

            ResolvePreviewDisplayInfo(self, itemData, out string title, out string desc, out string iconName);
            self.u_DataGunName?.SetValue(title);
            self.GunDescData?.SetValue(desc);
            ChangeSelectedIcon(self, iconName).Coroutine();
        }

        public static void ReleaseSelectedIconSprite(EquipSelectViewComponent self)
        {
            if (self.LoadedSelectSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = self.LoadedSelectSprite });

            if (self.SelectImage != null && self.SelectImage.sprite == self.LoadedSelectSprite)
            {
                self.SelectImage.sprite = null;
            }

            self.LoadedSelectSprite = null;
        }

        private static void ResolvePreviewDisplayInfo(
            EquipSelectViewComponent self,
            LoadoutWarehouseItemViewData itemData,
            out string title,
            out string desc,
            out string iconName)
        {
            title = itemData.Name;
            desc = null;
            iconName = itemData.Icon ?? string.Empty;

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(itemData.ConfigId);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = itemConfig?.Name;
            }

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(itemData.ConfigId);
            desc = weaponConfig?.Desc;

            if (string.IsNullOrWhiteSpace(desc) && itemConfig != null)
            {
                desc = itemConfig.Desc;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Item({itemData.ConfigId})";
            }

            if (weaponConfig != null)
            {
                desc = BuildWeaponPreviewDesc(weaponConfig, desc);
            }

            if (string.IsNullOrWhiteSpace(desc))
            {
                desc = title;
            }
        }

        private static string BuildWeaponPreviewDesc(WeaponConfig weaponConfig, string desc)
        {
            string line1 = $"类型：{FormatWeaponTypeText(weaponConfig.WeaponTypeId)} | 伤害：{FormatNumberText(weaponConfig.Damage)} | 射程：{FormatNumberText(weaponConfig.AttackRange)}m";
            string line2 = $"射速：{FormatFireRateText(weaponConfig.AttackIntervalMs)} | 弹匣：{weaponConfig.MagazineSize} | 换弹：{FormatSecondsText(weaponConfig.ReloadTimeMs)}";

            if (string.IsNullOrWhiteSpace(desc))
            {
                return $"{line1}\n{line2}";
            }

            return $"{line1}\n{line2}\n说明：{desc}";
        }

        private static string FormatWeaponTypeText(int weaponTypeId)
        {
            return weaponTypeId switch
            {
                (int)WeaponType.Shotgun         => "散弹枪",
                (int)WeaponType.Rifle1          => "步枪1",
                (int)WeaponType.Rifle2          => "步枪2",
                (int)WeaponType.RocketLauncher  => "火箭炮",
                (int)WeaponType.SMG             => "冲锋枪",
                (int)WeaponType.AutoRifle       => "自动步枪",
                (int)WeaponType.SniperRifle     => "狙击枪",
                (int)WeaponType.Pistol          => "手枪",
                (int)WeaponType.GrenadeLauncher => "榴弹炮",
                (int)WeaponType.RayGun          => "射线枪",
                _                               => $"类型{weaponTypeId}",
            };
        }

        private static string FormatNumberText(float value)
        {
            float rounded = Mathf.Round(value);
            if (Mathf.Abs(value - rounded) < 0.01f)
            {
                return rounded.ToString("0");
            }

            return value.ToString("0.#");
        }

        private static string FormatFireRateText(int attackIntervalMs)
        {
            if (attackIntervalMs <= 0)
            {
                return "-";
            }

            return $"{FormatNumberText(1000f / attackIntervalMs)}/s";
        }

        private static string FormatSecondsText(int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return "-";
            }

            return $"{FormatNumberText(milliseconds / 1000f)}s";
        }

        private static Image CacheSelectedIconImage(EquipSelectViewComponent self)
        {
            BindPreviewReferences(self);
            return self.SelectImage;
        }

        private static Image ResolvePreviewImage(YIUIChild uiBase)
        {
            if (uiBase?.ComponentTable == null)
            {
                return null;
            }

            if (uiBase.ComponentTable.AllBindDic.ContainsKey(SelectImageBindName))
            {
                return uiBase.ComponentTable.FindComponent<Image>(SelectImageBindName);
            }

            if (uiBase.ComponentTable.AllBindDic.ContainsKey(LegacySelectImageBindName))
            {
                return uiBase.ComponentTable.FindComponent<Image>(LegacySelectImageBindName);
            }

            return null;
        }

        private static async ETTask ChangeSelectedIcon(EquipSelectViewComponent self, string iconName)
        {
            EntityRef<EquipSelectViewComponent> selfRef = self;
            int lockHash = self.GetHashCode();

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Image iconImage = CacheSelectedIconImage(self);
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconName))
            {
                ReleaseSelectedIconSprite(self);
                iconImage.enabled = false;
                self.LoadedSelectIconName = string.Empty;
                return;
            }

            if (self.LoadedSelectIconName == iconName && self.LoadedSelectSprite != null)
            {
                iconImage.sprite = self.LoadedSelectSprite;
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

            iconImage = CacheSelectedIconImage(self);
            if (iconImage == null || sprite == null)
            {
                ReleaseSelectedIconSprite(self);
                self.LoadedSelectIconName = string.Empty;
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                return;
            }

            ReleaseSelectedIconSprite(self);
            self.LoadedSelectSprite = sprite;
            self.LoadedSelectIconName = iconName;
            iconImage.sprite = sprite;
            iconImage.enabled = true;
            iconImage.preserveAspect = true;
        }
    }
}
