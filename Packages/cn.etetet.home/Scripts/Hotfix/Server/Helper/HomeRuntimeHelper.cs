using global::ET;

namespace ET.Server
{
    public static class HomeRuntimeHelper
    {
        public static HomeBuilding GetMainCity(PlayerHomeComponent homeComp)
        {
            if (homeComp == null)
            {
                return null;
            }

            if (homeComp.MainCityBuildingId > 0)
            {
                HomeBuilding mainCity = homeComp.GetChild<HomeBuilding>(homeComp.MainCityBuildingId);
                if (mainCity != null)
                {
                    return mainCity;
                }
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is not HomeBuilding building)
                {
                    continue;
                }

                HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
                if (config != null && config.BuildingType == HomeBuildingType.MainCity)
                {
                    homeComp.MainCityBuildingId = building.Id;
                    return building;
                }
            }

            return null;
        }

        public static HomeBuilding GetBuildingBySlot(PlayerHomeComponent homeComp, int slotId)
        {
            if (homeComp == null || slotId <= 0)
            {
                return null;
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is HomeBuilding building && building.SlotId == slotId)
                {
                    return building;
                }
            }

            return null;
        }

        public static int GetMainCityLevel(PlayerHomeComponent homeComp)
        {
            HomeBuilding mainCity = GetMainCity(homeComp);
            return mainCity?.Level ?? 0;
        }

        public static HomeBuilding EnsureMainCity(PlayerHomeComponent homeComp)
        {
            HomeBuilding existing = GetMainCity(homeComp);
            if (existing != null)
            {
                return existing;
            }

            HomeBuildingConfig mainCityConfig = HomeConfigHelper.GetMainCityConfig();
            HomeSlotConfig mainCitySlot = HomeConfigHelper.GetMainCitySlotConfig();
            if (mainCityConfig == null || mainCitySlot == null)
            {
                return null;
            }

            long now = TimeInfo.Instance.ServerNow();
            HomeBuilding building = homeComp.AddChild<HomeBuilding>();
            building.ConfigId = mainCityConfig.Id;
            building.Level = 1;
            building.State = HomeBuildingState.Idle;
            building.SlotId = mainCitySlot.Id;
            building.LastCollectTime = now;
            building.LastProductionTime = now;
            homeComp.MainCityBuildingId = building.Id;
            return building;
        }

        public static void RefreshUnlockState(PlayerHomeComponent homeComp)
        {
            if (homeComp == null)
            {
                return;
            }

            homeComp.UnlockedBuildingConfigIds.Clear();
            foreach (HomeBuildingConfig config in HomeBuildingConfigCategory.Instance.DataList)
            {
                if (config != null && config.DefaultUnlock)
                {
                    homeComp.UnlockedBuildingConfigIds.Add(config.Id);
                }
            }

            int mainCityLevel = GetMainCityLevel(homeComp);
            if (mainCityLevel <= 0)
            {
                mainCityLevel = 1;
            }

            homeComp.UnlockedSlotIds.Clear();
            foreach (HomeSlotConfig slot in HomeConfigHelper.GetSortedSlotConfigs())
            {
                if (HomeConfigHelper.IsSlotUnlocked(mainCityLevel, slot))
                {
                    homeComp.UnlockedSlotIds.Add(slot.Id);
                }
            }
        }

        public static int CountBuildingsByConfig(PlayerHomeComponent homeComp, int configId)
        {
            int count = 0;
            if (homeComp == null || configId <= 0)
            {
                return count;
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is HomeBuilding building && building.ConfigId == configId)
                {
                    ++count;
                }
            }

            return count;
        }

        public static int CountBuildingsByType(PlayerHomeComponent homeComp, int buildingType)
        {
            int count = 0;
            if (homeComp == null)
            {
                return count;
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is not HomeBuilding building)
                {
                    continue;
                }

                HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
                if (config != null && config.BuildingType == buildingType)
                {
                    ++count;
                }
            }

            return count;
        }

        public static int GetHighestBuildingLevelByType(PlayerHomeComponent homeComp, int buildingType)
        {
            int level = 0;
            if (homeComp == null)
            {
                return level;
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is not HomeBuilding building)
                {
                    continue;
                }

                HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
                if (config != null && config.BuildingType == buildingType && building.Level > level)
                {
                    level = building.Level;
                }
            }

            return level;
        }

        public static int CalculateInvestedGold(HomeBuilding building)
        {
            if (building == null)
            {
                return 0;
            }

            int invested = 0;
            HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (config != null)
            {
                invested += config.BuildGoldCost;
            }

            for (int level = 2; level <= building.Level; ++level)
            {
                HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, level);
                if (levelConfig != null)
                {
                    invested += levelConfig.UpgradeGoldCost;
                }
            }

            return invested;
        }

        public static void ApplyStorageSummary(
            PlayerHomeComponent homeComp,
            long totalWealth,
            int warehouseItemCount,
            int warehouseOccupiedCellCount)
        {
            if (homeComp == null)
            {
                return;
            }

            homeComp.TotalWealth = totalWealth;
            homeComp.WarehouseItemCount = warehouseItemCount;
            homeComp.WarehouseOccupiedCellCount = warehouseOccupiedCellCount;
        }
    }
}
