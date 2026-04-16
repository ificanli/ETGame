using global::ET;

namespace ET.Server
{
    /// <summary>
    /// 建造逻辑
    /// </summary>
    public static class HomeBuildHelper
    {
        /// <summary>
        /// 校验建造是否允许，并返回建造成本。
        /// </summary>
        public static int CheckBuild(Unit unit, int slotId, int configId, out int buildGoldCost, out string message)
        {
            buildGoldCost = 0;
            message = string.Empty;

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            HomeRuntimeHelper.RefreshUnlockState(homeComp);

            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(configId);
            if (buildingConfig == null)
            {
                return ErrorCode.ERR_HomeBuildingNotUnlocked;
            }

            if (!homeComp.UnlockedBuildingConfigIds.Contains(configId))
            {
                return ErrorCode.ERR_HomeBuildingNotUnlocked;
            }

            HomeSlotConfig slotConfig = HomeConfigHelper.GetSlotConfig(slotId);
            if (slotConfig == null)
            {
                return ErrorCode.ERR_HomeSlotInvalid;
            }

            if (!homeComp.UnlockedSlotIds.Contains(slotId))
            {
                return ErrorCode.ERR_HomeSlotNotUnlocked;
            }

            if (HomeRuntimeHelper.GetBuildingBySlot(homeComp, slotId) != null)
            {
                return ErrorCode.ERR_HomeSlotOccupied;
            }

            if (!HomeConfigHelper.CanBuildInSlot(slotConfig, buildingConfig.BuildingType))
            {
                return ErrorCode.ERR_HomeSlotInvalid;
            }

            if (HomeRuntimeHelper.CountBuildingsByConfig(homeComp, configId) >= buildingConfig.MaxCount)
            {
                message = "当前建筑数量已达上限。";
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            if (buildingConfig.BuildingType == HomeBuildingType.MainCity)
            {
                message = "主城已默认存在，不能重复建造。";
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            buildGoldCost = buildingConfig.BuildGoldCost;
            if (homeComp.TotalWealth < buildGoldCost)
            {
                return ErrorCode.ERR_HomeResourceNotEnough;
            }

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 在指定槽位建造建筑。
        /// </summary>
        public static int Build(Unit unit, int slotId, int configId)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            long now = TimeInfo.Instance.ServerNow();
            HomeBuilding building = homeComp.AddChild<HomeBuilding>();
            building.ConfigId = configId;
            building.Level = 1;
            building.State = HomeBuildingState.Idle;
            building.SlotId = slotId;
            building.LastCollectTime = now;
            building.LastProductionTime = now;

            Log.Debug($"HomeBuildHelper: built configId={configId} at slotId={slotId} for unit {unit.Id}");
            return ErrorCode.ERR_Success;
        }

        public static HomeBuilding GetBuildingBySlot(Unit unit, int slotId)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            return HomeRuntimeHelper.GetBuildingBySlot(homeComp, slotId);
        }
    }
}
