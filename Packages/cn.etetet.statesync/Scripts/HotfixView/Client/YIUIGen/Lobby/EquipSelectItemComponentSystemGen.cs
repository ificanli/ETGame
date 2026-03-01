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
    [EntitySystemOf(typeof(EquipSelectItemComponent))]
    public static partial class EquipSelectItemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EquipSelectItemComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this EquipSelectItemComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this EquipSelectItemComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataSelect = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataSelect");
            self.u_DataEquipName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataEquipName");
            self.u_EventSelect = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventSelect");
            self.u_EventSelectHandle = self.u_EventSelect.Add(self,EquipSelectItemComponent.OnEventSelectInvoke);

        }
    }
}
