namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class RunTimeLimitClientComponent : Entity, IAwake
    {
        public bool IsActive { get; set; }
        public long RemainMs { get; set; }
        public long EndTimeMs { get; set; }
    }
}
