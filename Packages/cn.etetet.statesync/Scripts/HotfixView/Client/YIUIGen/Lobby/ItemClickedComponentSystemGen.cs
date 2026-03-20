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
    [EntitySystemOf(typeof(ItemClickedComponent))]
    public static partial class ItemClickedComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ItemClickedComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this ItemClickedComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this ItemClickedComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_ComImage = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComImage");
            self.u_ComEquipRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComEquipRectTransform");
            self.u_DataValue = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataValue");
            self.u_DataTitle = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataTitle");
            self.u_DataDesc = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataDesc");
            self.u_EventEquip = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventEquip");
            self.u_EventEquipHandle = self.u_EventEquip.Add(self,ItemClickedComponent.OnEventEquipInvoke);

        }
    }
}
