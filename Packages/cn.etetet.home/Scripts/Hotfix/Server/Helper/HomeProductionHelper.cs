using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 物品回收间生产逻辑。
    /// </summary>
    public static class HomeProductionHelper
    {
        public const int DefaultRecycleIntervalMs = 60_000;
        public const int DefaultOutputCount = 1;

        public static int CheckStartProduction(Unit unit, long buildingId, int recipeId, out string message)
        {
            message = string.Empty;

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            HomeBuilding building = homeComp.GetChild<HomeBuilding>(buildingId);
            if (building == null)
            {
                return ErrorCode.ERR_HomeBuildingNotFound;
            }

            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (buildingConfig == null || buildingConfig.BuildingType != HomeBuildingType.RecycleRoom)
            {
                message = "只有物品回收间可以执行回收。";
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            int normalizedRecipeId = LegacyItemConfigIdHelper.NormalizeConfigId(recipeId);
            if (normalizedRecipeId <= 0 || ItemConfigCategory.Instance.GetOrDefault(normalizedRecipeId) == null)
            {
                message = "当前没有可用的回收材料。";
                return ErrorCode.ERR_HomeRecipeNotUnlocked;
            }

            HomeProductionComponent prodComp = unit.GetComponent<HomeProductionComponent>();
            if (prodComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            RefreshOrders(prodComp);
            int capacity = GetRecycleSlotCapacity(building);
            int activeOrderCount = CountActiveOrders(prodComp, buildingId);
            if (activeOrderCount >= capacity)
            {
                message = "当前回收位已满，请先收取已完成产物或升级回收间。";
                return ErrorCode.ERR_HomeProductionQueueFull;
            }

            return ErrorCode.ERR_Success;
        }

        public static HomeProductionStartResult StartProduction(Unit unit, long buildingId, int recipeId)
        {
            int error = CheckStartProduction(unit, buildingId, recipeId, out _);
            if (error != ErrorCode.ERR_Success)
            {
                return new HomeProductionStartResult { ErrorCode = error };
            }

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            HomeBuilding building = homeComp?.GetChild<HomeBuilding>(buildingId);
            HomeProductionComponent prodComp = unit.GetComponent<HomeProductionComponent>();
            long now = TimeInfo.Instance.ServerNow();
            long orderId = IdGenerater.Instance.GenerateId();
            int normalizedRecipeId = LegacyItemConfigIdHelper.NormalizeConfigId(recipeId);

            HomeProductionOrderData order = new()
            {
                OrderId = orderId,
                BuildingEntityId = buildingId,
                RecipeId = normalizedRecipeId,
                State = HomeProductionState.InProgress,
                StartTime = now,
                FinishTime = now + GetRecycleIntervalMs(building)
            };

            prodComp.ProductionOrders[orderId] = order;
            if (building != null)
            {
                building.LastProductionTime = now;
            }

            Log.Debug($"HomeProductionHelper: started recycle orderId={orderId}, recipeId={normalizedRecipeId}");
            return new HomeProductionStartResult { ErrorCode = ErrorCode.ERR_Success, OrderId = orderId };
        }

        public static HomeCollectResult CollectProduction(Unit unit, long orderId)
        {
            HomeProductionComponent prodComp = unit.GetComponent<HomeProductionComponent>();
            if (prodComp == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            if (!prodComp.ProductionOrders.TryGetValue(orderId, out HomeProductionOrderData order))
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeProductionNotFound };
            }

            RefreshOrderState(order);
            if (order.State != HomeProductionState.Completed)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeProductionNotReady };
            }

            int outputConfigId = ResolveDemoOutputItemConfigId(order.RecipeId, order.OrderId);
            if (outputConfigId <= 0)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomePrerequisiteNotMet };
            }

            order.State = HomeProductionState.Collected;

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            HomeBuilding building = homeComp?.GetChild<HomeBuilding>(order.BuildingEntityId);
            if (building != null)
            {
                long now = TimeInfo.Instance.ServerNow();
                building.LastCollectTime = now;
                building.LastProductionTime = now;
            }

            Log.Debug($"HomeProductionHelper: collected recycle orderId={orderId}, output={outputConfigId}");
            return new HomeCollectResult
            {
                ErrorCode = ErrorCode.ERR_Success,
                ItemConfigIds = new List<int> { outputConfigId },
                ItemCounts = new List<int> { DefaultOutputCount }
            };
        }

        public static void ClearCollectedOrder(Unit unit, long orderId)
        {
            HomeProductionComponent prodComp = unit?.GetComponent<HomeProductionComponent>();
            prodComp?.ProductionOrders.Remove(orderId);
        }

        public static int GetRecycleSlotCapacity(HomeBuilding building)
        {
            if (building == null)
            {
                return 1;
            }

            HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level);
            return Math.Max(levelConfig?.CapacityValue1 ?? 1, 1);
        }

        public static int GetRecycleIntervalMs(HomeBuilding building)
        {
            if (building == null)
            {
                return DefaultRecycleIntervalMs;
            }

            HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level);
            return Math.Max(HomeConfigHelper.GetExtraInt(levelConfig, "processIntervalMs", DefaultRecycleIntervalMs), 1000);
        }

        public static void RefreshOrders(HomeProductionComponent prodComp)
        {
            if (prodComp == null)
            {
                return;
            }

            foreach (HomeProductionOrderData order in prodComp.ProductionOrders.Values)
            {
                RefreshOrderState(order);
            }
        }

        public static int CountActiveOrders(HomeProductionComponent prodComp, long buildingId)
        {
            int count = 0;
            if (prodComp == null || buildingId <= 0)
            {
                return count;
            }

            foreach (HomeProductionOrderData order in prodComp.ProductionOrders.Values)
            {
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                if (order.State != HomeProductionState.Collected)
                {
                    ++count;
                }
            }

            return count;
        }

        public static int CountCompletedOrders(HomeProductionComponent prodComp, long buildingId)
        {
            int count = 0;
            if (prodComp == null || buildingId <= 0)
            {
                return count;
            }

            foreach (HomeProductionOrderData order in prodComp.ProductionOrders.Values)
            {
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                RefreshOrderState(order);
                if (order.State == HomeProductionState.Completed)
                {
                    ++count;
                }
            }

            return count;
        }

        public static long GetFirstCompletedOrderId(HomeProductionComponent prodComp, long buildingId)
        {
            if (prodComp == null || buildingId <= 0)
            {
                return 0;
            }

            long candidate = 0;
            long candidateFinishTime = long.MaxValue;
            foreach (HomeProductionOrderData order in prodComp.ProductionOrders.Values)
            {
                if (order == null || order.BuildingEntityId != buildingId)
                {
                    continue;
                }

                RefreshOrderState(order);
                if (order.State != HomeProductionState.Completed)
                {
                    continue;
                }

                if (order.FinishTime < candidateFinishTime)
                {
                    candidate = order.OrderId;
                    candidateFinishTime = order.FinishTime;
                }
            }

            return candidate;
        }

        public static bool CanStoreOutput(Unit unit, IList<int> itemConfigIds, IList<int> itemCounts, out string message)
        {
            message = string.Empty;

            PlayerHomeComponent homeComp = unit?.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return false;
            }

            int capacity = GetWarehouseCapacity(homeComp);
            if (capacity <= 0)
            {
                message = "当前没有可用仓库，无法收取回收产物。";
                return false;
            }

            int occupied = homeComp.WarehouseOccupiedCellCount;
            int required = 0;
            for (int i = 0; i < itemConfigIds.Count && i < itemCounts.Count; ++i)
            {
                required += EstimateRequiredWarehouseCells(itemConfigIds[i], itemCounts[i]);
            }

            if (occupied + required > capacity)
            {
                message = $"仓库空间不足：当前 {occupied}/{capacity}，至少还需要 {required} 格。";
                return false;
            }

            return true;
        }

        private static int GetWarehouseCapacity(PlayerHomeComponent homeComp)
        {
            if (homeComp == null)
            {
                return 0;
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is not HomeBuilding building)
                {
                    continue;
                }

                HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
                if (buildingConfig == null || buildingConfig.BuildingType != HomeBuildingType.Warehouse)
                {
                    continue;
                }

                HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level);
                return levelConfig?.CapacityValue1 ?? 0;
            }

            return 0;
        }

        private static void RefreshOrderState(HomeProductionOrderData order)
        {
            if (order == null || order.State != HomeProductionState.InProgress)
            {
                return;
            }

            if (TimeInfo.Instance.ServerNow() >= order.FinishTime)
            {
                order.State = HomeProductionState.Completed;
            }
        }

        private static int EstimateRequiredWarehouseCells(int itemConfigId, int itemCount)
        {
            if (itemConfigId <= 0 || itemCount <= 0)
            {
                return 0;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(LegacyItemConfigIdHelper.NormalizeConfigId(itemConfigId));
            if (itemConfig == null)
            {
                return 0;
            }

            int maxStack = Math.Max(itemConfig.MaxStack, 1);
            int stackCount = (itemCount + maxStack - 1) / maxStack;
            int width = itemConfig.GridWidth > 0 ? itemConfig.GridWidth : LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
            int height = itemConfig.GridHeight > 0 ? itemConfig.GridHeight : LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
            return stackCount * width * height;
        }

        private static int ResolveDemoOutputItemConfigId(int sourceItemConfigId, long orderId)
        {
            List<int> candidates = new();
            foreach (ItemConfig config in ItemConfigCategory.Instance.DataList)
            {
                if (config == null || config.Id <= 0)
                {
                    continue;
                }

                if (config.Id == sourceItemConfigId)
                {
                    continue;
                }

                candidates.Add(config.Id);
            }

            if (candidates.Count == 0)
            {
                ItemConfig fallback = ItemConfigCategory.Instance.GetOrDefault(LegacyItemConfigIdHelper.NormalizeConfigId(sourceItemConfigId));
                return fallback?.Id ?? 0;
            }

            ulong seed = (ulong)orderId ^ (uint)sourceItemConfigId;
            int index = (int)(seed % (ulong)candidates.Count);
            return candidates[index];
        }
    }
}
