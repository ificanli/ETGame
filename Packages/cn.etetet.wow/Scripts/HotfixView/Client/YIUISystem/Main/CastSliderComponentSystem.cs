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
    [FriendOf(typeof(CastSliderComponent))]
    public static partial class CastSliderComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this CastSliderComponent self)
        {
            self.m_Player = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (self.Player == null)
            {
                Log.Error($"没有找到玩家实体");
                return;
            }

            self.u_DataProgress.SetValue(0);
            self.SetActive(false, true);
        }

        [EntitySystem]
        private static void Destroy(this CastSliderComponent self)
        {
        }

        private static void SetActive(this CastSliderComponent self, bool active, bool force = false)
        {
            if (active == self.m_CurrentActiveState && !force)
            {
                return;
            }

            self.m_CurrentActiveState = active;
            self.UIBase.SetActive(active);
        }

        [EntitySystem]
        private static async ETTask DynamicEvent(this CastSliderComponent self, BTEvent_ShowCastSlider data)
        {
            if (data.Unit != self.Player) return;

            self.m_CurrentBuff = data.Buff;
            if (self.CurrentBuff == null)
            {
                Log.Error($"CastSliderComponentSystem:DynamicEvent:Buff is null");
                return;
            }

            self.m_IsIncrease = data.IsIncrease;
            self.u_DataName.SetValue(data.ShowDisplayName);
            self.u_DataIcon.SetValue(data.IconName);
            await ETTask.CompletedTask;
        }

        [EntitySystem]
        private static void LateUpdate(this CastSliderComponent self)
        {
            if (self.CurrentBuff == null)
            {
                self.SetActive(false);
                return;
            }

            self.SetActive(true);

            var startTime = self.CurrentBuff.CreateTime;
            var endTime   = self.CurrentBuff.ExpireTime;
            var duration  = endTime - startTime;
            if (duration <= 0)
            {
                self.SetActive(false);
                return;
            }

            long serverNow  = TimeInfo.Instance.ServerNow();
            long passedTime = serverNow - startTime;
            if (passedTime < 0)
            {
                self.SetActive(false);
                return;
            }

            var progress     = (float)passedTime / duration;
            var showProgress = self.m_IsIncrease ? progress : 1 - progress;
            self.u_DataProgress.SetValue(showProgress);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}