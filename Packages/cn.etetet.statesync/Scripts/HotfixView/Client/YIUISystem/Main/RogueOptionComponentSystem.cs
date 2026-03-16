using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.6
    /// Desc
    /// </summary>
    [FriendOf(typeof(RogueOptionComponent))]
    public static partial class RogueOptionComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this RogueOptionComponent self)
        {
            self.IconImage = self.u_ComImageRectTransform?.GetComponent<Image>();
            self.TagText1 = self.u_ComTagRectTransform?.GetComponentInChildren<TMPro.TMP_Text>(true);
            self.TagText2 = self.u_ComTag2RectTransform?.GetComponentInChildren<TMPro.TMP_Text>(true);
            self.IsChoosing = false;
            RogueOptionViewHelper.ConfigureClickRaycastTargets(self);
            RogueOptionViewHelper.SetTags(self, Array.Empty<int>());
        }

        [EntitySystem]
        private static void Destroy(this RogueOptionComponent self)
        {
            if (self.u_EventClickRogueOption != null && self.u_EventClickRogueOptionHandle != null)
            {
                self.u_EventClickRogueOption.Remove(self.u_EventClickRogueOptionHandle);
            }

            if (self.u_EventClickReroll != null && self.u_EventClickRerollHandle != null)
            {
                self.u_EventClickReroll.Remove(self.u_EventClickRerollHandle);
            }

            if (self.u_EventClickTags != null && self.u_EventClickTagsHandle != null)
            {
                self.u_EventClickTags.Remove(self.u_EventClickTagsHandle);
            }

            self.u_EventClickRogueOptionHandle = null;
            self.u_EventClickRogueOption = null;
            self.u_EventClickRerollHandle = null;
            self.u_EventClickReroll = null;
            self.u_EventClickTagsHandle = null;
            self.u_EventClickTags = null;
            RogueOptionViewHelper.ReleaseSprite(self);
            self.IconImage = null;
            self.TagText1 = null;
            self.TagText2 = null;
            self.Panel = null;
            self.OptionIndex = -1;
            self.IsChoosing = false;
            self.DisplayTagIds = Array.Empty<int>();
            self.CurrentTagId = 0;
            self.IsTagDescVisible = false;
        }

        [YIUIInvoke(RogueOptionComponent.OnEventClickRogueOptionInvoke)]
        private static async ETTask OnEventClickRogueOptionInvoke(this RogueOptionComponent self)
        {
            Log.Info($"[RogueClient] option area click: index={self.OptionIndex}, choosing={self.IsChoosing}, hasPanel={self.Panel != null}");
            await self.TryChooseCurrentOption();
        }

        [YIUIInvoke(RogueOptionComponent.OnEventClickRerollInvoke)]
        private static async ETTask OnEventClickRerollInvoke(this RogueOptionComponent self)
        {
            Log.Info($"[RogueClient] reroll click: index={self.OptionIndex}, choosing={self.IsChoosing}, hasPanel={self.Panel != null}");
            await self.TryChooseCurrentOption();
        }

        [YIUIInvoke(RogueOptionComponent.OnEventClickTagsInvoke)]
        private static async ETTask OnEventClickTagsInvoke(this RogueOptionComponent self)
        {
            RogueOptionViewHelper.ToggleTagDesc(self, self.GetClickedTagIndex());
            await ETTask.CompletedTask;
        }

        private static async ETTask TryChooseCurrentOption(this RogueOptionComponent self)
        {
            if (self.IsChoosing)
            {
                Log.Info($"[RogueClient] option click ignored while choosing: index={self.OptionIndex}");
                await ETTask.CompletedTask;
                return;
            }

            RoguePanelComponent panel = self.Panel;
            if (panel == null || panel.IsDisposed || self.OptionIndex < 0)
            {
                Log.Warning(
                    $"[RogueClient] option click ignored: index={self.OptionIndex}, hasPanel={panel != null}, panelDisposed={panel?.IsDisposed ?? false}");
                await ETTask.CompletedTask;
                return;
            }

            self.IsChoosing = true;
            EntityRef<RogueOptionComponent> selfRef = self;
            await RoguePanelChoiceHelper.TryChooseOption(panel, self.OptionIndex);

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.IsChoosing = false;
        }

        private static int GetClickedTagIndex(this RogueOptionComponent self)
        {
            GameObject clickedObject = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            Transform clickedTransform = clickedObject?.transform;
            if (clickedTransform == null)
            {
                return self.DisplayTagIds.Length <= 1 ? 0 : -1;
            }

            if (self.u_ComTagRectTransform != null && clickedTransform.IsChildOf(self.u_ComTagRectTransform))
            {
                return 0;
            }

            if (self.u_ComTag2RectTransform != null && clickedTransform.IsChildOf(self.u_ComTag2RectTransform))
            {
                return 1;
            }

            return self.DisplayTagIds.Length <= 1 ? 0 : -1;
        }
    }
}
