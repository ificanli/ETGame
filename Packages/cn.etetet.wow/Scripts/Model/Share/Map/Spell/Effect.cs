namespace ET
{
    [ChildOf]
    public class Effect: Entity, IAwake
    {
        public BTTimeType BtTimeType { get; set; }
        
        public EffectConfig EffectConfig { get; set; }
    }
}