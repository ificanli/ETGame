using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public static partial class ActionBarSlotComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarSlotComponent self)
        {
            self.BindClickView();
            self.ResetInfo();
        }

        [EntitySystem]
        private static void Destroy(this ActionBarSlotComponent self)
        {
            GameObject ownerGameObject = self.UIBase?.OwnerGameObject;
            if (ownerGameObject == null)
            {
                return;
            }

            ActionBarSlotClickView clickView = ownerGameObject.GetComponent<ActionBarSlotClickView>();
            clickView?.ClearEntity();
        }

        [EntitySystem]
        private static void LateUpdate(this ET.Client.ActionBarSlotComponent self)
        {
            if (self.u_DataId.GetValue() == 0 || self.m_SpellConfig == null || self.SpellComponent == null) return;

            long timeNow = TimeInfo.Instance.ServerNow();
            long ggCDTime = self.SpellComponent.CDTime + self.GGCD;
            bool hasGGCD = ggCDTime > timeNow;
            long skillCDTime = 0;
            bool hasSkillCD = false;

            if (self.SpellComponent.SpellCD.TryGetValue(self.m_SpellConfig.Id, out long cdTime))
            {
                skillCDTime = cdTime + self.m_SpellConfig.CD;
                hasSkillCD = skillCDTime > timeNow;
            }

            if (hasSkillCD)
            {
                float residueTime = skillCDTime - timeNow;
                self.u_DataCountDown.SetValue(residueTime / 1000f);
                self.u_DataIconFill.SetValue(residueTime / self.m_SpellConfig.CD);
                return;
            }

            if (hasGGCD)
            {
                self.u_DataCountDown.SetValue(0);
                self.u_DataIconFill.SetValue((float)(ggCDTime - timeNow) / self.GGCD);
                return;
            }

            self.u_DataCountDown.SetValue(0);
            self.u_DataIconFill.SetValue(0);
        }

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
                Log.Error($"skill config not found: {skillId}");
                return;
            }

            Unit player = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (player == null)
            {
                self.ResetInfo();
                return;
            }

            self.m_Player = player;
            self.m_Numeric = player.NumericComponent;
            self.m_SpellComponent = player.GetComponent<SpellComponent>();
            self.u_DataIcon.SetValue(self.m_SpellConfig.Icon.Name);
        }

        public static void TryTriggerSpell(this ActionBarSlotComponent self)
        {
            int spellConfigId = self.u_DataId?.GetValue() ?? 0;
            if (spellConfigId <= 0)
            {
                return;
            }

            Unit player = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            if (player == null || player.IsDisposed)
            {
                Log.Warning($"[ActionBar] trigger spell failed: player invalid, spellConfigId={spellConfigId}");
                return;
            }

            EventSystem.Instance.Publish(self.Scene(), new OnSpellTrigger
            {
                Unit = player,
                SpellConfigId = spellConfigId,
            });
        }

        private static void OnClickTriggerSpell(ActionBarSlotComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.TryTriggerSpell();
        }

        private static void BindClickView(this ActionBarSlotComponent self)
        {
            GameObject ownerGameObject = self.UIBase?.OwnerGameObject;
            if (ownerGameObject == null)
            {
                return;
            }

            ActionBarSlotClickView clickView = ownerGameObject.GetComponent<ActionBarSlotClickView>();
            if (clickView == null)
            {
                clickView = ownerGameObject.AddComponent<ActionBarSlotClickView>();
            }

            clickView.SetEntity(self, OnClickTriggerSpell);
        }

        private static void ResetInfo(this ActionBarSlotComponent self)
        {
            self.u_DataId.SetValue(0, true);
            self.u_DataHotKey.SetValue("", true);
            self.u_DataCharge.SetValue(0, true);
            self.u_DataCountDown.SetValue(0, true);
            self.u_DataIconFill.SetValue(0, true);
            self.m_SpellConfig = null;
            self.m_Player = default;
            self.m_Numeric = default;
            self.m_SpellComponent = default;
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
