namespace ET
{
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        public static void Awake()
        {
            
        }

        public static Spell CreateSpell(this SpellComponent self, int spellConfigId)
        {
            Spell spell = self.AddChild<Spell, int>(spellConfigId);
            return spell;
        }
    }
}