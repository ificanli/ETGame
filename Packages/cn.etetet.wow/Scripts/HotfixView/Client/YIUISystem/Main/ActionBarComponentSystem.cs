using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.2
    /// Desc
    /// </summary>
    [FriendOf(typeof(ActionBarComponent))]
    public static partial class ActionBarComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ActionBarComponent self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
