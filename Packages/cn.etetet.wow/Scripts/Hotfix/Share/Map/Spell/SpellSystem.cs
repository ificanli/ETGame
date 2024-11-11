namespace ET
{
    [EntitySystemOf(typeof(Spell))]
    public static partial class SpellSystem
    {
        [EntitySystem]
        public static void Awake(this Spell self, SpellConfig config)
        {
            self.Config = config;
        }
        
        public static Unit GetCaster(this Spell self)
        {
            return self.Parent.GetParent<Unit>();
        } 
    }
}