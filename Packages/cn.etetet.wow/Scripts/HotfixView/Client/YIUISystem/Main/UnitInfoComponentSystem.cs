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
    [FriendOf(typeof(UnitInfoComponent))]
    public static partial class UnitInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this UnitInfoComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this UnitInfoComponent self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(UnitInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this UnitInfoComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
