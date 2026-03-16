using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 武器槽位组件系统
    /// </summary>
    [FriendOf(typeof(WeaponItemComponent))]
    public static partial class WeaponItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this WeaponItemComponent self)
        {
            self.CacheViews();
            self.SetSelected(false);
            self.u_DataAmmoText?.SetValue(string.Empty, true);
        }

        [EntitySystem]
        private static void Destroy(this WeaponItemComponent self)
        {
            // 清理事件
            if (self.u_EventClick != null && self.u_EventClickHandle != null)
            {
                self.u_EventClick.Remove(self.u_EventClickHandle);
            }
            self.u_EventClickHandle = null;
            self.u_EventClick = null;
            self.ReleaseWeaponIconSprite();
            self.IconImage = null;
            self.SelectImage = null;
            self.LoadedIconName = string.Empty;
        }

        #region YIUIEvent开始

        /// <summary>
        /// 武器槽位点击事件
        /// </summary>
        [YIUIInvoke(WeaponItemComponent.OnEventClickInvoke)]
        private static async ETTask OnEventClickInvoke(this WeaponItemComponent self)
        {
            Scene root = self.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (self.SlotIndex <= 0)
            {
                Log.Warning("[WeaponItem] SlotIndex is invalid, skip switch request");
                await ETTask.CompletedTask;
                return;
            }

            // 发送切换武器请求
            WeaponSwitchHelper.SwitchWeapon(root, self.SlotIndex);

            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束

        /// <summary>
        /// 刷新武器槽位显示
        /// </summary>
        public static void RefreshSlot(this WeaponItemComponent self, WeaponComponent weaponComp)
        {
            if (weaponComp == null) return;

            int weaponId = self.SlotIndex == 1 ? weaponComp.Slot1WeaponId : weaponComp.Slot2WeaponId;
            int ammo = self.SlotIndex == 1 ? weaponComp.Slot1Ammo : weaponComp.Slot2Ammo;
            bool isReloading = self.SlotIndex == 1 ? weaponComp.Slot1Reloading : weaponComp.Slot2Reloading;
            bool isCurrent = weaponComp.CurrentSlot == self.SlotIndex;
            self.CacheViews();
            self.SetSelected(isCurrent);
            self.SetWeaponIconByWeaponId(weaponId);
            self.RefreshAmmoText(weaponComp, weaponId, ammo, isReloading);
            self.RefreshBackgroundColor(weaponId, isCurrent, isReloading);
        }

        /// <summary>
        /// 缓存武器项显示组件
        /// </summary>
        private static void CacheViews(this WeaponItemComponent self)
        {
            if (self.IconImage != null && self.SelectImage != null)
            {
                return;
            }

            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return;
            }

            Transform viewRoot = rootTransform.Find("WeaponItem") ?? rootTransform;
            self.IconImage = viewRoot.Find("WeaponBg")?.GetComponent<Image>();
            self.SelectImage = viewRoot.Find("WeaponSelect")?.GetComponent<Image>();
            if (self.IconImage != null)
            {
                // 点击事件绑定在 WeaponSelect，背景不拦截射线。
                self.IconImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// 刷新选中态表现
        /// </summary>
        private static void SetSelected(this WeaponItemComponent self, bool selected)
        {
            self.CacheViews();
            if (self.SelectImage == null)
            {
                return;
            }

            if (!self.SelectImage.gameObject.activeSelf)
            {
                self.SelectImage.gameObject.SetActive(true);
            }

            CanvasRenderer selectRenderer = self.SelectImage.canvasRenderer;
            if (selectRenderer != null)
            {
                // alpha=0 时透明裁剪可能导致点击目标被剔除。
                selectRenderer.cullTransparentMesh = false;
            }

            Color color = self.SelectImage.color;
            float targetAlpha = selected ? 1f : 0.001f;
            if (!Mathf.Approximately(color.a, targetAlpha))
            {
                color.a = targetAlpha;
                self.SelectImage.color = color;
            }

            // Click 绑定在 WeaponSelect 上，不能关掉这个对象。
            self.SelectImage.raycastTarget = true;
        }

        /// <summary>
        /// 根据武器ID刷新图标
        /// </summary>
        private static void SetWeaponIconByWeaponId(this WeaponItemComponent self, int weaponId)
        {
            string iconName = self.ResolveWeaponIconName(weaponId);
            self.ChangeWeaponIcon(iconName).Coroutine();
        }

        /// <summary>
        /// 刷新弹药文本
        /// </summary>
        private static void RefreshAmmoText(this WeaponItemComponent self, WeaponComponent weaponComp, int weaponId, int ammo, bool isReloading)
        {
            if (weaponId <= 0)
            {
                self.SetAmmoText("-");
                return;
            }

            if (isReloading)
            {
                self.SetAmmoText("R");
                return;
            }

            int magazineSize = weaponId > 0 ? weaponComp.GetEffectiveMagazineSize(self.SlotIndex) : 0;
            if (magazineSize <= 0)
            {
                global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(weaponId);
                magazineSize = weaponConfig?.MagazineSize ?? 0;
            }

            self.SetAmmoText(magazineSize > 0 ? $"{ammo}/{magazineSize}" : ammo.ToString());
        }

        /// <summary>
        /// 刷新背景表现
        /// </summary>
        private static void RefreshBackgroundColor(this WeaponItemComponent self, int weaponId, bool isCurrent, bool isReloading)
        {
            self.CacheViews();
            if (self.IconImage == null)
            {
                return;
            }

            if (weaponId <= 0)
            {
                self.IconImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                return;
            }

            if (isReloading)
            {
                self.IconImage.color = new Color(0.85f, 0.68f, 0.28f, 1f);
                return;
            }

            self.IconImage.color = isCurrent
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0.72f, 0.72f, 0.72f, 1f);
        }

        /// <summary>
        /// 设置弹药文本
        /// </summary>
        private static void SetAmmoText(this WeaponItemComponent self, string text)
        {
            if (self.u_DataAmmoText == null)
            {
                return;
            }

            self.u_DataAmmoText.SetValue(text);
        }

        /// <summary>
        /// 异步切换武器图标
        /// </summary>
        private static async ETTask ChangeWeaponIcon(this WeaponItemComponent self, string iconName)
        {
            EntityRef<WeaponItemComponent> selfRef = self;
            int lockHash = self.GetHashCode();

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.CacheViews();
            if (self.IconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconName))
            {
                self.ReleaseWeaponIconSprite();
                self.IconImage.sprite = null;
                self.IconImage.enabled = false;
                self.LoadedIconName = string.Empty;
                return;
            }

            if (self.LoadedIconName == iconName && self.LoadedSprite != null)
            {
                self.IconImage.sprite = self.LoadedSprite;
                self.IconImage.enabled = true;
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

            self.CacheViews();
            if (self.IconImage == null || sprite == null)
            {
                self.ReleaseWeaponIconSprite();
                if (self.IconImage != null)
                {
                    self.IconImage.sprite = null;
                    self.IconImage.enabled = false;
                }

                self.LoadedIconName = string.Empty;
                return;
            }

            self.ReleaseWeaponIconSprite();
            self.LoadedSprite = sprite;
            self.LoadedIconName = iconName;
            self.IconImage.sprite = sprite;
            self.IconImage.enabled = true;
            self.IconImage.preserveAspect = true;
        }

        /// <summary>
        /// 释放已加载的武器图标
        /// </summary>
        private static void ReleaseWeaponIconSprite(this WeaponItemComponent self)
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

        /// <summary>
        /// 根据武器配置解析图标资源名
        /// </summary>
        private static string ResolveWeaponIconName(this WeaponItemComponent self, int weaponId)
        {
            if (weaponId <= 0)
            {
                return string.Empty;
            }

            global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(weaponId);
            if (weaponConfig == null)
            {
                return string.Empty;
            }

            return weaponConfig.WeaponTypeId switch
            {
                (int)global::ET.WeaponType.Shotgun => "weapon_shotgun",
                (int)global::ET.WeaponType.Rifle1 => "weapon_rifle1",
                (int)global::ET.WeaponType.Rifle2 => "weapon_rifle2",
                (int)global::ET.WeaponType.RocketLauncher => "weapon_rocket",
                _ => "weapon_smg",
            };
        }
    }
}
