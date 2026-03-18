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
    [FriendOf(typeof(PlayerInfoComponent))]
    public static partial class PlayerInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this PlayerInfoComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this PlayerInfoComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(PlayerInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this PlayerInfoComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
