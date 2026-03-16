namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class SpawnPointComponent : Entity, IAwake<string>
    {
        public string PointId { get; set; }
        public string LastGroupId { get; set; }
        public int LastSpawnCount { get; set; }
        public long LastRequestTime { get; set; }
    }
}
