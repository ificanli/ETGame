using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [EntitySystemOf(typeof(TargetTargetInfoComponent))]
    public static partial class TargetTargetInfoComponentSystem
    {
        [EntitySystem]
        private static void Awake(this TargetTargetInfoComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this TargetTargetInfoComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this TargetTargetInfoComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_EventClickInfo = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClickInfo");
            self.u_EventClickInfoHandle = self.u_EventClickInfo.Add(self,TargetTargetInfoComponent.OnEventClickInfoInvoke);
            self.u_UIUnitInfo = self.UIBase.CDETable.FindUIOwner<ET.Client.UnitInfoComponent>("UnitInfo");

        }
    }
}
