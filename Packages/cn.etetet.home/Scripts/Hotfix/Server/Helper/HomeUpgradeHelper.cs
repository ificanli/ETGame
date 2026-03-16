namespace ET.Server
{
    /// <summary>
    /// 升级逻辑
    /// </summary>
    public static class HomeUpgradeHelper
    {
        public const int DefaultMaxLevel = 5;

        /// <summary>
        /// 升级指定建筑
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

            if (building.Level >= DefaultMaxLevel)
            {
                return ErrorCode.ERR_HomeBuildingMaxLevel;
            }

            building.Level++;

            Log.Debug($"HomeUpgradeHelper: upgraded building {buildingId} to level {building.Level}");
            return ErrorCode.ERR_Success;
        }
    }
}
