using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试工坊生产功能
    /// </summary>
    public class Test_Home_Production_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_Production_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_Production_Test));
            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console("server unit is null");
                return 1;
            }

            Server.PlayerHomeComponent homeComp = serverUnit.GetComponent<Server.PlayerHomeComponent>();
            if (homeComp == null)
            {
                Log.Console("PlayerHomeComponent is null");
                return 2;
            }

            // 建造工坊 (configId=3)
            int buildResult = Server.HomeBuildHelper.Build(serverUnit, 3, 3);
            if (buildResult != ErrorCode.ERR_Success)
            {
                Log.Console($"build workshop failed, error={buildResult}");
                return 3;
            }

            // 找到工坊建筑
            Server.HomeBuilding workshop = null;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding b && b.SlotId == 3)
                {
                    workshop = b;
                    break;
                }
            }

            if (workshop == null)
            {
                Log.Console("workshop not found");
                return 4;
            }

            // 确保有 HomeProductionComponent
            Server.HomeProductionComponent prodComp = serverUnit.GetComponent<Server.HomeProductionComponent>();
            if (prodComp == null)
            {
                prodComp = serverUnit.AddComponent<Server.HomeProductionComponent>();
            }

            // 开始生产 (recipeId=1)
            var startResult = Server.HomeProductionHelper.StartProduction(serverUnit, workshop.Id, 1);
            if (startResult.ErrorCode != ErrorCode.ERR_Success)
            {
                Log.Console($"start production failed, error={startResult.ErrorCode}");
                return 5;
            }

            if (startResult.OrderId == 0)
            {
                Log.Console("orderId is 0");
                return 6;
            }

            // 验证队列中有一个订单
            if (prodComp.ProductionOrders.Count != 1)
            {
                Log.Console($"expected 1 order, got {prodComp.ProductionOrders.Count}");
                return 7;
            }

            // 1级回收间当前应支持 2 个处理位。
            var secondResult = Server.HomeProductionHelper.StartProduction(serverUnit, workshop.Id, 1);
            if (secondResult.ErrorCode != ErrorCode.ERR_Success)
            {
                Log.Console($"expected second order success, got {secondResult.ErrorCode}");
                return 8;
            }

            var queueFullResult = Server.HomeProductionHelper.StartProduction(serverUnit, workshop.Id, 1);
            if (queueFullResult.ErrorCode != ErrorCode.ERR_HomeProductionQueueFull)
            {
                Log.Console($"expected ERR_HomeProductionQueueFull, got {queueFullResult.ErrorCode}");
                return 9;
            }

            // 手动设置完成时间为过去，模拟生产完成
            Server.HomeProductionOrderData order = prodComp.ProductionOrders[startResult.OrderId];
            order.FinishTime = TimeInfo.Instance.ServerNow() - 1000;

            // 收取产物
            var collectResult = Server.HomeProductionHelper.CollectProduction(serverUnit, startResult.OrderId);
            if (collectResult.ErrorCode != ErrorCode.ERR_Success)
            {
                Log.Console($"collect production failed, error={collectResult.ErrorCode}");
                return 10;
            }

            // 验证获得产物
            if (collectResult.ItemConfigIds == null || collectResult.ItemConfigIds.Count == 0)
            {
                Log.Console("collect returned no items");
                return 11;
            }

            // 验证订单状态已更新为Collected
            if (order.State != HomeProductionState.Collected)
            {
                Log.Console($"expected Collected state, got {order.State}");
                return 12;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
