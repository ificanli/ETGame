namespace ET
{
    public struct Effect
    {
        public EffectConfig Config { get; }
        public Entity Owner { get; }
        public int EffectTimeType { get; }
        
        public Effect(EffectConfig config, Entity owner, int effectTimeType)
        {
            Config = config;
            Owner = owner;
            EffectTimeType = effectTimeType;
        }
    }
}