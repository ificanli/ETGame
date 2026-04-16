namespace ET.Server
{
    [EntitySystemOf(typeof(RogueMissionTaskPointComponent))]
    [FriendOf(typeof(RogueMissionTaskPointComponent))]
    public static partial class RogueMissionTaskPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueMissionTaskPointComponent self)
        {
            self.PointId = string.Empty;
            self.OwnerPlayerId = 0;
            self.RewardGold = 0;
            self.MonsterUnitConfigId = 0;
            self.SpawnCount = 0;
            self.RemainingMonsterCount = 0;
            self.Started = false;
            self.Completed = false;
        }
    }

    [EntitySystemOf(typeof(RogueMissionTaskMonsterComponent))]
    [FriendOf(typeof(RogueMissionTaskMonsterComponent))]
    public static partial class RogueMissionTaskMonsterComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueMissionTaskMonsterComponent self)
        {
            self.TaskPointUnitId = 0;
            self.OwnerPlayerId = 0;
        }
    }
}
