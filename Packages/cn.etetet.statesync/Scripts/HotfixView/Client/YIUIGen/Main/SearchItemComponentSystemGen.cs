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
    [EntitySystemOf(typeof(SearchItemComponent))]
    public static partial class SearchItemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SearchItemComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SearchItemComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SearchItemComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();


        }
    }
}
