namespace ET
{
    [ChildOf(typeof(BuffComponent))]
    public class Buff: Entity, IAwake<BuffConfig>
    {
        public BuffConfig Config { get; set; }
        
        public EntityRef<Unit> Caster { get; set; }
    }
}