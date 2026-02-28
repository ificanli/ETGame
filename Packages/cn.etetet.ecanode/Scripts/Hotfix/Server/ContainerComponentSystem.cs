using ET;

namespace ET.Server
{
    [EntitySystemOf(typeof(ContainerComponent))]
    [FriendOf(typeof(ContainerComponent))]
    public static partial class ContainerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ContainerComponent self, string pointId)
        {
            self.PointId = pointId;
            self.State = ContainerState.Closed;
        }

        public static ContainerComponent GetOrAdd(ECAPointComponent point)
        {
            if (point == null)
            {
                return null;
            }

            Unit unit = point.GetParent<Unit>();
            if (unit == null)
            {
                return null;
            }

            ContainerComponent container = unit.GetComponent<ContainerComponent>();
            if (container == null)
            {
                container = unit.AddComponent<ContainerComponent, string>(point.PointId);
            }

            return container;
        }

        public static void RecordSpawnItems(this ContainerComponent self, string lootTable, int count, float radius, long playerId)
        {
            if (self == null)
            {
                return;
            }

            self.LastLootTable = lootTable;
            self.LastDropCount = count;
            self.LastDropRadius = radius;
            self.LastRequestTime = TimeInfo.Instance.ServerNow();
            self.LastRequestPlayerId = playerId;
        }
    }
}
