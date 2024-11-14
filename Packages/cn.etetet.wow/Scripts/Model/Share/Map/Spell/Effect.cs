namespace ET
{
    [ChildOf]
    public class Effect: Entity, IAwake
    {
        public EffectTimeType EffectTimeType { get; set; }
        
        public EffectConfig EffectConfig { get; set; }
    }
}