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
            self.u_DataTextTitle = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataTextTitle");
            self.u_DataTextDes = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataTextDes");
            self.u_EventClickReroll = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickReroll");
            self.u_EventClickRerollHandle = self.u_EventClickReroll.Add(self,RogueOptionComponent.OnEventClickRerollInvoke);
            self.u_EventClickRogueOption = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickRogueOption");
            self.u_EventClickRogueOptionHandle = self.u_EventClickRogueOption.Add(self,RogueOptionComponent.OnEventClickRogueOptionInvoke);

        }
    }
}
