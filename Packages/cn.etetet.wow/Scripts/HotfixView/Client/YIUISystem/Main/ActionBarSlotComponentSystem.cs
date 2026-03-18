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
    [FriendOf(typeof(ActionBarSlotComponent))]
    public static partial class ActionBarSlotComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarSlotComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ActionBarSlotComponent self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
