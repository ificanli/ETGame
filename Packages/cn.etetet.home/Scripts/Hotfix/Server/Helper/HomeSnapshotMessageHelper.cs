using System.Collections.Generic;
using global::ET;

namespace ET.Server
{
    public static class HomeSnapshotMessageHelper
    {
        public static M2C_HomeSnapshot CreateSnapshot(Unit unit)
        {
            M2C_HomeSnapshot snapshot = M2C_HomeSnapshot.Create();
            snapshot.Id = unit?.Id ?? 0;
            if (unit == null)
            {
                return snapshot;
            }

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return snapshot;
            }

            snapshot.HomeVersion = homeComp.HomeVersion;
            snapshot.LastSettleTime = homeComp.LastSettleTime;
            snapshot.TotalWealth = homeComp.TotalWealth;

            foreach (int configId in homeComp.UnlockedBuildingConfigIds)
            {
                snapshot.UnlockedBuildingConfigIds.Add(configId);
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is HomeBuilding building)
                {
                    HomeCollectHelper.RefreshState(building);
                    snapshot.Buildings.Add(CreateBuildingInfo(building));
                }
            }

            foreach (HomeSlotConfig slotConfig in HomeConfigHelper.GetSortedSlotConfigs())
            {
                snapshot.Slots.Add(CreateSlotInfo(homeComp, slotConfig));
            }

            snapshot.MainCitySummary = CreateMainCitySummary(homeComp);
            snapshot.WarehouseSummary = CreateWarehouseSummary(homeComp);
            List<HomeMainCityTaskConfig> taskConfigs = HomeMainCityTaskHelper.GetCurrentTaskConfigs(homeComp);
            for (int i = 0; i < taskConfigs.Count; ++i)
            {
                snapshot.MainCityTasks.Add(CreateMainCityTaskInfo(homeComp, taskConfigs[i]));
            }

            if (homeComp.MuseumDisplays != null)
            {
                for (int i = 0; i < homeComp.MuseumDisplays.Count; ++i)
                {
                    HomeMuseumDisplayData display = homeComp.MuseumDisplays[i];
                    if (display != null)
                    {
                        snapshot.MuseumDisplays.Add(CreateMuseumDisplayInfo(display));
                    }
                }
            }

            HomeProductionComponent productionComp = unit.GetComponent<HomeProductionComponent>();
            if (productionComp != null)
            {
                HomeProductionHelper.RefreshOrders(productionComp);
                foreach (HomeProductionOrderData order in productionComp.ProductionOrders.Values)
                {
                    snapshot.ProductionOrders.Add(CreateOrderInfo(order));
                }
            }

            HomeContractComponent contractComp = unit.GetComponent<HomeContractComponent>();
            if (contractComp != null)
            {
                for (int i = 0; i < contractComp.AvailableContracts.Count; ++i)
                {
                    snapshot.AvailableContracts.Add(CreateContractInfo(contractComp.AvailableContracts[i]));
                }

                for (int i = 0; i < contractComp.ActiveContracts.Count; ++i)
                {
                    snapshot.ActiveContracts.Add(CreateContractInfo(contractComp.ActiveContracts[i]));
                }
            }

