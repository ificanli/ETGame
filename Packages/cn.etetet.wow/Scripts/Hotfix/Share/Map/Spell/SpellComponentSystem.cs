namespace ET
{
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        public static void Awake()
        {
            
        }

        public static Spell CreateSpell(this SpellComponent self, int configId)
        {
            Spell spell = self.AddChild<Spell, int>(configId);
            return spell;
        }
    }
}