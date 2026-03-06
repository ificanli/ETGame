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
    [EntitySystemOf(typeof(WeaponItemComponent))]
    public static partial class WeaponItemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WeaponItemComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this WeaponItemComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this WeaponItemComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_EventClick = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventClick");
            self.u_EventClickHandle = self.u_EventClick.Add(self,WeaponItemComponent.OnEventClickInvoke);

        }
    }
}
