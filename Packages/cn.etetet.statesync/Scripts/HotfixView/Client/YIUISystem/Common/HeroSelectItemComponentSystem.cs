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
        private const float NORMAL_SCALE_X = 1f;
        private const float NORMAL_SCALE_Y = 1f;
        private const float NORMAL_SCALE_Z = 1f;
        private const float SELECTED_SCALE_X = 1.1f;
        private const float SELECTED_SCALE_Y = 1.2f;
        private const float SELECTED_SCALE_Z = 1f;
        private const float SELECTED_OFFSET_Y = 15f;

        [EntitySystem]
        private static void YIUIInitialize(this HeroSelectItemComponent self)
        {
            self.CacheHeroIconImages();
            self.RefreshSelectVisual(self.u_DataSelect?.GetValue() ?? false);
        }

        [EntitySystem]
        private static void Destroy(this HeroSelectItemComponent self)
        {
            self.ReleaseHeroIconSprite();
            self.m_HeroIconImages.Clear();
            self.m_VisualRects.Clear();
            self.m_VisualBaseAnchoredPositions.Clear();
            self.m_LastHeroIconName = string.Empty;
        }

        public static void SetHeroIcon(this HeroSelectItemComponent self, string iconName)
        {
            self.ChangeHeroIcon(iconName).Coroutine();
        }

        public static void SetSelected(this HeroSelectItemComponent self, bool selected)
        {
            self.u_DataSelect?.SetValue(selected);
            self.RefreshSelectVisual(selected);
        }

        private static List<Image> CacheHeroIconImages(this HeroSelectItemComponent self)
        {
            if (self.m_HeroIconImages.Count > 0)
            {
                return self.m_HeroIconImages;
            }

            YIUIChild uiBase = self.UIBase;
            if (uiBase == null || uiBase.OwnerGameObject == null)
            {
                return null;
            }

            foreach (Transform child in uiBase.OwnerGameObject.transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Bg")
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    self.m_HeroIconImages.Add(image);
                }
            }

            return self.m_HeroIconImages.Count > 0 ? self.m_HeroIconImages : null;
        }

        private static void RefreshSelectVisual(this HeroSelectItemComponent self, bool selected)
        {
            YIUIChild uiBase = self.UIBase;
            if (uiBase?.OwnerGameObject == null)
            {
                return;
            }

            Vector3 scale = selected
                ? new Vector3(SELECTED_SCALE_X, SELECTED_SCALE_Y, SELECTED_SCALE_Z)
                : new Vector3(NORMAL_SCALE_X, NORMAL_SCALE_Y, NORMAL_SCALE_Z);
            uiBase.OwnerGameObject.transform.localScale = scale;

            CacheVisualRects(self, uiBase);
            ApplySelectedOffsetY(self, selected);
        }

        private static void CacheVisualRects(HeroSelectItemComponent self, YIUIChild uiBase)
        {
            if (self.m_VisualRects.Count > 0 &&
                self.m_VisualBaseAnchoredPositions.Count == self.m_VisualRects.Count)
            {
                return;
            }

            self.m_VisualRects.Clear();
            self.m_VisualBaseAnchoredPositions.Clear();

            Transform root = uiBase?.OwnerGameObject?.transform;
            if (root == null)
            {
                return;
            }

            int childCount = root.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                RectTransform rectTransform = root.GetChild(i) as RectTransform;
                if (rectTransform == null)
                {
                    continue;
                }

                self.m_VisualRects.Add(rectTransform);
                self.m_VisualBaseAnchoredPositions.Add(rectTransform.anchoredPosition);
            }
        }

        private static void ApplySelectedOffsetY(HeroSelectItemComponent self, bool selected)
        {
            if (self.m_VisualRects.Count <= 0 ||
                self.m_VisualBaseAnchoredPositions.Count != self.m_VisualRects.Count)
            {
                return;
            }

            float offsetY = selected ? SELECTED_OFFSET_Y : 0f;
            for (int i = 0; i < self.m_VisualRects.Count; ++i)
            {
                RectTransform rectTransform = self.m_VisualRects[i];
                if (rectTransform == null)
                {
                    continue;
                }

                Vector2 basePos = self.m_VisualBaseAnchoredPositions[i];
                rectTransform.anchoredPosition = new Vector2(basePos.x, basePos.y + offsetY);
            }
        }

        private static async ETTask ChangeHeroIcon(this HeroSelectItemComponent self, string iconName)
        {
            EntityRef<HeroSelectItemComponent> selfRef = self;
            int lockHash = self.GetHashCode();

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;

            List<Image> iconImages = self.CacheHeroIconImages();
            if (iconImages == null || iconImages.Count == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(iconName))
            {
                self.ReleaseHeroIconSprite();
                self.m_LastHeroIconName = string.Empty;
                return;
            }

            if (self.m_LastHeroIconName == iconName && self.m_LastHeroSprite != null)
            {
                ApplyHeroSprite(iconImages, self.m_LastHeroSprite);
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = iconName });

            self = selfRef;

            if (sprite == null)
            {
                self.ReleaseHeroIconSprite();
                self.m_LastHeroIconName = string.Empty;
                return;
            }

            self.ReleaseHeroIconSprite();

            if (self.IsDisposed || iconImages.Count == 0)
            {
                EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                    YIUISingletonHelper.YIUIMgr,
                    new YIUIInvokeEntity_ReleaseSprite { obj = sprite });
                return;
            }

            self.m_LastHeroSprite = sprite;
            self.m_LastHeroIconName = iconName;
            ApplyHeroSprite(iconImages, sprite);
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

            foreach (Image iconImage in self.m_HeroIconImages)
            {
                if (iconImage != null && iconImage.sprite == self.m_LastHeroSprite)
                {
                    iconImage.sprite = null;
                }
            }

            self.m_LastHeroSprite = null;
        }

        private static void ApplyHeroSprite(List<Image> iconImages, Sprite sprite)
        {
            foreach (Image iconImage in iconImages)
            {
                if (iconImage == null)
                {
                    continue;
                }

                iconImage.sprite = sprite;
                if (sprite != null)
                {
                    iconImage.enabled = true;
                }
            }
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(HeroSelectItemComponent.OnEventSelectInvoke)]
        private static void OnEventSelectInvoke(this HeroSelectItemComponent self)
        {

        }
        #endregion YIUIEvent结束
    }
}
