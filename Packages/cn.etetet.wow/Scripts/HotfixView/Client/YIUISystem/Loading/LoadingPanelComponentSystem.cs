using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2024.10.28
    /// Desc
    /// </summary>
    [FriendOf(typeof(LoadingPanelComponent))]
    public static partial class LoadingPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this LoadingPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this LoadingPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LoadingPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
