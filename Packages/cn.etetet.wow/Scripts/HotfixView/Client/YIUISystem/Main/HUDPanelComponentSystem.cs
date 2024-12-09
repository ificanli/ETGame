using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.9
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
            self.m_Unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (self.Unit == null)
            {
                Log.Error($"没有找到玩家实体");
                return false;
            }

            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Unit.ConfigId);
            return true;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
