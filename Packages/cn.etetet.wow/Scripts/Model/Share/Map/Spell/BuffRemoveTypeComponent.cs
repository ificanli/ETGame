namespace ET
{
    [Module(ModuleName.Spell)]
    [ComponentOf(typeof(Buff))]
    public class BuffRemoveTypeComponent: Entity, IAwake
    {
        public BuffFlags BuffRemoveType { get; set; }
    }
}