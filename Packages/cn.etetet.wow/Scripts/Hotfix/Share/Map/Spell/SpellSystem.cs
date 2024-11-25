namespace ET
{
    [EntitySystemOf(typeof(Spell))]
    public static partial class SpellSystem
    {
        [EntitySystem]
        private static void Awake(this Spell self, int spellConfigId)
        {
            self.ConfigId = spellConfigId;
        }
        
        public static SpellConfig GetConfig(this Spell self)
        {
            return SpellConfigCategory.Instance.Get(self.ConfigId);
        } 
        
        public static Unit GetCaster(this Spell self)
        {
            return self.Scene().GetComponent<UnitComponent>().Get(self.Caster);
        } 
    }
}