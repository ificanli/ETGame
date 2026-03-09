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
            ReleaseSprite(self);
            if (self.IconImage != null)
            {
                self.IconImage.enabled = false;
            }
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
    }
}
