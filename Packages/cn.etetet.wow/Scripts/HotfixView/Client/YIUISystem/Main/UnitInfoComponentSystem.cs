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
    public static partial class UnitInfoComponentSystem
    {
        [EntitySystem]
        private static async ETTask DynamicEvent(this UnitInfoComponent self, NumbericChange data)
        {
            if (self.Unit == null || data.Unit == null) return;
            if (data.Unit != self.Unit) return;

            //临时处理全监听
            self.UpdateHP();
            self.UpdateMP();

            await ETTask.CompletedTask;
        }

        [EntitySystem]
        private static void YIUIInitialize(this UnitInfoComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this UnitInfoComponent self)
        {
        }

        public static void RefreshUnitInfo(this UnitInfoComponent self, Unit unit)
        {
            self.m_Unit = unit;
            if (self.Unit == null)
            {
                self.m_Numeric = default;
                return;
            }

            self.m_Numeric = self.Unit.GetComponent<NumericComponent>();
            if (self.Numeric == null)
            {
                Log.Error($"没有找到目标的NumericComponent  {self.Unit.ConfigId},{self.Unit.Config().Name}");
                return;
            }

            self.UpdateInfo();
        }

        private static void UpdateInfo(this UnitInfoComponent self)
        {
            self.m_UnitConfig = UnitConfigCategory.Instance.Get(self.Unit.ConfigId);
            self.u_DataName.SetValue(self.m_UnitConfig.Name);
            self.u_DataIcon.SetValue(self.m_UnitConfig.HeadIcon);
            var classType = self.m_UnitConfig.ClassType;
            self.u_DataShowClass.SetValue(classType != EClassType.None);
            self.u_DataClass.SetValue(classType.ToString());

            self.UpdateHP();
            self.UpdateMP();
        }

        private static void UpdateHP(this UnitInfoComponent self)
        {
            if (self.Numeric == null) return;
            var hp    = self.Numeric.GetAsInt(NumericType.HP);
            var maxHP = self.Numeric.GetAsInt(NumericType.MaxHP);
            self.u_DataHP.SetValue(hp);
            self.u_DataMaxHP.SetValue(maxHP);
            var ratio = maxHP <= 0 ? 0 : (float)hp / maxHP;
            self.u_DataHPRatio.SetValue(ratio);
        }

        private static void UpdateMP(this UnitInfoComponent self)
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

        [YIUIInvoke(UnitInfoComponent.OnEventClickInfoInvoke)]
        private static async ETTask OnEventClickInfoInvoke(this UnitInfoComponent self)
        {
            await ETTask.CompletedTask;
            if (self.Unit == null)
            {
                return;
            }

            var player = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (player == null)
            {
                return;
            }

            var targetComponent = player.GetComponent<TargetComponent>();

            if (self.Unit == targetComponent.Unit)
            {
                return;
            }
            
            UnitClickHelper.Click(self.Root(), self.Unit.Id).NoContext();
        }

        #endregion YIUIEvent结束
    }
}