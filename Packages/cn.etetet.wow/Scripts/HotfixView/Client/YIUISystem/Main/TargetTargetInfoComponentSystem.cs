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
    [FriendOf(typeof(TargetTargetInfoComponent))]
    [FriendOf(typeof(UnitInfoComponent))]
    public static partial class TargetTargetInfoComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this TargetTargetInfoComponent self)
        {
            self.SetActive(false, true);
            self.UIUnitInfo.u_EventClickInfo.Add(self, TargetTargetInfoComponent.OnEventClickInfoInvoke);
            var playerUnit = UnitHelper.GetMyUnitFromClientScene(self.Fiber().Root);
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
        private static void Destroy(this TargetTargetInfoComponent self)
        {
        }

        private static void SetActive(this TargetTargetInfoComponent self, bool active, bool force = false)
        {
            if (active == self.m_CurrentActiveState && !force)
            {
                return;
            }

            self.m_CurrentActiveState = active;
            self.UIBase.SetActive(active);
        }

        private static void UpdateTarget(this TargetTargetInfoComponent self, Unit target)
        {
            if (target == self.Unit) return;
            self.m_Unit = target;
            if (self.Unit == null) return;

            self.UIUnitInfo.u_DataLayoutAnim.SetValue("UnitInfoRight", true);
            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Unit.ConfigId);
            self.UIUnitInfo.RefreshUnitInfo(self.Unit);
        }

        [EntitySystem]
        private static void LateUpdate(this TargetTargetInfoComponent self)
        {
            if (self.TargetComponent == null) return;
            var target = self.TargetComponent.Target;
            if (target == null)
            {
                self.SetActive(false);
                return;
            }

            var targetTarget = self.TargetComponent.Target.GetComponent<TargetComponent>()?.Target;
            if (targetTarget == null)
            {
                self.SetActive(false);
                return;
            }

            self.SetActive(true);

            self.UpdateTarget(targetTarget);
        }

        #region YIUIEvent开始

        [YIUIInvoke(TargetTargetInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this TargetTargetInfoComponent self)
        {
            await ETTask.CompletedTask;
            Log.Info($"点击了目标的目标头像 {self.m_UnitConfig.Name}");
        }

        #endregion YIUIEvent结束
    }
}