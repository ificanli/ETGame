namespace ET.Server
{
    /// <summary>
    /// 建造逻辑
    /// </summary>
    public static class HomeBuildHelper
    {
        /// <summary>
        /// 在指定槽位建造建筑
        /// </summary>
        public static int Build(Unit unit, int slotId, int configId)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            // 校验槽位未被占用
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is HomeBuilding existing && existing.SlotId == slotId)
                {
                    return ErrorCode.ERR_HomeSlotOccupied;
                }
            }

            // 创建建筑
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
    }
}
