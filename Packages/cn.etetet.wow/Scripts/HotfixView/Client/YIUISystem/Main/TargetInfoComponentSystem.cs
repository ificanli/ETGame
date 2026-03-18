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
    [FriendOf(typeof(TargetInfoComponent))]
    public static partial class TargetInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this TargetInfoComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this TargetInfoComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(TargetInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this TargetInfoComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
