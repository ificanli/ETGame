namespace ET.Server
{
    public static class HomeStorageGateHelper
    {
        public static async ETTask<int> RefreshSummary(Unit unit)
        {
            PlayerHomeComponent homeComp = unit?.GetComponent<PlayerHomeComponent>();
            if (unit == null || homeComp == null)
            {
                return ErrorCode.ERR_HomeNotInHomeScene;
            }

            UnitGateInfoComponent gateInfo = unit.GetComponent<UnitGateInfoComponent>();
            if (gateInfo == null || gateInfo.PlayerActorId == default)
            {
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            MessageSender sender = unit.Root().GetComponent<MessageSender>();
            if (sender == null)
            {
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            EntityRef<PlayerHomeComponent> homeRef = homeComp;
            G2Map_HomeStorageSummaryResponse response = await sender.Call(gateInfo.PlayerActorId, Map2G_HomeStorageSummaryRequest.Create()) as G2Map_HomeStorageSummaryResponse;

            homeComp = homeRef;
            if (homeComp == null)
            {
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            if (response == null)
            {
                return ErrorCode.ERR_HomePrerequisiteNotMet;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                return response.Error;
            }

            HomeRuntimeHelper.ApplyStorageSummary(homeComp, response.TotalWealth, response.WarehouseItemCount, response.WarehouseOccupiedCellCount);
            return ErrorCode.ERR_Success;
        }

        public static async ETTask<int> ApplyWealthDelta(Unit unit, int operationType, long wealthDelta)
        {
            G2Map_HomeStorageOperateResponse response = await ApplyStorageOperation(unit, operationType, wealthDelta, 0, 0, 0);
            return response?.Error ?? ErrorCode.ERR_HomePrerequisiteNotMet;
        }

        public static async ETTask<int> ConsumeWarehouseItem(Unit unit, int itemConfigId, int itemCount)
        {
            G2Map_HomeStorageOperateResponse response = await ApplyStorageOperation(unit, HomeStorageOperationType.ConsumeWarehouseItem, 0, itemConfigId, itemCount, 0);
            return response?.Error ?? ErrorCode.ERR_HomePrerequisiteNotMet;
        }

        public static async ETTask<int> AddWarehouseItem(Unit unit, int itemConfigId, int itemCount)
        {
            G2Map_HomeStorageOperateResponse response = await ApplyStorageOperation(unit, HomeStorageOperationType.AddWarehouseItem, 0, itemConfigId, itemCount, 0);
            return response?.Error ?? ErrorCode.ERR_HomePrerequisiteNotMet;
        }

        public static async ETTask<G2Map_HomeStorageOperateResponse> TakeWarehouseItemByUid(Unit unit, long itemUid, int itemCount = 1)
        {
            return await ApplyStorageOperation(unit, HomeStorageOperationType.TakeWarehouseItemByUid, 0, 0, itemCount, itemUid);
        }

        private static async ETTask<G2Map_HomeStorageOperateResponse> ApplyStorageOperation(
            Unit unit,
            int operationType,
            long wealthDelta,
            int itemConfigId,
            int itemCount,
            long itemUid)
        {
            G2Map_HomeStorageOperateResponse failedResponse = G2Map_HomeStorageOperateResponse.Create();
            PlayerHomeComponent homeComp = unit?.GetComponent<PlayerHomeComponent>();
            if (unit == null || homeComp == null)
            {
                failedResponse.Error = ErrorCode.ERR_HomeNotInHomeScene;
                return failedResponse;
            }

            UnitGateInfoComponent gateInfo = unit.GetComponent<UnitGateInfoComponent>();
            if (gateInfo == null || gateInfo.PlayerActorId == default)
            {
                failedResponse.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                return failedResponse;
            }

            MessageSender sender = unit.Root().GetComponent<MessageSender>();
            if (sender == null)
            {
                failedResponse.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                return failedResponse;
            }

            Map2G_HomeStorageOperateRequest request = Map2G_HomeStorageOperateRequest.Create();
            request.OperationType = operationType;
            request.WealthDelta = wealthDelta;
            request.ItemConfigId = itemConfigId;
            request.ItemCount = itemCount;
            request.ItemUid = itemUid;

            EntityRef<PlayerHomeComponent> homeRef = homeComp;
            G2Map_HomeStorageOperateResponse response = await sender.Call(gateInfo.PlayerActorId, request) as G2Map_HomeStorageOperateResponse;

            homeComp = homeRef;
            if (homeComp == null)
            {
                failedResponse.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                return failedResponse;
            }

            if (response == null)
            {
                failedResponse.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                return failedResponse;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                return response;
            }

            HomeRuntimeHelper.ApplyStorageSummary(homeComp, response.TotalWealth, response.WarehouseItemCount, response.WarehouseOccupiedCellCount);
            return response;
        }
    }
}
