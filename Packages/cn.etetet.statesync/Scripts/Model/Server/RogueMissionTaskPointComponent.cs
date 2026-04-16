namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class RogueMissionTaskPointComponent : Entity, IAwake
    {
        public string PointId { get; set; }
        public long OwnerPlayerId { get; set; }
        public int RewardGold { get; set; }
        public int MonsterUnitConfigId { get; set; }
        public int SpawnCount { get; set; }
        public int RemainingMonsterCount { get; set; }
        public bool Started { get; set; }
        public bool Completed { get; set; }
    }
}
