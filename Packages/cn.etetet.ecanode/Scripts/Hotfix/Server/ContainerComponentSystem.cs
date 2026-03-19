using System.Collections.Generic;
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
            self.OutputMode = ContainerOutputMode.ContainerPanel;
            self.LootGenerated = false;
            self.HasOpenedOnce = false;
            self.CreatorPlayerId = 0;
            self.CreateTime = 0;
            self.ItemEntries.Clear();
            self.SearchTimerIds.Clear();
            self.SearchStartTimes.Clear();
            self.SearchDurations.Clear();
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

        public static void SetSearchSession(this ContainerComponent self, long playerId, string timerId, long durationMs)
        {
            if (self == null || playerId == 0 || string.IsNullOrWhiteSpace(timerId))
            {
                return;
            }

            self.SearchTimerIds[playerId] = timerId;
            self.SearchStartTimes[playerId] = TimeInfo.Instance.ServerNow();
            self.SearchDurations[playerId] = durationMs;
        }

        public static void ClearSearchSession(this ContainerComponent self, long playerId)
        {
            if (self == null || playerId == 0)
            {
                return;
            }

            self.SearchTimerIds.Remove(playerId);
            self.SearchStartTimes.Remove(playerId);
            self.SearchDurations.Remove(playerId);
        }

        public static bool TryGetSearchTimerId(this ContainerComponent self, long playerId, out string timerId)
        {
            timerId = null;
            if (self == null || playerId == 0)
            {
                return false;
            }

            return self.SearchTimerIds.TryGetValue(playerId, out timerId) && !string.IsNullOrWhiteSpace(timerId);
        }

        public static bool TryGetSearchRemainMs(this ContainerComponent self, long playerId, out long remainMs)
        {
            remainMs = 0;
            if (self == null || playerId == 0)
            {
                return false;
            }

            if (!self.SearchStartTimes.TryGetValue(playerId, out long start))
            {
                return false;
            }

            if (!self.SearchDurations.TryGetValue(playerId, out long duration))
            {
                return false;
            }

            long now = TimeInfo.Instance.ServerNow();
            remainMs = duration - (now - start);
            if (remainMs < 0)
            {
                remainMs = 0;
            }
            return true;
        }

        public static bool IsSearching(this ContainerComponent self, long playerId)
        {
            if (self == null || playerId == 0)
            {
                return false;
            }

            return self.SearchTimerIds.ContainsKey(playerId);
        }

        public static void ClearItems(this ContainerComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.ItemEntries.Clear();
        }

        public static void SetItem(this ContainerComponent self, int slotIndex, int configId, int count)
        {
            if (self == null || slotIndex < 0 || configId <= 0 || count <= 0)
            {
                return;
            }

            self.ItemEntries[slotIndex] = new ContainerItemEntry
            {
                ConfigId = configId,
                Count = count
            };
        }

        public static bool TryGetItem(this ContainerComponent self, int slotIndex, out ContainerItemEntry item)
        {
            item = default;
            if (self == null || slotIndex < 0)
            {
                return false;
            }

            return self.ItemEntries.TryGetValue(slotIndex, out item);
        }

        public static void RemoveItem(this ContainerComponent self, int slotIndex)
        {
            if (self == null || slotIndex < 0)
            {
                return;
            }

            self.ItemEntries.Remove(slotIndex);
            if (self.ItemEntries.Count == 0)
            {
                self.State = ContainerState.Empty;
            }
        }

        public static bool HasAnyItem(this ContainerComponent self)
        {
            return self != null && self.ItemEntries.Count > 0;
        }

        public static void FillItemMessage(this ContainerComponent self, List<ContainerItemData> target)
        {
            target?.Clear();
            if (self == null || target == null || self.ItemEntries.Count == 0)
            {
                return;
            }

            List<int> keys = new List<int>(self.ItemEntries.Keys);
            keys.Sort();
            foreach (int slotIndex in keys)
            {
                ContainerItemEntry entry = self.ItemEntries[slotIndex];
                if (entry.ConfigId <= 0 || entry.Count <= 0)
                {
                    continue;
                }

                ContainerItemData item = ContainerItemData.Create();
                item.SlotIndex = slotIndex;
                item.ConfigId = entry.ConfigId;
                item.Count = entry.Count;
                target.Add(item);
            }
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
