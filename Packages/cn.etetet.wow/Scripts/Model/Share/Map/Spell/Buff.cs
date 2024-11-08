namespace ET
{
    [ChildOf(typeof(BuffComponent))]
    public class Buff: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id
        
        public EntityRef<Unit> Caster { get; set; }
    }
}