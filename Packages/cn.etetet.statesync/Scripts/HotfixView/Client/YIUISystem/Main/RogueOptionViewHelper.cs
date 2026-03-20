using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 客户端通用 YooAsset 资源名常量表。
    /// key 使用 const 定义，value 由业务侧维护为对应的 YooAsset 资源名。
    /// </summary>
    public static class ClientYooAssetConstTable
    {
        public const string RogueOptionBgQuality1 = "rogue.option.bg.quality.1";
        public const string RogueOptionBgQuality2 = "rogue.option.bg.quality.2";
        public const string RogueOptionBgQuality3 = "rogue.option.bg.quality.3";
        public const string RogueOptionTagBgQuality1 = "rogue.option.tag.bg.quality.1";
        public const string RogueOptionTagBgQuality2 = "rogue.option.tag.bg.quality.2";
        public const string RogueOptionTagBgQuality3 = "rogue.option.tag.bg.quality.3";
        public const string RogueOptionBgQuality1Value = "export (28)_3";
        public const string RogueOptionBgQuality2Value = "export (28)_4";
        public const string RogueOptionBgQuality3Value = "export (28)_7";
        public const string RogueOptionTagBgQuality1Value = "export (28)_0";
        public const string RogueOptionTagBgQuality2Value = "export (28)_1";
        public const string RogueOptionTagBgQuality3Value = "export (28)_2";

        /// <summary>
        /// 这里返回每个 key 对应的 YooAsset Sprite 资源名。
        /// </summary>
        public static bool TryGetValue(string key, out string value)
        {
            value = key switch
            {
                RogueOptionBgQuality1 => RogueOptionBgQuality1Value,
                RogueOptionBgQuality2 => RogueOptionBgQuality2Value,
                RogueOptionBgQuality3 => RogueOptionBgQuality3Value,
                RogueOptionTagBgQuality1 => RogueOptionTagBgQuality1Value,
                RogueOptionTagBgQuality2 => RogueOptionTagBgQuality2Value,
                RogueOptionTagBgQuality3 => RogueOptionTagBgQuality3Value,
                _ => string.Empty,
            };

            return !string.IsNullOrWhiteSpace(value);
        }
    }

    public static class RogueOptionViewHelper
    {
        public static void BindOption(RogueOptionComponent self, EntityRef<RoguePanelComponent> panelRef, int optionIndex, RogueClientOptionData optionData)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Panel = panelRef;
            self.OptionIndex = optionIndex;
            self.IsChoosing = false;
            self.IsRerolling = false;
            self.u_DataTextTitle?.SetValue(optionData.Name ?? string.Empty);
            self.u_DataTextDes?.SetValue(optionData.Desc ?? string.Empty);
            ConfigureClickRaycastTargets(self);
            RefreshRerollState(self, optionData.RerollCount);
            SetTags(self, CollectDisplayTags(optionData));
            ApplyQualityBackground(self, optionData.Quality);
            ApplyTagQualityBackground(self, optionData.Quality);
            ChangeOptionImage(self, optionData.ImagePath ?? string.Empty).Coroutine();
        }

        public static void ResetOption(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Panel = null;
            self.OptionIndex = -1;
            self.IsChoosing = false;
            self.IsRerolling = false;
            self.u_DataTextTitle?.SetValue(string.Empty);
            self.u_DataTextDes?.SetValue(string.Empty);
            ConfigureClickRaycastTargets(self);
            RefreshRerollState(self, 0);
            SetTags(self, System.Array.Empty<int>());
            ReleaseSprite(self);
            ChangeOptionBackgroundImage(self, string.Empty).Coroutine();
            ChangeTagBackgroundImage(self, string.Empty).Coroutine();
            if (self.IconImage != null)
            {
                self.IconImage.enabled = false;
            }
        }

        public static void ConfigureClickRaycastTargets(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            RectTransform optionRootRect = ResolveOptionClickRoot(self);
            if (optionRootRect == null)
            {
                return;
            }

            Graphic optionGraphic = optionRootRect.GetComponent<Graphic>();
            if (optionGraphic != null)
            {
                optionGraphic.raycastTarget = true;
            }

            foreach (Graphic graphic in optionRootRect.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null || graphic.transform == optionRootRect)
                {
                    continue;
                }

                graphic.raycastTarget = false;
            }

            RestoreInteractiveGraphic(self.u_ComTagRectTransform);
            RestoreInteractiveGraphic(self.u_ComTag2RectTransform);
        }

        public static void RefreshRerollState(RogueOptionComponent self, int rerollCount)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Button rerollButton = CacheRerollButton(self);
            Image rerollButtonImage = CacheRerollButtonImage(self);
            bool canReroll = rerollCount > 0;
            if (rerollButton != null)
            {
                rerollButton.interactable = canReroll;
            }

            if (rerollButtonImage != null)
            {
                rerollButtonImage.enabled = self.DefaultRerollButtonImageEnabled;
                rerollButtonImage.color = canReroll
                    ? self.DefaultRerollButtonColor
                    : ResolveRerollDisabledColor(rerollButton);
            }
        }

        public static void SetTags(RogueOptionComponent self, int[] tagIds)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.DisplayTagIds = tagIds ?? System.Array.Empty<int>();
            self.CurrentTagId = 0;
            self.IsTagDescVisible = false;
            self.u_DataTagDes?.SetValue(string.Empty);
            SetTagDescVisible(self, false);

            bool showTag1 = self.DisplayTagIds.Length >= 1;
            bool showTag2 = self.DisplayTagIds.Length >= 2;
            SetRectVisible(self.u_ComTagRectTransform, showTag1);
            SetRectVisible(self.u_ComTag2RectTransform, showTag2);

            SetTagButtonText(self.TagText1, showTag1 ? GetTagDisplayName(self.DisplayTagIds[0]) : string.Empty);
            SetTagButtonText(self.TagText2, showTag2 ? GetTagDisplayName(self.DisplayTagIds[1]) : string.Empty);
        }

        public static void ToggleTagDesc(RogueOptionComponent self, int tagIndex)
        {
            if (self == null || self.IsDisposed || tagIndex < 0 || tagIndex >= self.DisplayTagIds.Length)
            {
                return;
            }

            int tagId = self.DisplayTagIds[tagIndex];
            if (tagId <= 0)
            {
                return;
            }

            if (self.IsTagDescVisible && self.CurrentTagId == tagId)
            {
                self.CurrentTagId = 0;
                self.IsTagDescVisible = false;
                self.u_DataTagDes?.SetValue(string.Empty);
                SetTagDescVisible(self, false);
                return;
            }

            self.CurrentTagId = tagId;
            self.IsTagDescVisible = true;
            self.u_DataTagDes?.SetValue(GetTagDescription(tagId));
            SetTagDescVisible(self, true);
        }

        public static void ApplyQualityBackground(RogueOptionComponent self, int quality)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            string key = GetQualityBackgroundKey(quality);
            if (!ClientYooAssetConstTable.TryGetValue(key, out string imagePath))
            {
                imagePath = string.Empty;
            }

            ChangeOptionBackgroundImage(self, imagePath).Coroutine();
        }

        public static void ApplyTagQualityBackground(RogueOptionComponent self, int quality)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            string key = GetTagQualityBackgroundKey(quality);
            if (!ClientYooAssetConstTable.TryGetValue(key, out string imagePath))
            {
                imagePath = string.Empty;
            }

            ChangeTagBackgroundImage(self, imagePath).Coroutine();
        }

        public static async ETTask ChangeOptionImage(RogueOptionComponent self, string imagePath)
        {
            EntityRef<RogueOptionComponent> selfRef = self;
            int lockHash = self.GetHashCode();

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Image iconImage = self.IconImage ?? self.u_ComImageRectTransform?.GetComponent<Image>();
            self.IconImage = iconImage;
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                ReleaseSprite(self);
                iconImage.enabled = false;
                return;
            }

            if (self.LoadedSpriteName == imagePath && self.LoadedSprite != null)
            {
                iconImage.sprite = self.LoadedSprite;
                iconImage.enabled = true;
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = imagePath });

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

            iconImage = self.IconImage ?? self.u_ComImageRectTransform?.GetComponent<Image>();
            self.IconImage = iconImage;
            if (sprite == null || iconImage == null)
            {
                ReleaseSprite(self);
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                return;
            }

            ReleaseSprite(self);
            self.LoadedSprite = sprite;
            self.LoadedSpriteName = imagePath;
            iconImage.sprite = sprite;
            iconImage.enabled = true;
        }

        public static void ReleaseSprite(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed || self.LoadedSprite == null)
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
            self.LoadedSpriteName = string.Empty;
        }

        public static async ETTask ChangeOptionBackgroundImage(RogueOptionComponent self, string imagePath)
        {
            EntityRef<RogueOptionComponent> selfRef = self;
            int lockHash = unchecked((self.GetHashCode() * 397) ^ 7919);

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Image optionBgImage = self.OptionBgImage ?? ResolveOptionClickRoot(self)?.GetComponent<Image>();
            self.OptionBgImage = optionBgImage;
            if (optionBgImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                ReleaseBackgroundSprite(self);
                RestoreDefaultBackground(self);
                return;
            }

            if (self.LoadedBgSpriteName == imagePath && self.LoadedBgSprite != null)
            {
                optionBgImage.sprite = self.LoadedBgSprite;
                optionBgImage.enabled = true;
                optionBgImage.color = Color.white;
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = imagePath });

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

            optionBgImage = self.OptionBgImage ?? ResolveOptionClickRoot(self)?.GetComponent<Image>();
            self.OptionBgImage = optionBgImage;
            if (sprite == null || optionBgImage == null)
            {
                ReleaseBackgroundSprite(self);
                RestoreDefaultBackground(self);
                return;
            }

            ReleaseBackgroundSprite(self);
            self.LoadedBgSprite = sprite;
            self.LoadedBgSpriteName = imagePath;
            optionBgImage.sprite = sprite;
            optionBgImage.enabled = true;
            optionBgImage.color = Color.white;
        }

        public static void ReleaseBackgroundSprite(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed || self.LoadedBgSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = self.LoadedBgSprite });

            if (self.OptionBgImage != null && self.OptionBgImage.sprite == self.LoadedBgSprite)
            {
                self.OptionBgImage.sprite = null;
            }

            self.LoadedBgSprite = null;
            self.LoadedBgSpriteName = string.Empty;
        }

        public static async ETTask ChangeTagBackgroundImage(RogueOptionComponent self, string imagePath)
        {
            EntityRef<RogueOptionComponent> selfRef = self;
            int lockHash = unchecked((self.GetHashCode() * 397) ^ 12347);

            using var _ = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_CoroutineLock { Lock = lockHash });

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            CacheTagBackgroundImages(self);
            if (self.TagBgImage1 == null && self.TagBgImage2 == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                ReleaseTagBackgroundSprite(self);
                RestoreDefaultTagBackgrounds(self);
                return;
            }

            if (self.LoadedTagBgSpriteName == imagePath && self.LoadedTagBgSprite != null)
            {
                ApplyTagBackgroundSprite(self, self.LoadedTagBgSprite);
                return;
            }

            Sprite sprite = await EventSystem.Instance?.YIUIInvokeEntityAsyncSafety<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_LoadSprite { ResName = imagePath });

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

            CacheTagBackgroundImages(self);
            if (sprite == null || (self.TagBgImage1 == null && self.TagBgImage2 == null))
            {
                ReleaseTagBackgroundSprite(self);
                RestoreDefaultTagBackgrounds(self);
                return;
            }

            ReleaseTagBackgroundSprite(self);
            self.LoadedTagBgSprite = sprite;
            self.LoadedTagBgSpriteName = imagePath;
            ApplyTagBackgroundSprite(self, sprite);
        }

        public static void ReleaseTagBackgroundSprite(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed || self.LoadedTagBgSprite == null)
            {
                return;
            }

            EventSystem.Instance?.YIUIInvokeEntitySyncSafety(
                YIUISingletonHelper.YIUIMgr,
                new YIUIInvokeEntity_ReleaseSprite { obj = self.LoadedTagBgSprite });

            if (self.TagBgImage1 != null && self.TagBgImage1.sprite == self.LoadedTagBgSprite)
            {
                self.TagBgImage1.sprite = null;
            }

            if (self.TagBgImage2 != null && self.TagBgImage2.sprite == self.LoadedTagBgSprite)
            {
                self.TagBgImage2.sprite = null;
            }

            self.LoadedTagBgSprite = null;
            self.LoadedTagBgSpriteName = string.Empty;
        }

        private static int[] CollectDisplayTags(RogueClientOptionData optionData)
        {
            List<int> result = new(2);
            AppendTags(result, optionData.ShowTags);
            return result.ToArray();
        }

        private static void AppendTags(List<int> result, int[] tagIds)
        {
            if (tagIds == null || tagIds.Length == 0 || result.Count >= 2)
            {
                return;
            }

            foreach (int tagId in tagIds)
            {
                if (tagId <= 0 || result.Contains(tagId))
                {
                    continue;
                }

                result.Add(tagId);
                if (result.Count >= 2)
                {
                    return;
                }
            }
        }

        private static void SetRectVisible(RectTransform rectTransform, bool visible)
        {
            if (rectTransform == null)
            {
                return;
            }

            GameObject go = rectTransform.gameObject;
            if (go != null && go.activeSelf != visible)
            {
                go.SetActive(visible);
            }
        }

        private static void SetTagDescVisible(RogueOptionComponent self, bool visible)
        {
            SetRectVisible(self.u_ComTagsDescViewRectTransform, visible);
        }

        private static void SetTagButtonText(TMPro.TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value ?? string.Empty;
        }

        private static string GetTagDisplayName(int tagId)
        {
            RogueTagConfig tagConfig = GetTagConfig(tagId);
            if (tagConfig == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(tagConfig.ShowTagsName))
            {
                return tagConfig.ShowTagsName;
            }

            return tagConfig.TagDes ?? string.Empty;
        }

        private static string GetTagDescription(int tagId)
        {
            RogueTagConfig tagConfig = GetTagConfig(tagId);
            if (tagConfig == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(tagConfig.ShowTagsDesc))
            {
                return tagConfig.ShowTagsDesc;
            }

            if (!string.IsNullOrWhiteSpace(tagConfig.TagDes))
            {
                return tagConfig.TagDes;
            }

            return tagConfig.ShowTagsName ?? string.Empty;
        }

        private static RogueTagConfig GetTagConfig(int tagId)
        {
            RogueRuntimeConfigCategory category = RogueRuntimeConfigCategory.Instance;
            if (category != null && category.TryGetTag(tagId, out RogueTagConfig tagConfig))
            {
                return tagConfig;
            }

            return null;
        }

        private static void RestoreInteractiveGraphic(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            Graphic graphic = rectTransform.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }
        }

        private static Button CacheRerollButton(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return null;
            }

            self.RerollButton ??= ResolveRerollButtonTransform(self)?.GetComponent<Button>();
            return self.RerollButton;
        }

        private static Image CacheRerollButtonImage(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return null;
            }

            self.RerollButtonImage ??= ResolveRerollButtonTransform(self)?.GetComponent<Image>();
            return self.RerollButtonImage;
        }

        private static Color ResolveRerollDisabledColor(Button rerollButton)
        {
            if (rerollButton != null)
            {
                return rerollButton.colors.disabledColor;
            }

            return new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private static void RestoreDefaultBackground(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Image optionBgImage = self.OptionBgImage ?? ResolveOptionClickRoot(self)?.GetComponent<Image>();
            self.OptionBgImage = optionBgImage;
            if (optionBgImage == null)
            {
                return;
            }

            optionBgImage.sprite = self.DefaultBgSprite;
            optionBgImage.color = self.DefaultBgColor;
            optionBgImage.enabled = self.DefaultBgImageEnabled;
        }

        private static void RestoreDefaultTagBackgrounds(RogueOptionComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            CacheTagBackgroundImages(self);

            if (self.TagBgImage1 != null)
            {
                self.TagBgImage1.sprite = self.DefaultTagBgSprite1;
                self.TagBgImage1.color = self.DefaultTagBgColor1;
                self.TagBgImage1.enabled = self.DefaultTagBgImageEnabled1;
            }

            if (self.TagBgImage2 != null)
            {
                self.TagBgImage2.sprite = self.DefaultTagBgSprite2;
                self.TagBgImage2.color = self.DefaultTagBgColor2;
                self.TagBgImage2.enabled = self.DefaultTagBgImageEnabled2;
            }
        }

        private static string GetQualityBackgroundKey(int quality)
        {
            return quality switch
            {
                1 => ClientYooAssetConstTable.RogueOptionBgQuality1,
                2 => ClientYooAssetConstTable.RogueOptionBgQuality2,
                3 => ClientYooAssetConstTable.RogueOptionBgQuality3,
                _ => string.Empty,
            };
        }

        private static string GetTagQualityBackgroundKey(int quality)
        {
            return quality switch
            {
                1 => ClientYooAssetConstTable.RogueOptionTagBgQuality1,
                2 => ClientYooAssetConstTable.RogueOptionTagBgQuality2,
                3 => ClientYooAssetConstTable.RogueOptionTagBgQuality3,
                _ => string.Empty,
            };
        }

        private static void CacheTagBackgroundImages(RogueOptionComponent self)
        {
            self.TagBgImage1 ??= self.u_ComTagRectTransform?.GetComponent<Image>();
            self.TagBgImage2 ??= self.u_ComTag2RectTransform?.GetComponent<Image>();
        }

        private static void ApplyTagBackgroundSprite(RogueOptionComponent self, Sprite sprite)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            CacheTagBackgroundImages(self);

            if (self.TagBgImage1 != null)
            {
                self.TagBgImage1.sprite = sprite;
                self.TagBgImage1.enabled = sprite != null;
                self.TagBgImage1.color = Color.white;
            }

            if (self.TagBgImage2 != null)
            {
                self.TagBgImage2.sprite = sprite;
                self.TagBgImage2.enabled = sprite != null;
                self.TagBgImage2.color = Color.white;
            }
        }

        private static RectTransform ResolveOptionClickRoot(RogueOptionComponent self)
        {
            Transform ownerTransform = self.UIBase?.OwnerGameObject?.transform;
            RectTransform optionRootRect = ownerTransform?.Find("Option") as RectTransform;
            if (optionRootRect != null)
            {
                return optionRootRect;
            }

            return self.u_ComRogueOptionRectTransform;
        }

        private static Transform ResolveRerollButtonTransform(RogueOptionComponent self)
        {
            return self.UIBase?.OwnerGameObject?.transform?.Find("btnReroll");
        }
    }
}
