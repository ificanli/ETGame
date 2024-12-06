namespace ET
{
    [EntitySystemOf(typeof(SpellComponent))]
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpellComponent self)
        {

        }
        
        public static Spell CreateSpell(this SpellComponent self, long spellId, int configId)
        {
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(configId);
            Spell spell = self.AddChildWithId<Spell, int>(spellId, configId);
            spell.CreateTime = TimeInfo.Instance.FrameTime;

            foreach (SpellFlags spellFlags in spellConfig.Flags)
            {
                self.flagSpells.Add((int)spellFlags, spell);
            }
            
            return spell;
        }

        public static Spell GetSpell(this SpellComponent self, long spellId)
        {
            return self.GetChild<Spell>(spellId);
        }

        public static void RemoveSpell(this SpellComponent self, long spellId)
        {
            Spell spell = self.GetChild<Spell>(spellId);
            if (spell == null)
            {
                return;
            }
            foreach (SpellFlags flag in spell.GetConfig().Flags)
            {
                self.flagSpells.Remove((int)flag, spell);
            }
            self.RemoveChild(spellId);
        }

        public static bool CheckCD(this SpellComponent self, SpellConfig spellConfig)
        {
            long timeNow = TimeInfo.Instance.FrameTime;
            if (self.CDTime + 2000 > timeNow)
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

        public static void UpdateCD(this SpellComponent self, int spellConfigId)
        {
            long timeNow = TimeInfo.Instance.FrameTime;
            self.CDTime = timeNow;
            self.SpellCD[spellConfigId] = timeNow;
        }
    }
}