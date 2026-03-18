using System;
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
            self.WarehouseColumnCount = 0;
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
            self.WarehouseColumnCount = 0;
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

        public static bool TryGetWarehouseItem(this PlayerStorageComponent self, long itemUid, out int index, out LoadoutWarehouseItemInfo item)
        {
            index = -1;
            item = default;
            if (itemUid <= 0)
            {
                return false;
            }

            for (int i = 0; i < self.WarehouseItems.Count; ++i)
            {
                if (self.WarehouseItems[i].ItemUid != itemUid)
                {
                    continue;
                }

                index = i;
                item = self.WarehouseItems[i];
                return true;
            }

            return false;
        }

        public static bool TryTakeWarehouseItem(this PlayerStorageComponent self, long itemUid, int count, out LoadoutWarehouseItemInfo item, out string message)
        {
            item = default;
            message = string.Empty;
            if (!self.TryGetWarehouseItem(itemUid, out int index, out item))
            {
                message = "warehouse item not found";
                return false;
            }

            if (count <= 0 || count > item.Count)
            {
                message = "count invalid";
                return false;
            }

            if (count == item.Count)
            {
                self.WarehouseItems.RemoveAt(index);
                return true;
            }

            item.Count -= count;
            self.WarehouseItems[index] = item;
            return true;
        }

        public static bool EnsureWarehouseLayout(this PlayerStorageComponent self, int suggestedColumnCount)
        {
            int normalizedColumnCount = NormalizeWarehouseColumnCount(suggestedColumnCount);
            if (self.WarehouseColumnCount <= 0 && normalizedColumnCount > 0)
            {
                self.WarehouseColumnCount = normalizedColumnCount;
            }
            else if (normalizedColumnCount > self.WarehouseColumnCount)
            {
                self.WarehouseColumnCount = normalizedColumnCount;
                for (int i = 0; i < self.WarehouseItems.Count; ++i)
                {
                    LoadoutWarehouseItemInfo item = self.WarehouseItems[i];
                    item.AnchorSlotIndex = -1;
                    self.WarehouseItems[i] = item;
                }
            }

            if (self.WarehouseColumnCount <= 0)
            {
                return false;
            }

            self.NormalizeWarehouseLayout();
            return true;
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

            List<GridPlacementItemInfo> placements = null;
            if (self.WarehouseColumnCount > 0)
            {
                self.NormalizeWarehouseLayout();
                placements = BuildWarehousePlacements(self.WarehouseItems, self.WarehouseColumnCount);
            }

            while (remain > 0)
            {
                int stackCount = maxStack > 0 && remain > maxStack ? maxStack : remain;
                int anchorSlotIndex = -1;
                if (self.WarehouseColumnCount > 0)
                {
                    anchorSlotIndex = FindWarehouseAnchorSlot(placements, self.WarehouseColumnCount, gridWidth, gridHeight);
                    placements.Add(new GridPlacementItemInfo
                    {
                        ConfigId = configId,
                        Count = stackCount,
                        AnchorSlotIndex = anchorSlotIndex,
                        GridWidth = gridWidth,
                        GridHeight = gridHeight,
                    });
                }

                self.WarehouseItems.Add(new LoadoutWarehouseItemInfo
                {
                    ItemUid = IdGenerater.Instance.GenerateId(),
                    ConfigId = configId,
                    Count = stackCount,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                    AnchorSlotIndex = anchorSlotIndex,
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

        private static int NormalizeWarehouseColumnCount(int columnCount)
        {
            return columnCount > 0 ? columnCount : 0;
        }

        private static void NormalizeWarehouseLayout(this PlayerStorageComponent self)
        {
            if (self.WarehouseColumnCount <= 0)
            {
                return;
            }

            List<GridPlacementItemInfo> placements = new();
            for (int i = 0; i < self.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = self.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                if (TryResolveWarehouseItemMetrics(item.ConfigId, out _, out int configuredGridWidth, out int configuredGridHeight))
                {
                    item.GridWidth = configuredGridWidth;
                    item.GridHeight = configuredGridHeight;
                }

                item.GridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(item.GridWidth);
                item.GridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(item.GridHeight);

                if (!CanPlaceWarehouseAnchor(placements, self.WarehouseColumnCount, item.AnchorSlotIndex, item.GridWidth, item.GridHeight))
                {
                    item.AnchorSlotIndex = FindWarehouseAnchorSlot(placements, self.WarehouseColumnCount, item.GridWidth, item.GridHeight);
                }

                placements.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
                self.WarehouseItems[i] = item;
            }
        }

        private static List<GridPlacementItemInfo> BuildWarehousePlacements(IList<LoadoutWarehouseItemInfo> source, int columnCount)
        {
            List<GridPlacementItemInfo> result = new(source.Count);
            if (columnCount <= 0)
            {
                return result;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = source[i];
                if (item.ConfigId <= 0 || item.Count <= 0 || item.AnchorSlotIndex < 0)
                {
                    continue;
                }

                result.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = LoadoutGridPlacementHelper.NormalizeGridWidth(item.GridWidth),
                    GridHeight = LoadoutGridPlacementHelper.NormalizeGridHeight(item.GridHeight),
                });
            }

            return result;
        }

        private static bool CanPlaceWarehouseAnchor(
            List<GridPlacementItemInfo> placements,
            int columnCount,
            int anchorSlotIndex,
            int gridWidth,
            int gridHeight)
        {
            if (columnCount <= 0 || anchorSlotIndex < 0)
            {
                return false;
            }

            int anchorX = anchorSlotIndex % columnCount;
            if (anchorX + gridWidth > columnCount)
            {
                return false;
            }

            int candidateRows = Math.Max(GetWarehouseRequiredRows(placements, columnCount), anchorSlotIndex / columnCount + gridHeight);
            GridPlacementItemInfo candidate = new GridPlacementItemInfo
            {
                ConfigId = 1,
                Count = 1,
                AnchorSlotIndex = anchorSlotIndex,
                GridWidth = gridWidth,
                GridHeight = gridHeight,
            };

            placements.Add(candidate);
            bool valid = LoadoutGridPlacementHelper.ArePlacementsValid(placements, columnCount, Math.Max(1, candidateRows));
            placements.RemoveAt(placements.Count - 1);
            return valid;
        }

        private static int FindWarehouseAnchorSlot(
            List<GridPlacementItemInfo> placements,
            int columnCount,
            int gridWidth,
            int gridHeight)
        {
            int currentRows = GetWarehouseRequiredRows(placements, columnCount);
            for (int anchorSlotIndex = 0;; ++anchorSlotIndex)
            {
                int anchorX = anchorSlotIndex % columnCount;
                if (anchorX + gridWidth > columnCount)
                {
                    continue;
                }

                int candidateRows = Math.Max(currentRows, anchorSlotIndex / columnCount + gridHeight);
                GridPlacementItemInfo candidate = new GridPlacementItemInfo
                {
                    ConfigId = 1,
                    Count = 1,
                    AnchorSlotIndex = anchorSlotIndex,
                    GridWidth = gridWidth,
                    GridHeight = gridHeight,
                };

                placements.Add(candidate);
                bool valid = LoadoutGridPlacementHelper.ArePlacementsValid(placements, columnCount, Math.Max(1, candidateRows));
                placements.RemoveAt(placements.Count - 1);
                if (valid)
                {
                    return anchorSlotIndex;
                }
            }
        }

        private static int GetWarehouseRequiredRows(List<GridPlacementItemInfo> placements, int columnCount)
        {
            int rows = 1;
            for (int i = 0; i < placements.Count; ++i)
            {
                GridPlacementItemInfo item = placements[i];
                rows = System.Math.Max(rows, item.AnchorSlotIndex / columnCount + item.GridHeight);
            }

            return rows;
        }
    }
}
