namespace ET
{
    [ChildOf(typeof(BuffComponent))]
    public class Buff: Entity, IAwake<int>
    {
        public int ConfigId { get; set; }
        public long Caster;
        public long Source;
        public long CreateTime { get; set; }
    }
}