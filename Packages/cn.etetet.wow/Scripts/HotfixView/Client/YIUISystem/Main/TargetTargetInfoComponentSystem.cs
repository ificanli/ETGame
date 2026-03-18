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
    [FriendOf(typeof(TargetTargetInfoComponent))]
    public static partial class TargetTargetInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this TargetTargetInfoComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this TargetTargetInfoComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(TargetTargetInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this TargetTargetInfoComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
