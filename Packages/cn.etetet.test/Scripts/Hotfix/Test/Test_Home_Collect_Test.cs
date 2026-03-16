using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试建筑被动产出收取
    /// </summary>
    public class Test_Home_Collect_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_Collect_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_Collect_Test));
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

            // 建造一个可收取建筑 (configId=2, 补给站)
            int buildResult = Server.HomeBuildHelper.Build(serverUnit, 2, 2);
            if (buildResult != ErrorCode.ERR_Success)
            {
                Log.Console($"build failed, error={buildResult}");
                return 3;
            }

            // 找到建筑
            Server.HomeBuilding building = null;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding b && b.SlotId == 2)
                {
                    building = b;
                    break;
                }
            }

            if (building == null)
            {
                Log.Console("building not found");
                return 4;
            }

            // 手动设置 LastCollectTime 为1小时前，模拟时间流逝
            long oneHourAgo = TimeInfo.Instance.ServerNow() - 3600 * 1000;
            building.LastCollectTime = oneHourAgo;

            // 执行收取
            var collectResult = Server.HomeCollectHelper.Collect(serverUnit, building.Id);
            if (collectResult.ErrorCode != ErrorCode.ERR_Success)
            {
                Log.Console($"collect failed, error={collectResult.ErrorCode}");
                return 5;
            }

            // 验证收取到了物品 (至少1种物品)
            if (collectResult.ItemConfigIds == null || collectResult.ItemConfigIds.Count == 0)
            {
                Log.Console("collect returned no items");
                return 6;
            }

            // 验证 LastCollectTime 已更新（不再是1小时前）
            long timeDiff = System.Math.Abs(building.LastCollectTime - TimeInfo.Instance.ServerNow());
            if (timeDiff > 1000) // 允许1秒误差
            {
                Log.Console($"LastCollectTime not updated, diff={timeDiff}ms");
                return 7;
            }

            // 对刚收取完的建筑再次收取，应没有产出
            var collectResult2 = Server.HomeCollectHelper.Collect(serverUnit, building.Id);
            if (collectResult2.ErrorCode != ErrorCode.ERR_HomeNothingToCollect)
            {
                Log.Console($"expected ERR_HomeNothingToCollect on immediate re-collect, got {collectResult2.ErrorCode}");
                return 8;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
