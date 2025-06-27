using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.7
    /// Desc
    /// </summary>
    public static partial class PlayerInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this PlayerInfoComponent self)
        {
            self.UpdateInfo();
            self.UIUnitInfo.u_EventClickInfo.Add(self, PlayerInfoComponent.OnEventClickInfoInvoke);
        }

        [EntitySystem]
        private static void Destroy(this PlayerInfoComponent self)
        {
        }

        private static void UpdateInfo(this PlayerInfoComponent self)
        {
            self.m_Unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (self.Unit == null)
            {
                Log.Error($"没有找到玩家实体");
                return;
            }

            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Unit.ConfigId);
            self.UIUnitInfo.RefreshUnitInfo(self.Unit);
        }

        #region YIUIEvent开始

        [YIUIInvoke(PlayerInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this PlayerInfoComponent self)
        {
            await ETTask.CompletedTask;
            if (self.Unit == null) return;
            Log.Info($"点击了自己的头像 {self.m_UnitConfig.Name},{self.Unit.Id}");
        }

        #endregion YIUIEvent结束
    }
}