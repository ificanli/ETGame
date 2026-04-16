namespace ET.Server
{
    /// <summary>
    /// 进入Home时的加载逻辑
    /// </summary>
    public static class HomeEnterHelper
    {
        /// <summary>
        /// 玩家进入Home时调用，加载或创建基地数据
        /// </summary>
        public static async ETTask OnEnterHome(Unit unit)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                // 创建默认基地数据（首次进入）
                homeComp = unit.AddComponent<PlayerHomeComponent>();
                homeComp.HomeVersion = 1;
                homeComp.LastSettleTime = TimeInfo.Instance.ServerNow();

                Log.Debug($"HomeEnterHelper: created default PlayerHomeComponent for unit {unit.Id}");
            }

            HomeRuntimeHelper.EnsureMainCity(homeComp);
            HomeRuntimeHelper.RefreshUnlockState(homeComp);

            if (unit.GetComponent<HomeProductionComponent>() == null)
            {
                unit.AddComponent<HomeProductionComponent>();
            }

            HomeContractComponent contractComp = unit.GetComponent<HomeContractComponent>();
            if (contractComp == null)
            {
                contractComp = unit.AddComponent<HomeContractComponent>();
            }

            if (contractComp.AvailableContracts.Count == 0 && contractComp.ActiveContracts.Count == 0)
            {
                HomeContractHelper.RefreshContracts(unit);
            }

            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is HomeBuilding building)
                {
                    HomeCollectHelper.RefreshState(building);
                }
            }

            EntityRef<Unit> unitRef = unit;
            long unitId = unit.Id;
            int storageError = await HomeStorageGateHelper.RefreshSummary(unit);
            unit = unitRef;
            if (storageError != ErrorCode.ERR_Success)
            {
                Log.Warning($"HomeEnterHelper: refresh storage summary failed, unit={unitId}, error={storageError}");
            }
        }
    }
}
