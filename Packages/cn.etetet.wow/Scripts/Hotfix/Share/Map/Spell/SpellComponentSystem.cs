namespace ET
{
    [EntitySystemOf(typeof(SpellComponent))]
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpellComponent self)
        {

        }
        
        public static Spell CreateSpell(this SpellComponent self, SpellConfig config, long id, long parentSpellId = 0)
        {
            Spell spell = self.AddChildWithId<Spell, SpellConfig>(id, config);

            foreach (SpellFlags spellFlags in config.Flags)
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
            foreach (SpellFlags flag in spell.Config.Flags)
            {
                self.flagSpells.Remove((int)flag, spell);
            }
            self.RemoveChild(spellId);
        }

        public static bool CheckCD(this SpellComponent self, int spellConfigId)
        {
            long timeNow = TimeInfo.Instance.FrameTime;
            if (self.CDTime + 2000 > timeNow)
            {
                return false;
            }

            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            if (self.SpellCD.TryGetValue(spellConfigId, out long cdTime))
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