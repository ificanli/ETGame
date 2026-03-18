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
    [FriendOf(typeof(BagViewComponent))]
    public static partial class BagViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this BagViewComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BagViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this BagViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
