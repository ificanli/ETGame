using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.14
    /// Desc
    /// </summary>
    public static partial class ActionBarSlotComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarSlotComponent self)
        {
            self.ResetInfo();
        }

        [EntitySystem]
        private static void Destroy(this ActionBarSlotComponent self)
        {
        }

        [EntitySystem]
        private static void LateUpdate(this ET.Client.ActionBarSlotComponent self)
        {
            if (self.u_DataId.GetValue() == 0) return;

            //检查公共CD
            long timeNow     = TimeInfo.Instance.ServerNow();
            long ggCDTime    = self.SpellComponent.CDTime + self.GGCD;
            var  hasGGCD     = ggCDTime > timeNow;
            long skillCDTime = 0;
            var  hasSkillCD  = false;

            if (self.SpellComponent.SpellCD.TryGetValue(self.m_SpellConfig.Id, out long cdTime))
            {
                skillCDTime = cdTime + self.m_SpellConfig.CD;
                hasSkillCD  = skillCDTime > timeNow; //有技能CD
            }

            //显示技能CD 不管大小 都显示技能CD
            if (hasSkillCD)
            {
                //TODO 技能系统还没有实现 技能在CD中被减少  所以这里没处理
                var residueTime = (float)(skillCDTime - timeNow);
                self.u_DataCountDown.SetValue(residueTime / 1000f);
                self.u_DataIconFill.SetValue(residueTime / self.m_SpellConfig.CD);
                return;
            }

            //显示公共CD   目前配置中还没有标识那些技能是不占GGCD的
            if (hasGGCD)
            {
                self.u_DataCountDown.SetValue(0); //ggcd 没有倒计时
                self.u_DataIconFill.SetValue((float)(ggCDTime - timeNow) / self.GGCD);
                return;
            }

            //没有任何CD
            self.u_DataCountDown.SetValue(0);
            self.u_DataIconFill.SetValue(0);
        }

        //临时做法 不应该使用技能ID绑定  根据WOW 这里可能是任何东西 技能/宏/等等
        public static void RefreshInfo(this ActionBarSlotComponent self, string hotKey, int skillId)
        {
            self.ResetInfo();
            self.u_DataHotKey.SetValue(hotKey);
            self.u_DataId.SetValue(skillId);
            if (skillId == 0)
            {
                return;
            }

            self.m_SpellConfig = SpellConfigCategory.Instance.Get(skillId);
            if (self.m_SpellConfig == null)
            {
                self.ResetInfo();
                Log.Error($"没找到这个技能配置: {skillId}");
                return;
            }

            var player = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (player == null)
            {
                self.ResetInfo();
                return;
            }

            self.m_Player         = player;
            self.m_Numeric        = player.GetComponent<NumericComponent>();
            self.m_SpellComponent = player.GetComponent<SpellComponent>();

            //设置技能图片
            self.u_DataIcon.SetValue(self.m_SpellConfig.Icon.Name);
        }

        private static void ResetInfo(this ActionBarSlotComponent self)
        {
            self.u_DataId.SetValue(0, true);
            self.u_DataHotKey.SetValue("", true);
            self.u_DataCharge.SetValue(0, true);
            self.u_DataCountDown.SetValue(0, true);
            self.u_DataIconFill.SetValue(0, true);
            self.m_SpellConfig = null;
            self.m_Player      = default;
            self.m_Numeric     = default;
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}