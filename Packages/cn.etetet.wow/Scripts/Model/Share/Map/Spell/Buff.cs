namespace ET
{
    public static partial class TimerInvokeType
    {
        public const int BuffTimeoutTimer = PackageType.WOW * 1000 + 1;
    }
    
    [Module(ModuleName.Spell)]
    [ChildOf(typeof(BuffComponent))]
    public class Buff: Entity, IAwake<int>
    {
        public int ConfigId { get; set; }
        public long Caster { get; set; }
        public long CreateTime { get; set; }
        public int TickTime { get; set; }
        public long ExpireTime { get; set; }
        public int Stack { get; set; }

        public long TimeoutTimer { get; set; }
    }
}