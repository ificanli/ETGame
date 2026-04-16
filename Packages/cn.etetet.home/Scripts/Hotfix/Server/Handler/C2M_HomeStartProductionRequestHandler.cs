namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_HomeStartProductionRequestHandler : MessageLocationHandler<Unit, C2M_HomeStartProductionRequest, M2C_HomeStartProductionResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_HomeStartProductionRequest request, M2C_HomeStartProductionResponse response)
        {
            int error = HomeProductionHelper.CheckStartProduction(unit, request.BuildingId, request.RecipeId, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = message;
                return;
            }

            EntityRef<Unit> unitRef = unit;
            error = await HomeStorageGateHelper.ConsumeWarehouseItem(unit, request.RecipeId, 1);
            unit = unitRef;
            if (unit == null)
            {
                return;
            }

            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = "仓库中没有足够的可回收物品。";
                return;
            }

            HomeProductionStartResult result = HomeProductionHelper.StartProduction(unit, request.BuildingId, request.RecipeId);
            if (result.ErrorCode != ErrorCode.ERR_Success)
            {
                response.Error = result.ErrorCode;
                await HomeStorageGateHelper.AddWarehouseItem(unit, request.RecipeId, 1);
                return;
            }

            HomeProductionComponent prodComp = unit.GetComponent<HomeProductionComponent>();
            if (prodComp != null && prodComp.ProductionOrders.TryGetValue(result.OrderId, out HomeProductionOrderData order))
            {
                response.Order = HomeSnapshotMessageHelper.CreateOrderInfo(order);
            }

            MapMessageHelper.NoticeClient(unit, HomeSnapshotMessageHelper.CreateSnapshot(unit), NoticeType.Self);
        }
    }
}
