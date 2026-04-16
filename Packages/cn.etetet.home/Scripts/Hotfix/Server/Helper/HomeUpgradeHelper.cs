using global::ET;

namespace ET.Server
{
    /// <summary>
    /// 升级逻辑
    /// </summary>
    public static class HomeUpgradeHelper
    {
        /// <summary>
        /// 校验升级，并返回目标建筑和升级花费。
        /// </summary>
        public static int CheckUpgrade(Unit unit, long buildingId, out HomeBuilding building, out int upgradeGoldCost, out string message)
        {
            building = null;
            upgradeGoldCost = 0;
            message = string.Empty;

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            building = homeComp.GetChild<HomeBuilding>(buildingId);
            if (building == null)
            {
                return ErrorCode.ERR_HomeBuildingNotFound;
            }

            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (buildingConfig == null)
            {
                return ErrorCode.ERR_HomeBuildingMaxLevel;
            }

            if (buildingConfig.BuildingType == HomeBuildingType.MainCity)
            {
                if (building.Level >= HomeConfigHelper.GetMaxMainCityLevel())
                {
                    return ErrorCode.ERR_HomeBuildingMaxLevel;
                }

                if (!HomeMainCityTaskHelper.IsCurrentTaskGroupCompleted(homeComp, out _, out _, out _))
                {
                    message = HomeMainCityTaskHelper.BuildFirstIncompleteTaskHint(homeComp);
                    return ErrorCode.ERR_HomePrerequisiteNotMet;
                }

                HomeMainCityLevelConfig nextMainCityLevelConfig = HomeConfigHelper.GetMainCityLevelConfig(building.Level + 1);
                if (nextMainCityLevelConfig == null)
                {
                    return ErrorCode.ERR_HomeBuildingMaxLevel;
                }

                upgradeGoldCost = nextMainCityLevelConfig.UpgradeGoldCost;
                if (homeComp.TotalWealth < upgradeGoldCost)
                {
                    return ErrorCode.ERR_HomeResourceNotEnough;
                }

                return ErrorCode.ERR_Success;
            }

            HomeBuildingLevelConfig nextLevelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level + 1);
            if (nextLevelConfig == null)
            {
                return ErrorCode.ERR_HomeBuildingMaxLevel;
            }

            int mainCityLevel = HomeRuntimeHelper.GetMainCityLevel(homeComp);
            HomeMainCityLevelConfig mainCityLevelConfig = HomeConfigHelper.GetMainCityLevelConfig(mainCityLevel);
            if (mainCityLevelConfig == null || nextLevelConfig.Level > mainCityLevelConfig.OtherBuildingMaxLevel)
            {
                message = "请先提升主城等级后再升级该建筑。";
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            upgradeGoldCost = nextLevelConfig.UpgradeGoldCost;
            if (homeComp.TotalWealth < upgradeGoldCost)
            {
                return ErrorCode.ERR_HomeResourceNotEnough;
            }

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 升级指定建筑。
        /// </summary>
        public static int Upgrade(Unit unit, long buildingId)
        {
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

            building.Level++;
            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (buildingConfig != null && buildingConfig.BuildingType == HomeBuildingType.MainCity)
            {
                HomeRuntimeHelper.RefreshUnlockState(homeComp);
            }

            HomeCollectHelper.RefreshState(building);

            Log.Debug($"HomeUpgradeHelper: upgraded building {buildingId} to level {building.Level}");
            return ErrorCode.ERR_Success;
        }
    }
}
