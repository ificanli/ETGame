namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeCollectProductionRequestHandler : MessageLocationHandler<Unit, C2M_HomeCollectProductionRequest, M2C_HomeCollectProductionResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeCollectProductionRequest request, M2C_HomeCollectProductionResponse response)
        {
            HomeCollectResult result = HomeProductionHelper.CollectProduction(unit, request.OrderId);
            if (result.ErrorCode != ErrorCode.ERR_Success)
            {
                response.Error = result.ErrorCode;
                return;
            }

            if (!HomeProductionHelper.CanStoreOutput(unit, result.ItemConfigIds, result.ItemCounts, out string message))
            {
                response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                response.Message = message;
                return;
            }

            EntityRef<Unit> unitRef = unit;
            for (int i = 0; i < result.ItemConfigIds.Count && i < result.ItemCounts.Count; ++i)
            {
                int error = await HomeStorageGateHelper.AddWarehouseItem(unit, result.ItemConfigIds[i], result.ItemCounts[i]);
                unit = unitRef;
                if (unit == null)
                {
                    return;
                }

                if (error != ErrorCode.ERR_Success)
                {
                    response.Error = error;
                    response.Message = "回收产物入仓失败。";
                    return;
                }
            }

            HomeProductionHelper.ClearCollectedOrder(unit, request.OrderId);
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp != null)
            {
                homeComp.RecycleCollectedCount++;
            }

            response.ItemConfigIds.AddRange(result.ItemConfigIds);
            response.ItemCounts.AddRange(result.ItemCounts);
            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
