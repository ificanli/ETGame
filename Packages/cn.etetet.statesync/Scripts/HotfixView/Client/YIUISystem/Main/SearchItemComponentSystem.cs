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
    [FriendOf(typeof(SearchItemComponent))]
    public static partial class SearchItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SearchItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SearchItemComponent self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
