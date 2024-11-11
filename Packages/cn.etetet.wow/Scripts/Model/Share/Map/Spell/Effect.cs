namespace ET
{
    public struct Effect
    {
        public Entity Source { get; set; }
        public EffectTimeType EffectTimeType { get; }
        
        public Effect(Entity source, EffectTimeType effectTimeType)
        {
            this.Source = source;
            EffectTimeType = effectTimeType;
        }
    }
}