namespace ET
{
    [EntitySystemOf(typeof(Spell))]
    public static partial class SpellSystem
    {
        [EntitySystem]
        public static void Awake(this Spell self, int configId)
        {
            self.ConfigId = configId;
        }
        
        public static SpellConfig GetConfig(this Spell self)
        {
            return SpellConfigCategory.Instance.Get(self.ConfigId);
        } 
    }
}