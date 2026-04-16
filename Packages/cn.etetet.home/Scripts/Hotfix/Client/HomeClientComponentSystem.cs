using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(HomeClientComponent))]
    public static partial class HomeClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HomeClientComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HomeClientComponent self)
        {
            self.ResetRuntime();
        }

        public static void ResetRuntime(this HomeClientComponent self)
        {
            self.HomeVersion = 0;
            self.LastSettleTime = 0;
            self.TotalWealth = 0;
            self.MainCitySummary = new HomeClientMainCitySummaryData();
            self.WarehouseSummary = new HomeClientWarehouseSummaryData();
            self.UnlockedBuildingConfigIds.Clear();
            self.Slots.Clear();
            self.Buildings.Clear();
            self.ProductionOrders.Clear();
            self.MainCityTasks.Clear();
            self.MuseumDisplays.Clear();
            self.AvailableContracts.Clear();
            self.ActiveContracts.Clear();
        }

        public static HomeClientBuildingData GetBuilding(this HomeClientComponent self, long buildingId)
        {
            for (int i = 0; i < self.Buildings.Count; ++i)
            {
                HomeClientBuildingData building = self.Buildings[i];
                if (building.BuildingId == buildingId)
                {
                    return building;
                }
            }

            return null;
        }

        public static void UpsertBuilding(this HomeClientComponent self, HomeClientBuildingData data)
        {
            if (data == null || data.BuildingId <= 0)
            {
                return;
            }

            for (int i = 0; i < self.Buildings.Count; ++i)
            {
                if (self.Buildings[i].BuildingId != data.BuildingId)
                {
                    continue;
                }

                self.Buildings[i] = data;
                return;
            }

            self.Buildings.Add(data);
        }

        public static bool RemoveBuilding(this HomeClientComponent self, long buildingId)
        {
            for (int i = 0; i < self.Buildings.Count; ++i)
            {
                if (self.Buildings[i].BuildingId != buildingId)
                {
                    continue;
                }

                self.Buildings.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static HomeClientProductionOrderData GetProductionOrder(this HomeClientComponent self, long orderId)
        {
            for (int i = 0; i < self.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = self.ProductionOrders[i];
                if (order != null && order.OrderId == orderId)
                {
                    return order;
                }
            }

            return null;
        }

        public static void UpsertProductionOrder(this HomeClientComponent self, HomeClientProductionOrderData data)
        {
            if (data == null || data.OrderId <= 0)
            {
                return;
            }

            for (int i = 0; i < self.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = self.ProductionOrders[i];
                if (order == null || order.OrderId != data.OrderId)
                {
                    continue;
                }

                self.ProductionOrders[i] = data;
                return;
            }

            self.ProductionOrders.Add(data);
        }

        public static bool RemoveProductionOrder(this HomeClientComponent self, long orderId)
        {
            for (int i = 0; i < self.ProductionOrders.Count; ++i)
            {
                HomeClientProductionOrderData order = self.ProductionOrders[i];
                if (order == null || order.OrderId != orderId)
                {
                    continue;
                }

                self.ProductionOrders.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static HomeClientMuseumDisplayData GetMuseumDisplay(this HomeClientComponent self, long displayId)
        {
            for (int i = 0; i < self.MuseumDisplays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = self.MuseumDisplays[i];
                if (display != null && display.DisplayId == displayId)
                {
                    return display;
                }
            }

            return null;
        }

        public static void UpsertMuseumDisplay(this HomeClientComponent self, HomeClientMuseumDisplayData data)
        {
            if (data == null || data.DisplayId <= 0)
            {
                return;
            }

            for (int i = 0; i < self.MuseumDisplays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = self.MuseumDisplays[i];
                if (display == null || display.DisplayId != data.DisplayId)
                {
                    continue;
                }

                self.MuseumDisplays[i] = data;
                return;
            }

            self.MuseumDisplays.Add(data);
        }

        public static bool RemoveMuseumDisplay(this HomeClientComponent self, long displayId)
        {
            for (int i = 0; i < self.MuseumDisplays.Count; ++i)
            {
                HomeClientMuseumDisplayData display = self.MuseumDisplays[i];
                if (display == null || display.DisplayId != displayId)
                {
                    continue;
                }

                self.MuseumDisplays.RemoveAt(i);
                return true;
            }

            return false;
        }

        public static int GetMaxBuildingLevel(this HomeClientComponent self)
        {
            int maxLevel = 0;
            for (int i = 0; i < self.Buildings.Count; ++i)
            {
                if (self.Buildings[i].Level > maxLevel)
                {
                    maxLevel = self.Buildings[i].Level;
                }
            }

            return maxLevel;
        }

        public static HomeClientSlotData GetSlot(this HomeClientComponent self, int slotId)
        {
            for (int i = 0; i < self.Slots.Count; ++i)
            {
                HomeClientSlotData slot = self.Slots[i];
                if (slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }

        public static List<int> GetBuildablePrototypeConfigIds(this HomeClientComponent self)
        {
            List<HomeBuildingConfig> configs = new();
            for (int i = 0; i < self.UnlockedBuildingConfigIds.Count; ++i)
            {
                int configId = self.UnlockedBuildingConfigIds[i];
                HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
                if (config == null || config.BuildingType == HomeBuildingType.MainCity)
                {
                    continue;
                }

                if (self.CountBuildingsByConfig(configId) >= config.MaxCount)
                {
                    continue;
                }

                configs.Add(config);
            }

            configs.Sort(static (a, b) =>
            {
                int sortCompare = a.SortOrder.CompareTo(b.SortOrder);
                return sortCompare != 0 ? sortCompare : a.Id.CompareTo(b.Id);
            });

            List<int> result = new(configs.Count);
            for (int i = 0; i < configs.Count; ++i)
            {
                result.Add(configs[i].Id);
            }

            return result;
        }

        public static int GetNextAvailableBuildSlotId(this HomeClientComponent self, int configId)
        {
            HomeBuildingConfig config = HomeBuildingConfigCategory.Instance.GetOrDefault(configId);
            if (config == null)
            {
                return 0;
            }

            for (int i = 0; i < self.Slots.Count; ++i)
            {
                HomeClientSlotData slot = self.Slots[i];
                if (slot == null || !slot.Unlocked || slot.SlotType != HomeSlotType.Buildable || slot.BuildingId > 0)
                {
                    continue;
                }

                if (slot.CanBuildTypes.Count == 0)
                {
                    return slot.SlotId;
                }

                for (int j = 0; j < slot.CanBuildTypes.Count; ++j)
                {
                    if (slot.CanBuildTypes[j] == config.BuildingType)
                    {
                        return slot.SlotId;
                    }
                }
            }

            return 0;
        }

        private static int CountBuildingsByConfig(this HomeClientComponent self, int configId)
        {
            int count = 0;
            for (int i = 0; i < self.Buildings.Count; ++i)
            {
                if (self.Buildings[i].ConfigId == configId)
                {
                    ++count;
                }
            }

            return count;
        }
    }
}
