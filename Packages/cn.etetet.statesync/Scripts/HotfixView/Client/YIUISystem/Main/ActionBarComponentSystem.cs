using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public static partial class ActionBarComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this ActionBarComponent self)
        {
            self.RefreshHeroSkillSlot();
        }

        [EntitySystem]
        private static void Destroy(this ActionBarComponent self)
        {
        }

        private static void RefreshHeroSkillSlot(this ActionBarComponent self)
        {
            LoadoutComponent loadout = self.Root().GetComponent<LoadoutComponent>();
            int heroConfigId = loadout?.SelectedHeroConfigId ?? 0;
            if (heroConfigId == 0 && loadout != null && loadout.Heroes.Count > 0)
            {
                heroConfigId = loadout.Heroes[0].HeroConfigId;
            }

            if (heroConfigId == 0)
            {
                self.UISlot12.RefreshInfo("1", 0);
                Log.Warning("[ActionBar] no selected hero config, clear skill slot");
                return;
            }

            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            if (heroConfig == null)
            {
                self.UISlot12.RefreshInfo("1", 0);
                Log.Warning($"[ActionBar] hero config not found: heroConfigId={heroConfigId}");
                return;
            }

            self.UISlot12.RefreshInfo("1", heroConfig.SkillConfigId);
            Log.Info($"[ActionBar] bind hero skill: heroConfigId={heroConfigId}, skillConfigId={heroConfig.SkillConfigId}");
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}