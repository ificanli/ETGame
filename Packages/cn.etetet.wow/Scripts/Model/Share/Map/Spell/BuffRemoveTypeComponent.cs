namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class BuffRemoveTypeComponent: Entity, IAwake
    {
        public BuffRemoveType BuffRemoveType { get; set; }
    }
}