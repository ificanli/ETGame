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
    [EntitySystemOf(typeof(EquipSlotItemComponent))]
    public static partial class EquipSlotItemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EquipSlotItemComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this EquipSlotItemComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this EquipSlotItemComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataSlotName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataSlotName");
            self.u_DataEquipName = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataEquipName");
            self.u_DataIsEmpty = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataIsEmpty");

        }
    }
}
