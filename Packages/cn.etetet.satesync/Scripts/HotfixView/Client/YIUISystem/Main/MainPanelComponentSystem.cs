using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.2
    /// Desc
    /// </summary>
    [FriendOf(typeof(MainPanelComponent))]
    public static partial class MainPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this MainPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this MainPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this MainPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(MainPanelComponent.OnEventClickSearchingButtonInvoke)]
        private static async ETTask OnEventClickSearchingButtonInvoke(this MainPanelComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        
        [YIUIInvoke(MainPanelComponent.OnEventClickBagButtonInvoke)]
        private static async ETTask OnEventClickBagButtonInvoke(this MainPanelComponent self)
        {
            
            await ETTask.CompletedTask;
        }
        #endregion YIUIEvent结束
    }
}
