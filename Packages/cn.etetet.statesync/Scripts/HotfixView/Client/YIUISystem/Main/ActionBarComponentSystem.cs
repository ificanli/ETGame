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



            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(heroConfigId);
            if (heroConfig == null)
            {
                return;
            }

        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}