using System;
using UnityEngine;
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
            self.IsChoosing = false;
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

            self.u_EventClickRogueOptionHandle = null;
            self.u_EventClickRogueOption = null;
            self.u_EventClickRerollHandle = null;
            self.u_EventClickReroll = null;
            RogueOptionViewHelper.ReleaseSprite(self);
            self.IconImage = null;
            self.Panel = null;
            self.OptionIndex = -1;
            self.IsChoosing = false;
        }

        [YIUIInvoke(RogueOptionComponent.OnEventClickRogueOptionInvoke)]
        private static async ETTask OnEventClickRogueOptionInvoke(this RogueOptionComponent self)
        {
            await self.TryChooseCurrentOption();
        }

        [YIUIInvoke(RogueOptionComponent.OnEventClickRerollInvoke)]
        private static async ETTask OnEventClickRerollInvoke(this RogueOptionComponent self)
        {
            await self.TryChooseCurrentOption();
        }

        private static async ETTask TryChooseCurrentOption(this RogueOptionComponent self)
        {
            if (self.IsChoosing)
            {
                await ETTask.CompletedTask;
                return;
            }

            RoguePanelComponent panel = self.Panel;
            if (panel == null || panel.IsDisposed || self.OptionIndex < 0)
            {
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
    }
}
