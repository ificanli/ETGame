namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class ContainerComponent : Entity, IAwake<string>
    {
        public string PointId { get; set; }
        public int State { get; set; }
        public string LastLootTable { get; set; }
        public int LastDropCount { get; set; }
        public float LastDropRadius { get; set; }
        public long LastRequestTime { get; set; }
        public long LastRequestPlayerId { get; set; }
    }
}
