using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(SpellComponent))]
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpellComponent self)
        {

        }

        public static bool CheckCD(this SpellComponent self, SpellConfig spellConfig)
        {
            long timeNow = TimeInfo.Instance.ServerNow();
            if (self.CDTime + 500 > timeNow)  // 公共CD
            {
                return false;
            }

            if (self.SpellCD.TryGetValue(spellConfig.Id, out long cdTime))
            {
                if (cdTime + spellConfig.CD > timeNow)
                {
                    return false;
                }
            }
            return true;
        }

        public static void UpdateCD(this SpellComponent self, int spellConfigId, long time)
        {
            self.CDTime = time;
            self.SpellCD[spellConfigId] = time;
        }

        public static int GetMod(this SpellComponent self, int spellConfigId, SpellModType spellModType)
        {
            if (!self.SpellMods.TryGetValue(spellConfigId, out Dictionary<int, int> mods))
            {
                return 0;
            }
            return mods.GetValueOrDefault((int)spellModType, 0);
        }
        
        public static void AddMod(this SpellComponent self, int spellConfigId, SpellModType spellModType, int value)
        {
            if (value == 0)
            {
                return;
            }
            
            if (!self.SpellMods.TryGetValue(spellConfigId, out Dictionary<int, int> mods))
            {
                mods = new Dictionary<int, int>();
                self.SpellMods[spellConfigId] = mods;
            }


            int modType = (int)spellModType;
            if (!mods.TryGetValue(modType, out int oldValue))
            {
                mods[modType] = value;
                return;
            }
            
            value = oldValue + value;
            if (value != 0)
            {
                mods[modType] = value;
                return;
            }

            mods.Remove(modType);
            if (mods.Count == 0)
            {
                self.SpellMods.Remove(spellConfigId);
            }
        }
    }
}