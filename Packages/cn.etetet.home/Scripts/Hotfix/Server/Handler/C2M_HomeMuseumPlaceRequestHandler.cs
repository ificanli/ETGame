namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeMuseumPlaceRequestHandler : MessageLocationHandler<Unit, C2M_HomeMuseumPlaceRequest, M2C_HomeMuseumPlaceResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeMuseumPlaceRequest request, M2C_HomeMuseumPlaceResponse response)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                response.Error = ErrorCode.ERR_HomeNotInHomeScene;
                return;
            }

            HomeBuilding building = homeComp.GetChild<HomeBuilding>(request.BuildingId);
            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building?.ConfigId ?? 0);
            if (buildingConfig == null || buildingConfig.BuildingType != HomeBuildingType.Museum)
            {
                response.Error = ErrorCode.ERR_HomeBuildingNotFound;
                response.Message = "目标建筑不是大红收藏馆。";
                return;
            }

            if (HomeMuseumHelper.CountDisplays(homeComp, request.BuildingId) >= HomeMuseumHelper.GetMuseumCapacity(building))
            {
                response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                response.Message = "当前收藏馆展示位已满，请先取下部分展示单位。";
                return;
            }

            EntityRef<Unit> unitRef = unit;
            EntityRef<PlayerHomeComponent> homeRef = homeComp;
            G2Map_HomeStorageOperateResponse storageResponse = await HomeStorageGateHelper.TakeWarehouseItemByUid(unit, request.ItemUid);
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

            if (storageResponse == null)
            {
                response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                response.Message = "从仓库取出展示单位失败。";
                return;
            }

            if (storageResponse.Error != ErrorCode.ERR_Success)
            {
                response.Error = storageResponse.Error;
                response.Message = storageResponse.Message;
                return;
            }

            int itemConfigId = storageResponse.ResultItemConfigId;
            if (itemConfigId <= 0)
            {
                response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                response.Message = "取出的展示物品配置无效。";
                return;
            }

            int error = HomeMuseumHelper.PlaceDisplay(homeComp, request.BuildingId, itemConfigId, out HomeMuseumDisplayData display, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                int rollbackError = await HomeStorageGateHelper.AddWarehouseItem(unit, itemConfigId, 1);
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

                response.Error = rollbackError == ErrorCode.ERR_Success ? error : rollbackError;
                response.Message = rollbackError == ErrorCode.ERR_Success
                    ? message
                    : "收藏馆摆放失败，且回滚到仓库时失败。";
                return;
            }

            response.Display = HomeSnapshotMessageHelper.CreateMuseumDisplayInfo(display);
            response.ItemConfigId = itemConfigId;
            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
