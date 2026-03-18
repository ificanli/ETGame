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
    [FriendOf(typeof(HPViewComponent))]
    public static partial class HPViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HPViewComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HPViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this HPViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
