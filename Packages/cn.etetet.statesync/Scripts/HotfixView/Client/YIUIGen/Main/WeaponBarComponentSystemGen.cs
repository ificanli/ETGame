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
    [EntitySystemOf(typeof(WeaponBarComponent))]
    public static partial class WeaponBarComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WeaponBarComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this WeaponBarComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this WeaponBarComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_ComWeaponItemRectTransform = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComWeaponItemRectTransform");

        }
    }
}
