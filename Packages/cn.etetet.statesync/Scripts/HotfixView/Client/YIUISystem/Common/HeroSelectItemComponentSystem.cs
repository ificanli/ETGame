using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.2.28
    /// Desc
    /// </summary>
    [FriendOf(typeof(HeroSelectItemComponent))]
    public static partial class HeroSelectItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HeroSelectItemComponent self)
        {
            self.CacheHeroIconImage();
        }

        [EntitySystem]
        private static void Destroy(this HeroSelectItemComponent self)
        {
            self.ReleaseHeroIconSprite();
            self.m_HeroIconImage = null;
            self.m_LastHeroIconName = string.Empty;
        }

        public static void SetHeroIcon(this HeroSelectItemComponent self, string iconName)
        {
            self.ChangeHeroIcon(iconName).Coroutine();
        }

        private static Image CacheHeroIconImage(this HeroSelectItemComponent self)
        {
            if (self.m_HeroIconImage != null)
            {
                return self.m_HeroIconImage;
            }

            YIUIChild uiBase = self.UIBase;
            if (uiBase == null || uiBase.OwnerGameObject == null)
            {
                return null;
            }

            Transform bg = uiBase.OwnerGameObject.transform.Find("Bg");
            if (bg == null)
            {
                return null;
            }

            self.m_HeroIconImage = bg.GetComponent<Image>();
            return self.m_HeroIconImage;
        }

        private static async ETTask ChangeHeroIcon(this HeroSelectItemComponent self, string iconName)
        {
            EntityRef<HeroSelectItemComponent> selfRef = self;
            int lockHash = self.GetHashCode();

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;

            Image iconImage = self.CacheHeroIconImage();
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(iconName))
            {
                self.ReleaseHeroIconSprite();
                iconImage.enabled = false;
                self.m_LastHeroIconName = string.Empty;
                return;
            }

            if (self.m_LastHeroIconName == iconName && self.m_LastHeroSprite != null)
            {
                if (iconImage.sprite == null)
                {
                    iconImage.sprite = self.m_LastHeroSprite;
                }

                iconImage.enabled = true;
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = iconName });

            self = selfRef;

            if (sprite == null)
            {
                self.ReleaseHeroIconSprite();
                iconImage.enabled = false;
                self.m_LastHeroIconName = string.Empty;
                return;
            }

            self.ReleaseHeroIconSprite();

            if (self.IsDisposed || iconImage == null)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                return;
            }

            self.m_LastHeroSprite = sprite;
            self.m_LastHeroIconName = iconName;
            iconImage.sprite = sprite;
            iconImage.enabled = true;
        }

        private static void ReleaseHeroIconSprite(this HeroSelectItemComponent self)
        {
            if (self.m_LastHeroSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = self.m_LastHeroSprite });

            if (self.m_HeroIconImage != null && self.m_HeroIconImage.sprite == self.m_LastHeroSprite)
            {
                self.m_HeroIconImage.sprite = null;
            }

            self.m_LastHeroSprite = null;
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(HeroSelectItemComponent.OnEventSelectInvoke)]
        private static void OnEventSelectInvoke(this HeroSelectItemComponent self)
        {

        }
        #endregion YIUIEvent结束
    }
}
