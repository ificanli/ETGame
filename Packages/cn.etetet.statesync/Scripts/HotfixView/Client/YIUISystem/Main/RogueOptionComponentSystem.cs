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
            Transform ownerTransform = self.UIBase?.OwnerGameObject?.transform;
            Transform rerollTransform = ownerTransform?.Find("btnReroll");
            self.IconImage = self.u_ComImageRectTransform?.GetComponent<Image>();
            self.OptionBgImage = self.u_ComRogueOptionRectTransform?.GetComponent<Image>();
            self.RerollButton = rerollTransform?.GetComponent<Button>();
            self.RerollButtonImage = rerollTransform?.GetComponent<Image>();
            self.TagBgImage1 = self.u_ComTagRectTransform?.GetComponent<Image>();
            self.TagBgImage2 = self.u_ComTag2RectTransform?.GetComponent<Image>();
            self.DefaultBgSprite = self.OptionBgImage?.sprite;
            self.DefaultBgColor = self.OptionBgImage?.color ?? Color.white;
            self.DefaultBgImageEnabled = self.OptionBgImage?.enabled ?? true;
            self.DefaultRerollButtonColor = self.RerollButtonImage?.color ?? Color.white;
            self.DefaultRerollButtonImageEnabled = self.RerollButtonImage?.enabled ?? true;
            self.DefaultTagBgSprite1 = self.TagBgImage1?.sprite;
            self.DefaultTagBgSprite2 = self.TagBgImage2?.sprite;
            self.DefaultTagBgColor1 = self.TagBgImage1?.color ?? Color.white;
            self.DefaultTagBgColor2 = self.TagBgImage2?.color ?? Color.white;
            self.DefaultTagBgImageEnabled1 = self.TagBgImage1?.enabled ?? true;
            self.DefaultTagBgImageEnabled2 = self.TagBgImage2?.enabled ?? true;
            self.TagText1 = self.u_ComTagRectTransform?.GetComponentInChildren<TMPro.TMP_Text>(true);
            self.TagText2 = self.u_ComTag2RectTransform?.GetComponentInChildren<TMPro.TMP_Text>(true);
            self.IsChoosing = false;
            self.IsRerolling = false;
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
            RogueOptionViewHelper.ReleaseBackgroundSprite(self);
            RogueOptionViewHelper.ReleaseTagBackgroundSprite(self);
            self.IconImage = null;
            self.OptionBgImage = null;
            self.RerollButton = null;
            self.RerollButtonImage = null;
            self.TagBgImage1 = null;
            self.TagBgImage2 = null;
            self.DefaultBgSprite = null;
            self.DefaultBgColor = Color.white;
            self.DefaultBgImageEnabled = true;
            self.DefaultRerollButtonColor = Color.white;
            self.DefaultRerollButtonImageEnabled = true;
            self.DefaultTagBgSprite1 = null;
            self.DefaultTagBgSprite2 = null;
            self.DefaultTagBgColor1 = Color.white;
            self.DefaultTagBgColor2 = Color.white;
            self.DefaultTagBgImageEnabled1 = true;
            self.DefaultTagBgImageEnabled2 = true;
            self.TagText1 = null;
            self.TagText2 = null;
            self.Panel = null;
            self.OptionIndex = -1;
            self.IsChoosing = false;
            self.IsRerolling = false;
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
            Log.Info(
                $"[RogueClient] reroll click: index={self.OptionIndex}, choosing={self.IsChoosing}, rerolling={self.IsRerolling}, hasPanel={self.Panel != null}");
            await self.TryRerollCurrentOption();
        }

        [YIUIInvoke(RogueOptionComponent.OnEventClickTagsInvoke)]
        private static async ETTask OnEventClickTagsInvoke(this RogueOptionComponent self)
        {
            RogueOptionViewHelper.ToggleTagDesc(self, self.GetClickedTagIndex());
            await ETTask.CompletedTask;
        }

        private static async ETTask TryChooseCurrentOption(this RogueOptionComponent self)
        {
            if (self.IsChoosing || self.IsRerolling)
            {
                Log.Info(
                    $"[RogueClient] option click ignored while busy: index={self.OptionIndex}, choosing={self.IsChoosing}, rerolling={self.IsRerolling}");
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

        private static async ETTask TryRerollCurrentOption(this RogueOptionComponent self)
        {
            if (self.IsChoosing || self.IsRerolling)
            {
                Log.Info(
                    $"[RogueClient] reroll ignored while busy: index={self.OptionIndex}, choosing={self.IsChoosing}, rerolling={self.IsRerolling}");
                await ETTask.CompletedTask;
                return;
            }

            RoguePanelComponent panel = self.Panel;
            if (panel == null || panel.IsDisposed || self.OptionIndex < 0)
            {
                Log.Warning(
                    $"[RogueClient] reroll ignored: index={self.OptionIndex}, hasPanel={panel != null}, panelDisposed={panel?.IsDisposed ?? false}");
                await ETTask.CompletedTask;
                return;
            }

            Scene root = panel.Root();
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null || self.OptionIndex >= runtime.ChoiceOptions.Count)
            {
                Log.Warning(
                    $"[RogueClient] reroll blocked: index={self.OptionIndex}, serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}");
                await ETTask.CompletedTask;
                return;
            }

            int currentOptionId = runtime.ChoiceOptions[self.OptionIndex].OptionId;
            if (currentOptionId <= 0)
            {
                Log.Warning(
                    $"[RogueClient] reroll blocked by invalid option id: index={self.OptionIndex}, serial={runtime.ChoiceSerial}, optionId={currentOptionId}");
                await ETTask.CompletedTask;
                return;
            }

            int rerollCount = runtime.ChoiceOptions[self.OptionIndex].RerollCount;
            if (rerollCount <= 0)
            {
                RogueOptionViewHelper.RefreshRerollState(self, rerollCount);
                Log.Info(
                    $"[RogueClient] reroll blocked by reroll count: index={self.OptionIndex}, serial={runtime.ChoiceSerial}, optionId={currentOptionId}, rerollCount={rerollCount}");
                await ETTask.CompletedTask;
                return;
            }

            self.IsRerolling = true;
            EntityRef<RogueOptionComponent> selfRef = self;
            await RoguePanelChoiceHelper.TryRerollOption(panel, self.OptionIndex, currentOptionId);

            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.IsRerolling = false;
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
