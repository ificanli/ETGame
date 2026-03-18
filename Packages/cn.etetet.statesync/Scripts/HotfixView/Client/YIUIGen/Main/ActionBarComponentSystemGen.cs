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


        }
    }
}
