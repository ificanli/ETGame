using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [EntitySystemOf(typeof(ActionBarComponent))]
    public static partial class ActionBarComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ActionBarComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this ActionBarComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this ActionBarComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_UISlot12 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot12");
            self.u_UISlot11 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot11");
            self.u_UISlot10 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot10");
            self.u_UISlot9 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot9");
            self.u_UISlot8 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot8");
            self.u_UISlot7 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot7");
            self.u_UISlot6 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot6");
            self.u_UISlot5 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot5");
            self.u_UISlot4 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot4");
            self.u_UISlot3 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot3");
            self.u_UISlot2 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot2");
            self.u_UISlot1 = self.UIBase.CDETable.FindUIOwner<ET.Client.ActionBarSlotComponent>("Slot1");

        }
    }
}
