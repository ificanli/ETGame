namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeMuseumTakeDownRequestHandler : MessageLocationHandler<Unit, C2M_HomeMuseumTakeDownRequest, M2C_HomeMuseumTakeDownResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeMuseumTakeDownRequest request, M2C_HomeMuseumTakeDownResponse response)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            HomeMuseumDisplayData display = HomeMuseumHelper.GetDisplay(homeComp, request.DisplayId);
            if (display == null)
            {
                response.Error = ErrorCode.ERR_HomeBuildingNotFound;
                response.Message = "没有找到要取下的展示单位。";
                return;
            }

            int itemConfigId = display.ItemConfigId;
            EntityRef<Unit> unitRef = unit;
            EntityRef<PlayerHomeComponent> homeRef = homeComp;
            int error = await HomeStorageGateHelper.AddWarehouseItem(unit, itemConfigId, 1);
            unit = unitRef;
            if (unit == null)
            {
                return;
            }

            homeComp = homeRef;
            if (homeComp == null)
            {
                return;
            }

            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = "展示单位放回仓库失败。";
                return;
            }

            error = HomeMuseumHelper.TakeDownDisplay(homeComp, request.DisplayId, out display, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = message;
                return;
            }

            response.DisplayId = request.DisplayId;
            response.ItemConfigId = display.ItemConfigId;
            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
