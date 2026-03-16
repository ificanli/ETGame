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
    [FriendOf(typeof(MatchViewComponent))]
    public static partial class MatchViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this MatchViewComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this MatchViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MatchViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
