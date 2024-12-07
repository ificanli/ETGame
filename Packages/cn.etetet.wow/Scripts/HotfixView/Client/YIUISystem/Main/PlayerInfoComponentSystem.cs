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
    [FriendOf(typeof(PlayerInfoComponent))]
    public static partial class PlayerInfoComponentSystem
    {
        [EntitySystem]
        private static async ETTask DynamicEvent(this ET.Client.PlayerInfoComponent self, ET.NumbericChange param1)
        {
            if (param1.Unit != self.Player) return;
            await ETTask.CompletedTask;

            //临时处理全监听
            self.UpdateHP();
            self.UpdateMP();
        }

        [EntitySystem]
        private static void YIUIInitialize(this PlayerInfoComponent self)
        {
            self.UpdateInfo();
        }

        [EntitySystem]
        private static void Destroy(this PlayerInfoComponent self)
        {
        }

        private static void UpdateInfo(this PlayerInfoComponent self)
        {
            self.m_Player = UnitHelper.GetMyUnitFromClientScene(self.Fiber().Root);
            if (self.Player == null)
            {
                Log.Error($"没有找到玩家实体");
                return;
            }

            self.m_Numeric = self.Player.GetComponent<NumericComponent>();
            if (self.Numeric == null)
            {
                Log.Error($"没有找到玩家的NumericComponent");
                return;
            }

            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Player.ConfigId);
            self.u_DataName.SetValue(self.m_UnitConfig.Name);

            self.UpdateHP();
            self.UpdateMP();
        }

        private static void UpdateHP(this PlayerInfoComponent self)
        {
            if (self.Numeric == null) return;
            var hp    = self.Numeric.GetAsInt(NumericType.HP);
            var maxHP = self.Numeric.GetAsInt(NumericType.MaxHP);
            self.u_DataHP.SetValue(hp);
            self.u_DataMaxHP.SetValue(maxHP);
            var ratio = maxHP <= 0 ? 0 : (float)hp / maxHP;
            self.u_DataHPRatio.SetValue(ratio);
        }

        private static void UpdateMP(this PlayerInfoComponent self)
        {
            if (self.Numeric == null) return;
            var mp    = self.Numeric.GetAsInt(NumericType.MP);
            var maxMP = self.Numeric.GetAsInt(NumericType.MaxMP);
            self.u_DataMP.SetValue(mp);
            self.u_DataMaxMP.SetValue(maxMP);
            var ratio = maxMP <= 0 ? 0 : (float)mp / maxMP;
            self.u_DataMPRatio.SetValue(ratio);
        }

        #region YIUIEvent开始

        [YIUIInvoke(PlayerInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this PlayerInfoComponent self)
        {
            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束
    }
}