using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [FriendOf(typeof(YIUIChild))]
    [EntitySystemOf(typeof(RogueOptionComponent))]
    public static partial class RogueOptionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueOptionComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this RogueOptionComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this RogueOptionComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_ComImageRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComImageRectTransform");
            self.u_ComRogueOptionRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComRogueOptionRectTransform");
            self.u_ComTagsDescViewRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTagsDescViewRectTransform");
            self.u_ComTagRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTagRectTransform");
            self.u_ComTag2RectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComTag2RectTransform");
            self.u_DataTextTitle = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataTextTitle");
            self.u_DataTextDes = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataTextDes");
            self.u_DataTagDes = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataTagDes");
            self.u_EventClickReroll = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickReroll");
            self.u_EventClickRerollHandle = self.u_EventClickReroll.Add(self,RogueOptionComponent.OnEventClickRerollInvoke);
            self.u_EventClickRogueOption = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickRogueOption");
            self.u_EventClickRogueOptionHandle = self.u_EventClickRogueOption.Add(self,RogueOptionComponent.OnEventClickRogueOptionInvoke);
            self.u_EventClickTags = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickTags");
            self.u_EventClickTagsHandle = self.u_EventClickTags.Add(self,RogueOptionComponent.OnEventClickTagsInvoke);

        }
    }
}
