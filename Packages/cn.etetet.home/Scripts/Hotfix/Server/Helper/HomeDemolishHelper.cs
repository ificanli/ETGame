namespace ET.Server
{
    /// <summary>
    /// 拆除逻辑
    /// </summary>
    public static class HomeDemolishHelper
    {
        /// <summary>
        /// 拆除指定建筑
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