            return snapshot;
        }

        public static HomeBuildingInfo CreateBuildingInfo(HomeBuilding building)
        {
            HomeBuildingInfo info = HomeBuildingInfo.Create();
            if (building == null)
            {
                return info;
            }

            info.BuildingId = building.Id;
            info.ConfigId = building.ConfigId;
            info.Level = building.Level;
            info.State = building.State;
            info.SlotId = building.SlotId;
            info.LastCollectTime = building.LastCollectTime;
            info.LastProductionTime = building.LastProductionTime;
            return info;
        }

        public static HomeSlotInfo CreateSlotInfo(PlayerHomeComponent homeComp, HomeSlotConfig slotConfig)
        {
            HomeSlotInfo info = HomeSlotInfo.Create();
            if (homeComp == null || slotConfig == null)
            {
                return info;
            }

            info.SlotId = slotConfig.Id;
            info.SlotType = slotConfig.SlotType;
            info.SceneAnchorKey = slotConfig.SceneAnchorKey ?? string.Empty;
            info.Unlocked = homeComp.UnlockedSlotIds.Contains(slotConfig.Id);
            info.SortOrder = slotConfig.SortOrder;
            if (slotConfig.CanBuildTypes != null)
            {
                info.CanBuildTypes.AddRange(slotConfig.CanBuildTypes);
            }

            HomeBuilding building = HomeRuntimeHelper.GetBuildingBySlot(homeComp, slotConfig.Id);
            if (building != null)
            {
                info.BuildingId = building.Id;
                info.BuildingConfigId = building.ConfigId;
            }

            return info;
        }

        public static HomeMainCitySummary CreateMainCitySummary(PlayerHomeComponent homeComp)
        {
            HomeMainCitySummary summary = HomeMainCitySummary.Create();
            HomeBuilding mainCity = HomeRuntimeHelper.GetMainCity(homeComp);
            if (mainCity == null)
            {
                return summary;
            }

            HomeMainCityLevelConfig levelConfig = HomeConfigHelper.GetMainCityLevelConfig(mainCity.Level);
            HomeMainCityLevelConfig nextLevelConfig = HomeConfigHelper.GetMainCityLevelConfig(mainCity.Level + 1);
            summary.BuildingId = mainCity.Id;
            summary.BuildingConfigId = mainCity.ConfigId;
            summary.Level = mainCity.Level;
            summary.MaxLevel = HomeConfigHelper.GetMaxMainCityLevel();
            summary.UnlockedSlotCount = levelConfig?.UnlockSlotCount ?? 0;
            summary.OtherBuildingMaxLevel = levelConfig?.OtherBuildingMaxLevel ?? 0;
            summary.TaskGroupId = levelConfig?.TaskGroupId ?? 0;
            summary.NextUpgradeGoldCost = nextLevelConfig?.UpgradeGoldCost ?? 0;
            HomeMainCityTaskHelper.CountTaskGroupProgress(homeComp, summary.TaskGroupId, out int finishedCount, out int totalCount);
            summary.TaskFinishedCount = finishedCount;
            summary.TaskTotalCount = totalCount;
            summary.CanUpgrade = nextLevelConfig != null && (totalCount <= 0 || finishedCount >= totalCount);
            summary.PreviewText = levelConfig?.PreviewText ?? string.Empty;
            return summary;
        }

        public static HomeWarehouseSummary CreateWarehouseSummary(PlayerHomeComponent homeComp)
        {
            HomeWarehouseSummary summary = HomeWarehouseSummary.Create();
            if (homeComp == null)
            {
                return summary;
            }

            HomeBuilding warehouse = null;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is not HomeBuilding building)
                {
                    continue;
                }

                HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
                if (config != null && config.BuildingType == HomeBuildingType.Warehouse)
                {
                    warehouse = building;
                    break;
                }
            }

            if (warehouse != null)
            {
                HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(warehouse.ConfigId, warehouse.Level);
                summary.Level = warehouse.Level;
                summary.Capacity = levelConfig?.CapacityValue1 ?? 0;
            }

            summary.ItemCount = homeComp.WarehouseItemCount;
            summary.OccupiedCellCount = homeComp.WarehouseOccupiedCellCount;
            return summary;
        }

        public static HomeMainCityTaskInfo CreateMainCityTaskInfo(PlayerHomeComponent homeComp, HomeMainCityTaskConfig config)
        {
            HomeMainCityTaskInfo info = HomeMainCityTaskInfo.Create();
            if (config == null)
            {
                return info;
            }

            HomeMainCityTaskHelper.EvaluateTask(homeComp, config, out int progress, out int target, out bool completed);
            info.TaskId = config.Id;
            info.TaskGroupId = config.TaskGroupId;
            info.TaskType = config.TaskType;
            info.Param1 = config.Param1;
            info.Param2 = config.Param2;
            info.Title = config.Title ?? string.Empty;
            info.Desc = config.Desc ?? string.Empty;
            info.Progress = progress;
            info.Target = target;
            info.Completed = completed;
            info.SortOrder = config.SortOrder;
            return info;
        }

        public static HomeMuseumDisplayInfo CreateMuseumDisplayInfo(HomeMuseumDisplayData display)
        {
            HomeMuseumDisplayInfo info = HomeMuseumDisplayInfo.Create();
            if (display == null)
            {
                return info;
            }

            info.DisplayId = display.DisplayId;
            info.BuildingId = display.BuildingId;
            info.SlotIndex = display.SlotIndex;
            info.ItemConfigId = display.ItemConfigId;
            return info;
        }

        public static HomeProductionOrderInfo CreateOrderInfo(HomeProductionOrderData order)
        {
            HomeProductionOrderInfo info = HomeProductionOrderInfo.Create();
            if (order == null)
            {
                return info;
            }

            info.OrderId = order.OrderId;
            info.BuildingEntityId = order.BuildingEntityId;
            info.RecipeId = order.RecipeId;
            info.State = order.State;
            info.StartTime = order.StartTime;
            info.FinishTime = order.FinishTime;
            return info;
        }

        public static HomeContractInfo CreateContractInfo(HomeContractData contract)
        {
            HomeContractInfo info = HomeContractInfo.Create();
            if (contract == null)
            {
                return info;
            }

            info.ContractId = contract.ContractId;
            info.ConfigId = contract.ConfigId;
            info.State = contract.State;
            info.AcceptTime = contract.AcceptTime;
            info.ExpireTime = contract.ExpireTime;
            info.Progress = contract.Progress;
            info.Target = contract.Target;
            return info;
        }
    }
}
