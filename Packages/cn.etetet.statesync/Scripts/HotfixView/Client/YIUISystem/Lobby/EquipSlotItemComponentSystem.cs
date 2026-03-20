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
    [FriendOf(typeof(EquipSlotItemComponent))]
    public static partial class EquipSlotItemComponentSystem
    {
        private const float EquippedGunAlpha = 1f;
        private const float UnequippedGunAlpha = 0f;

        [EntitySystem]
        private static void YIUIInitialize(this EquipSlotItemComponent self)
        {
            self.SetGunImageAlpha(UnequippedGunAlpha);
        }

        [EntitySystem]
        private static void Destroy(this EquipSlotItemComponent self)
        {
            self.ReleaseItemIconSprite();
            self.IconImage = null;
            self.LoadedIconName = string.Empty;
        }

        public static void SetItemIcon(this EquipSlotItemComponent self, string iconName)
        {
            self.ChangeItemIcon(iconName).Coroutine();
        }

        private static Image CacheIconImage(this EquipSlotItemComponent self)
        {
            if (self.IconImage != null)
            {
                return self.IconImage;
            }

            if (self.u_ComGunRectTransform == null)
            {
                Log.Warning($"[EquipSlotItem] CacheIconImage failed: u_ComGunRectTransform is null, ui={EquipSlotItemComponent.ResName}");
                return null;
            }

            self.IconImage = self.u_ComGunRectTransform.GetComponent<Image>();
            if (self.IconImage == null)
            {
                Log.Warning($"[EquipSlotItem] CacheIconImage failed: Image missing on u_ComGunRectTransform, ui={EquipSlotItemComponent.ResName}");
            }

            return self.IconImage;
        }

        private static void SetGunImageAlpha(this EquipSlotItemComponent self, float alpha)
        {
            Image iconImage = self.CacheIconImage();
            if (iconImage == null)
            {
                return;
            }

            Color color = iconImage.color;
            color.a = Mathf.Clamp01(alpha);
            iconImage.color = color;
        }

        private static async ETTask ChangeItemIcon(this EquipSlotItemComponent self, string iconName)
        {
            EntityRef<EquipSlotItemComponent> selfRef = self;
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
                self.SetGunImageAlpha(UnequippedGunAlpha);
                iconImage.enabled = false;
                self.LoadedIconName = string.Empty;
                return;
            }

            if (self.LoadedIconName == iconName && self.LoadedSprite != null)
            {
                iconImage.sprite = self.LoadedSprite;
                self.SetGunImageAlpha(EquippedGunAlpha);
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
                self.SetGunImageAlpha(UnequippedGunAlpha);
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                self.LoadedIconName = string.Empty;
                return;
            }

            self.ReleaseItemIconSprite();
            self.LoadedSprite = sprite;
            self.LoadedIconName = iconName;
            iconImage.sprite = sprite;
            self.SetGunImageAlpha(EquippedGunAlpha);
            iconImage.enabled = true;
        }

        private static void ReleaseItemIconSprite(this EquipSlotItemComponent self)
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
        
        [YIUIInvoke(EquipSlotItemComponent.OnEventSlotClickInvoke)]
        private static async ETTask OnEventSlotClickInvoke(this EquipSlotItemComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
