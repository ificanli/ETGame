namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeDemolishRequestHandler : MessageLocationHandler<Unit, C2M_HomeDemolishRequest, M2C_HomeDemolishResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeDemolishRequest request, M2C_HomeDemolishResponse response)
        {
            int error = HomeDemolishHelper.CheckDemolish(unit, request.BuildingId, out HomeBuilding building, out int refundGold, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = message;
                return;
            }

            EntityRef<Unit> unitRef = unit;
            if (refundGold > 0)
            {
                error = await HomeStorageGateHelper.ApplyWealthDelta(unit, HomeStorageOperationType.AddWealth, refundGold);
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

            error = HomeDemolishHelper.Demolish(unit, request.BuildingId);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                return;
            }

            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
