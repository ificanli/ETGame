using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
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
            self.u_DataTextTitle?.SetValue(optionData.Name ?? string.Empty);
            self.u_DataTextDes?.SetValue(optionData.Desc ?? string.Empty);
            ConfigureClickRaycastTargets(self);
            SetTags(self, CollectDisplayTags(optionData));
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
            self.u_DataTextTitle?.SetValue(string.Empty);
            self.u_DataTextDes?.SetValue(string.Empty);
            ConfigureClickRaycastTargets(self);
            SetTags(self, System.Array.Empty<int>());
            ReleaseSprite(self);
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

        public static void ApplyQualityStyle(RectTransform optionRect, int quality)
        {
            if (optionRect == null)
            {
                return;
            }

            Image optionImage = optionRect.GetComponent<Image>();
            if (optionImage == null)
            {
                return;
            }

            optionImage.color = quality switch
            {
                1 => new Color32(70, 130, 255, 255),
                2 => new Color32(160, 90, 255, 255),
                3 => new Color32(255, 150, 40, 255),
                _ => Color.white,
            };
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
    }
}
