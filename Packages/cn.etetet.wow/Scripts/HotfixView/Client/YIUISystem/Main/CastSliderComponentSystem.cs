using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.17
    /// Desc
    /// </summary>
    [FriendOf(typeof(CastSliderComponent))]
    public static partial class CastSliderComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this CastSliderComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this CastSliderComponent self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
