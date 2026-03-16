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
    [FriendOf(typeof(EquipSelectItemComponent))]
    public static partial class EquipSelectItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this EquipSelectItemComponent self)
        {
            self.CacheIconImage();
            self.CacheBgRectTransform();
            self.RefreshSelectVisual(self.u_DataSelect?.GetValue() ?? false);
        }

        [EntitySystem]
        private static void Destroy(this EquipSelectItemComponent self)
        {
            self.ReleaseItemIconSprite();
            self.IconImage = null;
            self.BgRectTransform = null;
            self.LoadedIconName = string.Empty;
        }

        public static void SetItemIcon(this EquipSelectItemComponent self, string iconName)
        {
            self.ChangeItemIcon(iconName).Coroutine();
        }

        public static void SetSelected(this EquipSelectItemComponent self, bool selected)
        {
            self.u_DataSelect?.SetValue(selected);
            self.RefreshSelectVisual(selected);
        }

        private static Image CacheIconImage(this EquipSelectItemComponent self)
        {
            if (self.IconImage != null)
            {
                return self.IconImage;
            }

            if (self.u_ComIcon == null)
            {
                Log.Warning($"[EquipSelectItem] CacheIconImage failed: u_ComIcon is null, ui={EquipSelectItemComponent.ResName}");
                return null;
            }

            self.IconImage = self.u_ComIcon.GetComponent<Image>();
            if (self.IconImage == null)
            {
                Log.Warning($"[EquipSelectItem] CacheIconImage failed: Image missing on u_ComIcon, ui={EquipSelectItemComponent.ResName}");
            }

            return self.IconImage;
        }

        private static RectTransform CacheBgRectTransform(this EquipSelectItemComponent self)
        {
            if (self.BgRectTransform != null)
            {
                return self.BgRectTransform;
            }

            if (self.u_ComBgRectTransform == null)
            {
                Log.Warning($"[EquipSelectItem] CacheBgRectTransform failed: u_ComBgRectTransform is null, ui={EquipSelectItemComponent.ResName}");
                return null;
            }

            self.BgRectTransform = self.u_ComBgRectTransform;
            return self.BgRectTransform;
        }

        private static void RefreshSelectVisual(this EquipSelectItemComponent self, bool selected)
        {
            RectTransform bgRectTransform = self.CacheBgRectTransform();
            if (bgRectTransform == null)
            {
                return;
            }

            bgRectTransform.gameObject.SetActive(!selected);
        }

        private static async ETTask ChangeItemIcon(this EquipSelectItemComponent self, string iconName)
        {
            EntityRef<EquipSelectItemComponent> selfRef = self;
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
            iconImage.enabled = true;
        }

        private static void ReleaseItemIconSprite(this EquipSelectItemComponent self)
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
        
        [YIUIInvoke(EquipSelectItemComponent.OnEventSelectInvoke)]
        private static ETTask OnEventSelectInvoke(this EquipSelectItemComponent self)
        {
            return ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
