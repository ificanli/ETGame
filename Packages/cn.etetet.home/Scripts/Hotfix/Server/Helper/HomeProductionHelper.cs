using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 工坊生产逻辑
    /// </summary>
    public static class HomeProductionHelper
    {
        // 默认生产时长 5分钟(ms) — TODO: 从配置表读取
        public const long DefaultDuration = 5 * 60 * 1000;
        // 默认产出物品
        public const int DefaultOutputItemId = 1002;
        public const int DefaultOutputCount = 1;

        public static HomeProductionStartResult StartProduction(Unit unit, long buildingId, int recipeId)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return new HomeProductionStartResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            HomeBuilding building = homeComp.GetChild<HomeBuilding>(buildingId);
            if (building == null)
            {
                return new HomeProductionStartResult { ErrorCode = ErrorCode.ERR_HomeBuildingNotFound };
            }

            HomeProductionComponent prodComp = unit.GetComponent<HomeProductionComponent>();
            if (prodComp == null)
            {
                return new HomeProductionStartResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            // 单队列限制：检查是否有进行中的订单
            foreach (var kv in prodComp.ProductionOrders)
            {
                if (kv.Value.State == HomeProductionState.InProgress)
                {
                    return new HomeProductionStartResult { ErrorCode = ErrorCode.ERR_HomeProductionQueueFull };
                }
            }

            long now = TimeInfo.Instance.ServerNow();
            long orderId = IdGenerater.Instance.GenerateId();

            var order = new HomeProductionOrderData
            {
                OrderId = orderId,
                BuildingEntityId = buildingId,
                RecipeId = recipeId,
                State = HomeProductionState.InProgress,
                StartTime = now,
                FinishTime = now + DefaultDuration
            };

            prodComp.ProductionOrders[orderId] = order;

            Log.Debug($"HomeProductionHelper: started production orderId={orderId}, recipeId={recipeId}");
            return new HomeProductionStartResult { ErrorCode = ErrorCode.ERR_Success, OrderId = orderId };
        }

        public static HomeCollectResult CollectProduction(Unit unit, long orderId)
        {
            HomeProductionComponent prodComp = unit.GetComponent<HomeProductionComponent>();
            if (prodComp == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            if (!prodComp.ProductionOrders.TryGetValue(orderId, out HomeProductionOrderData order))
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeProductionNotFound };
            }

            long now = TimeInfo.Instance.ServerNow();

            // 惰性判定完成
            if (order.State == HomeProductionState.InProgress && now >= order.FinishTime)
            {
                order.State = HomeProductionState.Completed;
            }

            if (order.State != HomeProductionState.Completed)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeProductionNotReady };
            }

            order.State = HomeProductionState.Collected;

            Log.Debug($"HomeProductionHelper: collected production orderId={orderId}");
            return new HomeCollectResult
            {
                ErrorCode = ErrorCode.ERR_Success,
                ItemConfigIds = new List<int> { DefaultOutputItemId },
                ItemCounts = new List<int> { DefaultOutputCount }
            };
        }
    }

}
