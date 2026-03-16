using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerStorageComponent))]
    public static partial class PlayerStorageComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerStorageComponent self)
        {
            self.WarehouseItems.Clear();
            self.LastEvacuationItems.Clear();
            self.LastEvacuationWealth = 0;
            self.TotalWealth = 0;
        }

        [EntitySystem]
        private static void Destroy(this PlayerStorageComponent self)
        {
            self.WarehouseItems.Clear();
            self.LastEvacuationItems.Clear();
        }

        /// <summary>
        /// 写入撤离结算数据
        /// </summary>
        public static void WriteEvacuationResult(this PlayerStorageComponent self, M2C_EvacuationSettlement settlement)
        {
            self.LastEvacuationItems.Clear();
            self.LastEvacuationWealth = settlement.TotalWealth;
            self.TotalWealth += settlement.TotalWealth;

            foreach (ItemData item in settlement.Items)
            {
                if (self.LastEvacuationItems.TryGetValue(item.ConfigId, out int existing))
                {
                    self.LastEvacuationItems[item.ConfigId] = existing + item.Count;
                }
                else
                {
                    self.LastEvacuationItems[item.ConfigId] = item.Count;
                }

                if (self.WarehouseItems.TryGetValue(item.ConfigId, out int warehouseCount))
                {
                    self.WarehouseItems[item.ConfigId] = warehouseCount + item.Count;
                }
                else
                {
                    self.WarehouseItems[item.ConfigId] = item.Count;
                }
            }

            Log.Info($"[PlayerStorage] wrote evacuation result: {self.LastEvacuationItems.Count} item types, wealth={self.LastEvacuationWealth}");
        }

        /// <summary>
        /// 仅记录撤离摘要与财富，不自动写入仓库。
        /// </summary>
        public static void RecordEvacuationSummary(this PlayerStorageComponent self, Dictionary<int, int> summaryItems, long totalWealthDelta)
        {
            self.LastEvacuationItems.Clear();
            self.LastEvacuationWealth = totalWealthDelta;
            self.TotalWealth += totalWealthDelta;

            if (summaryItems == null)
            {
                return;
            }

            foreach (var kv in summaryItems)
            {
                if (kv.Key <= 0 || kv.Value <= 0)
                {
                    continue;
                }

                self.LastEvacuationItems[kv.Key] = kv.Value;
            }
        }

        public static int GetWarehouseCount(this PlayerStorageComponent self, int configId)
        {
            if (configId <= 0)
            {
                return 0;
            }

            return self.WarehouseItems.TryGetValue(configId, out int count) ? count : 0;
        }

        public static bool TryConsumeWarehouseItem(this PlayerStorageComponent self, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return false;
            }

            if (!self.WarehouseItems.TryGetValue(configId, out int current) || current < count)
            {
                return false;
            }

            int remain = current - count;
            if (remain > 0)
            {
                self.WarehouseItems[configId] = remain;
            }
            else
            {
                self.WarehouseItems.Remove(configId);
            }

            return true;
        }

        public static void AddWarehouseItem(this PlayerStorageComponent self, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return;
            }

            if (self.WarehouseItems.TryGetValue(configId, out int current))
            {
                self.WarehouseItems[configId] = current + count;
            }
            else
            {
                self.WarehouseItems[configId] = count;
            }
        }
    }
}
