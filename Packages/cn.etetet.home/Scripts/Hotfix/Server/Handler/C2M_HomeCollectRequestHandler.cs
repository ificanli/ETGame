namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeCollectRequestHandler : MessageLocationHandler<Unit, C2M_HomeCollectRequest, M2C_HomeCollectResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeCollectRequest request, M2C_HomeCollectResponse response)
        {
            HomeCollectResult result = HomeCollectHelper.Collect(unit, request.BuildingId);
            if (result.ErrorCode != ErrorCode.ERR_Success)
            {
                response.Error = result.ErrorCode;
                return;
            }

            EntityRef<Unit> unitRef = unit;
            int error = await HomeStorageGateHelper.ApplyWealthDelta(unit, HomeStorageOperationType.AddWealth, result.WealthDelta);
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

            HomeCollectHelper.ApplyCollect(unit, request.BuildingId, result.CollectTickCount, result.CollectIntervalMs);
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            HomeBuilding building = homeComp?.GetChild<HomeBuilding>(request.BuildingId);
            HomeBuildingConfig buildingConfig = HomeConfigHelper.GetBuildingConfig(building?.ConfigId ?? 0);
            if (homeComp != null && buildingConfig != null && buildingConfig.BuildingType == HomeBuildingType.Farm)
            {
                homeComp.FarmCollectedCount++;
            }

            response.ItemConfigIds.AddRange(result.ItemConfigIds);
            response.ItemCounts.AddRange(result.ItemCounts);
            response.WealthDelta = result.WealthDelta;
            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
