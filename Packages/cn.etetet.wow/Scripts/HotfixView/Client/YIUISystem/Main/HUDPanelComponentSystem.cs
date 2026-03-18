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
    [FriendOf(typeof(HUDPanelComponent))]
    public static partial class HUDPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this HUDPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HUDPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this HUDPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
