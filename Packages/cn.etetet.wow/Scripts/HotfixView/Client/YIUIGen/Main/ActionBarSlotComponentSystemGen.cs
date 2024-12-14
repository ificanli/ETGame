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
    [EntitySystemOf(typeof(ActionBarSlotComponent))]
    public static partial class ActionBarSlotComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ActionBarSlotComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this ActionBarSlotComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this ActionBarSlotComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataHotKey = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataHotKey");
            self.u_DataCharge = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataCharge");
            self.u_DataIcon = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataIcon");
            self.u_DataCountDown = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataCountDown");
            self.u_DataIconFill = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueFloat>("u_DataIconFill");
            self.u_DataId = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueInt>("u_DataId");

        }
    }
}
