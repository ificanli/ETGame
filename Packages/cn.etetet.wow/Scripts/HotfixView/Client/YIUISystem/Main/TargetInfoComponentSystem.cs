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
    public static partial class TargetInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this TargetInfoComponent self)
        {
            self.SetActive(false, true);
            self.UIUnitInfo.u_EventClickInfo.Add(self, TargetInfoComponent.OnEventClickInfoInvoke);
            var playerUnit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (playerUnit == null)
            {
                Log.Error($"没有找到玩家实体");
                return;
            }

            self.m_TargetComponent = playerUnit.GetComponent<TargetComponent>();
            if (self.TargetComponent == null)
            {
                Log.Error($"没有找到目标组件");
                return;
            }
        }

        [EntitySystem]
        private static void Destroy(this TargetInfoComponent self)
        {
        }

        private static void SetActive(this TargetInfoComponent self, bool active, bool force = false)
        {
            if (active == self.m_CurrentActiveState && !force)
            {
                return;
            }

            self.m_CurrentActiveState = active;
            self.UIBase.SetActive(active);
        }

        private static void UpdateTarget(this TargetInfoComponent self, Unit target)
        {
            if (target == self.Unit) return;
            self.m_Unit = target;
            if (self.Unit == null) return;

            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Unit.ConfigId);
            self.UIUnitInfo.RefreshUnitInfo(self.Unit);
        }

        [EntitySystem]
        private static void LateUpdate(this TargetInfoComponent self)
        {
            if (self.TargetComponent == null) return;
            var target = self.TargetComponent.Unit;
            if (target == null)
            {
                self.SetActive(false);
                return;
            }

            self.SetActive(true);

            self.UpdateTarget(target);
        }

        #region YIUIEvent开始

        [YIUIInvoke(TargetInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this TargetInfoComponent self)
        {
            await ETTask.CompletedTask;
            if (self.Unit == null) return;
            Log.Info($"点击了目标的头像 {self.m_UnitConfig.Name},{self.Unit.Id}");
        }

        #endregion YIUIEvent结束
    }
}