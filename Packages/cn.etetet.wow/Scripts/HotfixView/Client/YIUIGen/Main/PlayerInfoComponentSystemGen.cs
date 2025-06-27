using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
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

            self.u_EventClickInfo = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickInfo");
            self.u_EventClickInfoHandle = self.u_EventClickInfo.Add(self,PlayerInfoComponent.OnEventClickInfoInvoke);
            self.u_UIUnitInfo = self.UIBase.CDETable.FindUIOwner<ET.Client.UnitInfoComponent>("UnitInfo");

        }
    }
}
