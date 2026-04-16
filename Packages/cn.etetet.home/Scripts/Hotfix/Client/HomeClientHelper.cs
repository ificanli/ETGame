namespace ET.Client
{
    public static class HomeClientHelper
    {
        public static HomeClientComponent GetOrAddRuntime(Scene root)
        {
            if (root == null)
            {
                return null;
            }

            HomeClientComponent runtime = root.GetComponent<HomeClientComponent>();
            if (runtime == null)
            {
                runtime = root.AddComponent<HomeClientComponent>();
            }

            return runtime;
        }

        public static void ApplySnapshot(Scene root, M2C_HomeSnapshot message)
        {
            if (root == null || message == null)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.ResetRuntime();
            runtime.HomeVersion = message.HomeVersion;
            runtime.LastSettleTime = message.LastSettleTime;
            runtime.TotalWealth = message.TotalWealth;
            runtime.MainCitySummary = CloneMainCitySummary(message.MainCitySummary);
            runtime.WarehouseSummary = CloneWarehouseSummary(message.WarehouseSummary);

            for (int i = 0; i < message.UnlockedBuildingConfigIds.Count; ++i)
            {
                runtime.UnlockedBuildingConfigIds.Add(message.UnlockedBuildingConfigIds[i]);
            }

            for (int i = 0; i < message.Slots.Count; ++i)
            {
                runtime.Slots.Add(CloneSlot(message.Slots[i]));
            }

            for (int i = 0; i < message.Buildings.Count; ++i)
            {
                runtime.Buildings.Add(CloneBuilding(message.Buildings[i]));
            }

            for (int i = 0; i < message.ProductionOrders.Count; ++i)
            {
                runtime.ProductionOrders.Add(CloneOrder(message.ProductionOrders[i]));
            }

            for (int i = 0; i < message.MainCityTasks.Count; ++i)
            {
                runtime.MainCityTasks.Add(CloneMainCityTask(message.MainCityTasks[i]));
            }

            for (int i = 0; i < message.MuseumDisplays.Count; ++i)
            {
                runtime.MuseumDisplays.Add(CloneMuseumDisplay(message.MuseumDisplays[i]));
            }

            for (int i = 0; i < message.AvailableContracts.Count; ++i)
            {
                runtime.AvailableContracts.Add(CloneContract(message.AvailableContracts[i]));
            }

            for (int i = 0; i < message.ActiveContracts.Count; ++i)
            {
                runtime.ActiveContracts.Add(CloneContract(message.ActiveContracts[i]));
            }
        }

        public static void ApplyBuildResponse(Scene root, M2C_HomeBuildResponse response)
        {
            if (response == null || response.Error != ErrorCode.ERR_Success || response.Building == null)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.UpsertBuilding(CloneBuilding(response.Building));

            HomeClientSlotData slot = runtime.GetSlot(response.Building.SlotId);
            if (slot != null)
            {
                slot.BuildingId = response.Building.BuildingId;
                slot.BuildingConfigId = response.Building.ConfigId;
            }
        }

        public static void ApplyUpgradeResponse(Scene root, M2C_HomeUpgradeResponse response)
        {
            if (response == null || response.Error != ErrorCode.ERR_Success || response.Building == null)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.UpsertBuilding(CloneBuilding(response.Building));
        }

        public static void ApplyDemolish(Scene root, long buildingId)
        {
            if (buildingId <= 0)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.RemoveBuilding(buildingId);

            for (int i = 0; i < runtime.Slots.Count; ++i)
            {
                HomeClientSlotData slot = runtime.Slots[i];
                if (slot == null || slot.BuildingId != buildingId)
                {
                    continue;
                }

                slot.BuildingId = 0;
                slot.BuildingConfigId = 0;
                break;
            }
        }

        public static void ApplyCollect(Scene root, long buildingId)
        {
            if (buildingId <= 0)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            HomeClientBuildingData building = runtime.GetBuilding(buildingId);
            if (building == null)
            {
                return;
            }

            building.LastCollectTime = TimeInfo.Instance.ServerNow();
            building.State = HomeBuildingState.Idle;
        }

        public static void ApplyStartProductionResponse(Scene root, M2C_HomeStartProductionResponse response)
        {
            if (response == null || response.Error != ErrorCode.ERR_Success || response.Order == null)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.UpsertProductionOrder(CloneOrder(response.Order));

            HomeClientBuildingData building = runtime.GetBuilding(response.Order.BuildingEntityId);
            if (building != null)
            {
                building.LastProductionTime = response.Order.StartTime;
            }
        }

        public static void ApplyCollectProduction(Scene root, long orderId)
        {
            if (orderId <= 0)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            HomeClientProductionOrderData order = runtime.GetProductionOrder(orderId);
            if (order != null)
            {
                HomeClientBuildingData building = runtime.GetBuilding(order.BuildingEntityId);
                if (building != null)
                {
                    long now = TimeInfo.Instance.ServerNow();
                    building.LastCollectTime = now;
                    building.LastProductionTime = now;
                }
            }

            runtime.RemoveProductionOrder(orderId);
        }

        public static void ApplyMuseumPlaceResponse(Scene root, M2C_HomeMuseumPlaceResponse response)
        {
            if (response == null || response.Error != ErrorCode.ERR_Success || response.Display == null)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.UpsertMuseumDisplay(CloneMuseumDisplay(response.Display));
        }

        public static void ApplyMuseumTakeDownResponse(Scene root, M2C_HomeMuseumTakeDownResponse response)
        {
            if (response == null || response.Error != ErrorCode.ERR_Success || response.DisplayId <= 0)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.RemoveMuseumDisplay(response.DisplayId);
        }

        public static void ApplyContractsChanged(Scene root, M2C_HomeContractsChanged message)
        {
            if (root == null || message == null)
            {
                return;
            }

            HomeClientComponent runtime = GetOrAddRuntime(root);
            runtime.AvailableContracts.Clear();
            runtime.ActiveContracts.Clear();

            for (int i = 0; i < message.AvailableContracts.Count; ++i)
            {
                runtime.AvailableContracts.Add(CloneContract(message.AvailableContracts[i]));
            }

            for (int i = 0; i < message.ActiveContracts.Count; ++i)
            {
                runtime.ActiveContracts.Add(CloneContract(message.ActiveContracts[i]));
            }
        }

        private static HomeClientBuildingData CloneBuilding(HomeBuildingInfo source)
        {
            if (source == null)
            {
                return null;
            }

            return new HomeClientBuildingData
            {
                BuildingId = source.BuildingId,
                ConfigId = source.ConfigId,
                Level = source.Level,
                State = source.State,
                SlotId = source.SlotId,
                LastCollectTime = source.LastCollectTime,
                LastProductionTime = source.LastProductionTime,
            };
        }

        private static HomeClientSlotData CloneSlot(HomeSlotInfo source)
        {
            if (source == null)
            {
                return null;
            }

            HomeClientSlotData data = new()
            {
                SlotId = source.SlotId,
                SlotType = source.SlotType,
                SceneAnchorKey = source.SceneAnchorKey,
                Unlocked = source.Unlocked,
                SortOrder = source.SortOrder,
                BuildingId = source.BuildingId,
                BuildingConfigId = source.BuildingConfigId,
            };
            data.CanBuildTypes.AddRange(source.CanBuildTypes);
            return data;
        }

        private static HomeClientMainCitySummaryData CloneMainCitySummary(HomeMainCitySummary source)
        {
            if (source == null)
            {
                return new HomeClientMainCitySummaryData();
            }

            return new HomeClientMainCitySummaryData
            {
                BuildingId = source.BuildingId,
                BuildingConfigId = source.BuildingConfigId,
                Level = source.Level,
                MaxLevel = source.MaxLevel,
                UnlockedSlotCount = source.UnlockedSlotCount,
                OtherBuildingMaxLevel = source.OtherBuildingMaxLevel,
                TaskGroupId = source.TaskGroupId,
                NextUpgradeGoldCost = source.NextUpgradeGoldCost,
                CanUpgrade = source.CanUpgrade,
                PreviewText = source.PreviewText,
                TaskFinishedCount = source.TaskFinishedCount,
                TaskTotalCount = source.TaskTotalCount,
            };
        }

        private static HomeClientWarehouseSummaryData CloneWarehouseSummary(HomeWarehouseSummary source)
        {
            if (source == null)
            {
                return new HomeClientWarehouseSummaryData();
            }

            return new HomeClientWarehouseSummaryData
            {
                Level = source.Level,
                Capacity = source.Capacity,
                OccupiedCellCount = source.OccupiedCellCount,
                ItemCount = source.ItemCount,
            };
        }

        private static HomeClientProductionOrderData CloneOrder(HomeProductionOrderInfo source)
        {
            if (source == null)
            {
                return null;
            }

            return new HomeClientProductionOrderData
            {
                OrderId = source.OrderId,
                BuildingEntityId = source.BuildingEntityId,
                RecipeId = source.RecipeId,
                State = source.State,
                StartTime = source.StartTime,
                FinishTime = source.FinishTime,
            };
        }

        private static HomeClientMainCityTaskData CloneMainCityTask(HomeMainCityTaskInfo source)
        {
            if (source == null)
            {
                return null;
            }

            return new HomeClientMainCityTaskData
            {
                TaskId = source.TaskId,
                TaskGroupId = source.TaskGroupId,
                TaskType = source.TaskType,
                Param1 = source.Param1,
                Param2 = source.Param2,
                Title = source.Title,
                Desc = source.Desc,
                Progress = source.Progress,
                Target = source.Target,
                Completed = source.Completed,
                SortOrder = source.SortOrder,
            };
        }

        private static HomeClientMuseumDisplayData CloneMuseumDisplay(HomeMuseumDisplayInfo source)
        {
            if (source == null)
            {
                return null;
            }

            return new HomeClientMuseumDisplayData
            {
                DisplayId = source.DisplayId,
                BuildingId = source.BuildingId,
                SlotIndex = source.SlotIndex,
                ItemConfigId = source.ItemConfigId,
            };
        }

        private static HomeClientContractData CloneContract(HomeContractInfo source)
        {
            if (source == null)
            {
                return null;
            }

            return new HomeClientContractData
            {
                ContractId = source.ContractId,
                ConfigId = source.ConfigId,
                State = source.State,
                AcceptTime = source.AcceptTime,
                ExpireTime = source.ExpireTime,
                Progress = source.Progress,
                Target = source.Target,
            };
        }
    }
}
