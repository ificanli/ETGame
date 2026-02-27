using ET;

namespace ET.Server
{
    [EntitySystemOf(typeof(SpawnPointComponent))]
    [FriendOf(typeof(SpawnPointComponent))]
    public static partial class SpawnPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpawnPointComponent self, string pointId)
        {
            self.PointId = pointId;
        }

        public static SpawnPointComponent GetOrAdd(ECAPointComponent point)
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

            SpawnPointComponent spawnPoint = unit.GetComponent<SpawnPointComponent>();
            if (spawnPoint == null)
            {
                spawnPoint = unit.AddComponent<SpawnPointComponent, string>(point.PointId);
            }

            return spawnPoint;
        }

        public static void RecordSpawnRequest(this SpawnPointComponent self, string groupId, int count)
        {
            if (self == null)
            {
                return;
            }

            self.LastGroupId = groupId;
            self.LastSpawnCount = count;
            self.LastRequestTime = TimeInfo.Instance.ServerNow();
        }
    }
}
