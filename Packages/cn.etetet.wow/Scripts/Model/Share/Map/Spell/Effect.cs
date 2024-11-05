namespace ET
{
    public enum EffectTimeType
    {
        SpellHit = 1,
        SpellRemove = 2,
        BuffAdd = 3,
        BuffRemove = 4,
        BuffTick = 5
    }
    
    public struct Effect
    {
        public EffectConfig Config { get; }
        public Entity Owner { get; }
        public EffectTimeType EffectTimeType { get; }
        
        public Effect(EffectConfig config, Entity owner, EffectTimeType effectTimeType)
        {
            Config = config;
            Owner = owner;
            EffectTimeType = effectTimeType;
        }
    }
}