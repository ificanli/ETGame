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
    [EntitySystemOf(typeof(PlayerInfoComponent))]
    public static partial class PlayerInfoComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerInfoComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this PlayerInfoComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this PlayerInfoComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataName");
            self.u_DataHP = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataHP");
            self.u_DataMaxHP = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataMaxHP");
            self.u_DataMP = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataMP");
            self.u_DataMaxMP = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataMaxMP");
            self.u_DataIcon = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataIcon");
            self.u_DataHPRatio = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataHPRatio");
            self.u_DataMPRatio = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataMPRatio");
            self.u_EventClickInfo = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickInfo");
            self.u_EventClickInfoHandle = self.u_EventClickInfo.Add(self,PlayerInfoComponent.OnEventClickInfoInvoke);

        }
    }
}
