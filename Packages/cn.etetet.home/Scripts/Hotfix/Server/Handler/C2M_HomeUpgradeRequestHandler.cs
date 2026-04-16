namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeUpgradeRequestHandler : MessageLocationHandler<Unit, C2M_HomeUpgradeRequest, M2C_HomeUpgradeResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeUpgradeRequest request, M2C_HomeUpgradeResponse response)
        {
            int error = HomeUpgradeHelper.CheckUpgrade(unit, request.BuildingId, out HomeBuilding building, out int upgradeGoldCost, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = message;
                return;
            }

            EntityRef<Unit> unitRef = unit;
            if (upgradeGoldCost > 0)
            {
                error = await HomeStorageGateHelper.ApplyWealthDelta(unit, HomeStorageOperationType.SpendWealth, -upgradeGoldCost);
                unit = unitRef;
                if (unit == null)
                {
                    return;
                }

                if (error != ErrorCode.ERR_Success)
                {
                    response.Error = error;
                    return;
                }
            }

            error = HomeUpgradeHelper.Upgrade(unit, request.BuildingId);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                return;
            }

            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            building = homeComp?.GetChild<HomeBuilding>(request.BuildingId);
            if (building != null)
            {
                response.Building = HomeSnapshotMessageHelper.CreateBuildingInfo(building);
            }

            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
