using global::ET;

namespace ET.Server
{
    /// <summary>
    /// 拆除逻辑
    /// </summary>
    public static class HomeDemolishHelper
    {
        /// <summary>
        /// 校验拆除，并返回返还金币。
        /// </summary>
        public static int CheckDemolish(Unit unit, long buildingId, out HomeBuilding building, out int refundGold, out string message)
        {
            building = null;
            refundGold = 0;
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

            HomeBuildingConfig config = HomeConfigHelper.GetBuildingConfig(building.ConfigId);
            if (config == null)
            {
                return ErrorCode.ERR_HomeBuildingNotFound;
            }

            if (config.BuildingType == HomeBuildingType.MainCity)
            {
                message = "主城不能拆除。";
                return ErrorCode.ERR_HomeBuildingCannotDemolish;
            }

            if (config.BuildingType == HomeBuildingType.Farm && HomeCollectHelper.PeekFarmWealth(building) > 0)
            {
                message = "农场还有未收取金币，不能拆除。";
                return ErrorCode.ERR_HomeBuildingCannotDemolish;
            }

            if (config.BuildingType == HomeBuildingType.Museum && HomeMuseumHelper.CountDisplays(homeComp, buildingId) > 0)
            {
                message = "收藏馆还有已摆放展示单位，不能拆除。";
                return ErrorCode.ERR_HomeBuildingCannotDemolish;
            }

            if (config.BuildingType == HomeBuildingType.RecycleRoom)
            {
                HomeProductionComponent productionComp = unit.GetComponent<HomeProductionComponent>();
                if (HomeProductionHelper.CountActiveOrders(productionComp, buildingId) > 0)
                {
                    message = "回收间还有未完成或待收取的回收订单，不能拆除。";
                    return ErrorCode.ERR_HomeBuildingCannotDemolish;
                }
            }

            if (config.BuildingType == HomeBuildingType.Warehouse && homeComp.WarehouseItemCount > 0)
            {
                message = "仓库内还有物品，不能拆除。";
                return ErrorCode.ERR_HomeBuildingCannotDemolish;
            }

            refundGold = HomeRuntimeHelper.CalculateInvestedGold(building) * 70 / 100;
            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 拆除指定建筑。
        /// </summary>
        public static int Demolish(Unit unit, long buildingId)
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

            int slotId = building.SlotId;
            int configId = building.ConfigId;
            building.Dispose();

            Log.Debug($"HomeDemolishHelper: demolished building configId={configId} at slotId={slotId} for unit {unit.Id}");
            return ErrorCode.ERR_Success;
        }
    }
}
