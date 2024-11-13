namespace ET
{
    [EntitySystemOf(typeof(SpellComponent))]
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ET.SpellComponent self)
        {

        }
        
        public static Spell CreateSpell(this SpellComponent self, SpellConfig config, long id, long parentSpellId = 0)
        {
            Spell spell = self.AddChildWithId<Spell, SpellConfig>(id, config);
            return spell;
        }

        public static void RemoveSpell(this SpellComponent self, long spellId)
        {
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