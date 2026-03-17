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
            self.InitialItemsGranted = false;
            self.GrantInitialWarehouseItems();
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

                self.AddWarehouseItem(item.ConfigId, item.Count);
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

            int totalCount = 0;
            for (int i = 0; i < self.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = self.WarehouseItems[i];
                if (item.ConfigId != configId || item.Count <= 0)
                {
                    continue;
                }

                totalCount += item.Count;
            }

            return totalCount;
        }

        public static bool TryConsumeWarehouseItem(this PlayerStorageComponent self, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return false;
            }

            if (self.GetWarehouseCount(configId) < count)
            {
                return false;
            }

            int remain = count;
            for (int i = 0; i < self.WarehouseItems.Count && remain > 0;)
            {
                LoadoutWarehouseItemInfo item = self.WarehouseItems[i];
                if (item.ConfigId != configId || item.Count <= 0)
                {
                    ++i;
                    continue;
                }

                if (item.Count <= remain)
                {
                    remain -= item.Count;
                    self.WarehouseItems.RemoveAt(i);
                    continue;
                }

                item.Count -= remain;
                self.WarehouseItems[i] = item;
                remain = 0;
                break;
            }

            return remain == 0;
        }

        public static void AddWarehouseItem(this PlayerStorageComponent self, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return;
            }

            if (!TryResolveWarehouseItemMetrics(configId, out int maxStack, out int gridWidth, out int gridHeight))
            {
                return;
            }

            int remain = count;
            if (maxStack > 1)
            {
                for (int i = 0; i < self.WarehouseItems.Count && remain > 0; ++i)
                {
                    LoadoutWarehouseItemInfo item = self.WarehouseItems[i];
                    if (item.ConfigId != configId || item.Count <= 0 || item.Count >= maxStack)
                    {
                        continue;
                    }

                    int canAdd = maxStack - item.Count;
                    int addCount = remain < canAdd ? remain : canAdd;
                    item.Count += addCount;
                    self.WarehouseItems[i] = item;
                    remain -= addCount;
                }
            }

            while (remain > 0)
            {
                int stackCount = maxStack > 0 && remain > maxStack ? maxStack : remain;
                self.WarehouseItems.Add(new LoadoutWarehouseItemInfo
                {
                    ItemUid = IdGenerater.Instance.GenerateId(),
                    ConfigId = configId,
                    Count = stackCount,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                });
                remain -= stackCount;
            }
        }

        public static void GrantInitialWarehouseItems(this PlayerStorageComponent self)
        {
            if (self.InitialItemsGranted)
            {
                return;
            }

            HashSet<int> grantedConfigIds = new();

            AddFirstItemByPredicate(grantedConfigIds, static item => item.CanEquipMainWeapon || item.CanEquipSubWeapon);
            AddFirstItemByPredicate(grantedConfigIds, static item => item.CanEquipArmor);
            AddFirstItemByPredicate(grantedConfigIds, static item => item.CanEquipBackpack || item.IsBackpack);
            AddFirstItemByPredicate(grantedConfigIds, static item => item.LoadoutShopCategory == 4);
            AddFirstItemByPredicate(grantedConfigIds, static item => item.Type == 3 || item.LoadoutShopCategory == 5);

            HashSet<int> seenTypes = new();
            foreach (ItemConfig itemConfig in ItemConfigCategory.Instance.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0)
                {
                    continue;
                }

                if (!seenTypes.Add(itemConfig.Type))
                {
                    continue;
                }

                grantedConfigIds.Add(itemConfig.Id);
            }

            foreach (int configId in grantedConfigIds)
            {
                self.AddWarehouseItem(configId, 1);
            }

            self.InitialItemsGranted = true;
        }

        private static void AddFirstItemByPredicate(HashSet<int> grantedConfigIds, System.Func<ItemConfig, bool> predicate)
        {
            foreach (ItemConfig itemConfig in ItemConfigCategory.Instance.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0)
                {
                    continue;
                }

                if (!predicate(itemConfig))
                {
                    continue;
                }

                grantedConfigIds.Add(itemConfig.Id);
                return;
            }
        }

        private static bool TryResolveWarehouseItemMetrics(int configId, out int maxStack, out int gridWidth, out int gridHeight)
        {
            maxStack = 1;
            gridWidth = LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
            gridHeight = LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                maxStack = itemConfig.MaxStack > 0 ? itemConfig.MaxStack : 1;
                gridWidth = itemConfig.GridWidth > 0 ? itemConfig.GridWidth : LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
                gridHeight = itemConfig.GridHeight > 0 ? itemConfig.GridHeight : LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
                return true;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            return equipConfig != null;
        }
    }
}
