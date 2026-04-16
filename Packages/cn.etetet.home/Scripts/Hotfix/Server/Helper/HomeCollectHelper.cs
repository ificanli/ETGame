using System.Collections.Generic;
using global::ET;

namespace ET.Server
{
    /// <summary>
    /// 收取被动产出。
    /// </summary>
    public static class HomeCollectHelper
    {
        private const int DEFAULT_FARM_COLLECT_INTERVAL_MS = 60_000;

        public static HomeCollectResult Collect(Unit unit, long buildingId)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            HomeBuilding building = homeComp.GetChild<HomeBuilding>(buildingId);
            if (building == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeBuildingNotFound };
            }

            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (buildingConfig == null || buildingConfig.BuildingType != HomeBuildingType.Farm)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNothingToCollect };
            }

            HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level);
            if (levelConfig == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNothingToCollect };
            }

            long now = TimeInfo.Instance.ServerNow();
            int collectIntervalMs = HomeConfigHelper.GetExtraInt(levelConfig, "collectIntervalMs", DEFAULT_FARM_COLLECT_INTERVAL_MS);
            long elapsed = now - building.LastCollectTime;
            long tickCount = collectIntervalMs > 0 ? elapsed / collectIntervalMs : 0;
            long wealthDelta = tickCount * levelConfig.OutputValue;
            if (tickCount <= 0 || wealthDelta <= 0)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNothingToCollect };
            }

            return new HomeCollectResult
            {
                ErrorCode = ErrorCode.ERR_Success,
                ItemConfigIds = new List<int>(),
                ItemCounts = new List<int>(),
                WealthDelta = wealthDelta,
                CollectTickCount = tickCount,
                CollectIntervalMs = collectIntervalMs,
            };
        }

        public static void ApplyCollect(Unit unit, long buildingId, long collectTickCount, int collectIntervalMs)
        {
            if (collectTickCount <= 0 || collectIntervalMs <= 0)
            {
                return;
            }

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            HomeBuilding building = homeComp?.GetChild<HomeBuilding>(buildingId);
            if (building == null)
            {
                return;
            }

            building.LastCollectTime += collectTickCount * collectIntervalMs;
            building.LastProductionTime = TimeInfo.Instance.ServerNow();
            building.State = HomeBuildingState.Idle;
        }

        public static void RefreshState(HomeBuilding building)
        {
            if (building == null)
            {
                return;
            }

            HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (config != null && config.BuildingType == HomeBuildingType.Farm && PeekFarmWealth(building) > 0)
            {
                building.State = HomeBuildingState.Collecting;
                return;
            }

            building.State = HomeBuildingState.Idle;
        }

        public static long PeekFarmWealth(HomeBuilding building)
        {
            if (building == null)
            {
                return 0;
            }

            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (buildingConfig == null || buildingConfig.BuildingType != HomeBuildingType.Farm)
            {
                return 0;
            }

            HomeBuildingLevelConfig levelConfig = HomeConfigHelper.GetBuildingLevelConfig(building.ConfigId, building.Level);
            if (levelConfig == null)
            {
                return 0;
            }

            int collectIntervalMs = HomeConfigHelper.GetExtraInt(levelConfig, "collectIntervalMs", DEFAULT_FARM_COLLECT_INTERVAL_MS);
            if (collectIntervalMs <= 0)
            {
                return 0;
            }

            long elapsed = TimeInfo.Instance.ServerNow() - building.LastCollectTime;
            long tickCount = elapsed / collectIntervalMs;
            return tickCount <= 0 ? 0 : tickCount * levelConfig.OutputValue;
        }
    }
}
