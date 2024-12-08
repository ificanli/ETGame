using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.8
    /// Desc
    /// </summary>
    [FriendOf(typeof(TargetInfoComponent))]
    [FriendOf(typeof(UnitInfoComponent))]
    public static partial class TargetInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this TargetInfoComponent self)
        {
            self.UIBase.SetActive(false);
            self.UIUnitInfo.u_EventClickInfo.Add(self, TargetInfoComponent.OnEventClickInfoInvoke);
        }

        [EntitySystem]
        private static void Destroy(this TargetInfoComponent self)
        {
        }

        private static void UpdateInfo(this TargetInfoComponent self)
        {
            self.m_Unit = UnitHelper.GetMyUnitFromClientScene(self.Fiber().Root);
            if (self.Unit == null)
            {
                Log.Error($"没有找到目标实体");
                return;
            }

            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Unit.ConfigId);
            self.UIUnitInfo.RefreshUnitInfo(self.Unit);
        }

        #region YIUIEvent开始

        [YIUIInvoke(TargetInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this TargetInfoComponent self)
        {
            await ETTask.CompletedTask;
            Log.Info($"点击了目标的头像 {self.m_UnitConfig.Name}");
        }

        #endregion YIUIEvent结束
    }
}