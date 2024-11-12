namespace ET
{
    public static partial class SpellComponentSystem
    {
        public static Spell CreateSpell(this SpellComponent self, SpellConfig config, long id, long parentSpellId = 0)
        {
            Spell spell = self.AddChildWithId<Spell, SpellConfig>(id, config);
            return spell;
        }
        
        public static void RemoveSpell(this SpellComponent self, long spellId)
        {
            self.RemoveChild(spellId);
        }
    }
}