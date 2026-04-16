namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeBuildRequestHandler : MessageLocationHandler<Unit, C2M_HomeBuildRequest, M2C_HomeBuildResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeBuildRequest request, M2C_HomeBuildResponse response)
        {
            int error = HomeBuildHelper.CheckBuild(unit, request.SlotId, request.ConfigId, out int buildGoldCost, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = message;
                return;
            }

            EntityRef<Unit> unitRef = unit;
            if (buildGoldCost > 0)
            {
                error = await HomeStorageGateHelper.ApplyWealthDelta(unit, HomeStorageOperationType.SpendWealth, -buildGoldCost);
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

            error = HomeBuildHelper.Build(unit, request.SlotId, request.ConfigId);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                return;
            }

            HomeBuilding building = HomeBuildHelper.GetBuildingBySlot(unit, request.SlotId);
            if (building != null)
            {
                response.Building = HomeSnapshotMessageHelper.CreateBuildingInfo(building);
            }

            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
