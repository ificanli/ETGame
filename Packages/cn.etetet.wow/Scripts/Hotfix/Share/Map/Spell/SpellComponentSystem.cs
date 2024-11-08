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
        
        public static void RemoveSpell(this SpellComponent self, long spellId)
        {
            Spell currentSpell = self.Current;
            if (currentSpell.Id == spellId)
            {
                foreach (var kv in self.Children)
                {
                    kv.Value.Dispose();
                }

                self.Current = default;
            }
            
            self.RemoveChild(spellId);
        }
    }
}