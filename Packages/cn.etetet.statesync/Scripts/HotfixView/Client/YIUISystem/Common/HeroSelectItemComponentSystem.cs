using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.2.28
    /// Desc
    /// </summary>
    [FriendOf(typeof(HeroSelectItemComponent))]
    public static partial class HeroSelectItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HeroSelectItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HeroSelectItemComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(HeroSelectItemComponent.OnEventSelectInvoke)]
        private static void OnEventSelectInvoke(this HeroSelectItemComponent self)
        {

        }
        #endregion YIUIEvent结束
    }
}
