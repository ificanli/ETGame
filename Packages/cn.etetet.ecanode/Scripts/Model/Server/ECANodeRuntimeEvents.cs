namespace ET.Server
{
    [EnableClass]
    public class ContainerLootBuildContext
    {
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
        public string LootTable;
        public int Count;
        public bool AllowRepeat;
        public int ResultRollMultiplierPermille = 1000;
    }

    public struct ContainerLootBuildEvent
    {
        public ContainerLootBuildContext Context;
    }

    public struct ContainerOpenedEvent
    {
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
        public bool IsFirstOpen;
    }

    [EnableClass]
    public class SearchMonsterSpawnContext
    {
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
        public string GroupId;
        public int SpawnCount;
        public int ChancePermille = 1000;
    }

    public struct SearchMonsterSpawnEvent
    {
        public SearchMonsterSpawnContext Context;
    }

    public struct DoorKeyConsumedEvent
    {
        public EntityRef<Unit> Player;
        public int ItemConfigId;
        public int Count;
    }
}
