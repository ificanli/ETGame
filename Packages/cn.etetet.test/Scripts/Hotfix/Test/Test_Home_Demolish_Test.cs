using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试建筑拆除功能
    /// </summary>
    public class Test_Home_Demolish_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_Demolish_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_Demolish_Test));
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

            // 建造两个建筑
            int result1 = Server.HomeBuildHelper.Build(serverUnit, 1, 1);
            int result2 = Server.HomeBuildHelper.Build(serverUnit, 2, 2);
            if (result1 != ErrorCode.ERR_Success || result2 != ErrorCode.ERR_Success)
            {
                Log.Console($"build failed, result1={result1}, result2={result2}");
                return 3;
            }

            // 找到 slotId=2 的建筑
            Server.HomeBuilding building2 = null;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding b && b.SlotId == 2)
                {
                    building2 = b;
                    break;
                }
            }

            if (building2 == null)
            {
                Log.Console("building at slot 2 not found");
                return 4;
            }

            long buildingId = building2.Id;

            // 拆除建筑
            int demolishResult = Server.HomeDemolishHelper.Demolish(serverUnit, buildingId);
            if (demolishResult != ErrorCode.ERR_Success)
            {
                Log.Console($"demolish failed, error={demolishResult}");
                return 5;
            }

            // 验证建筑已移除
            int remainingCount = 0;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding)
                {
                    remainingCount++;
                }
            }

            if (remainingCount != 1)
            {
                Log.Console($"expected 1 remaining building, got {remainingCount}");
                return 6;
            }

            // 验证已拆除的槽位可以重新建造
            int rebuildResult = Server.HomeBuildHelper.Build(serverUnit, 2, 2);
            if (rebuildResult != ErrorCode.ERR_Success)
            {
                Log.Console($"rebuild on demolished slot failed, error={rebuildResult}");
                return 7;
            }

            // 验证拆除不存在的建筑返回错误
            int notFoundResult = Server.HomeDemolishHelper.Demolish(serverUnit, 999999);
            if (notFoundResult != ErrorCode.ERR_HomeBuildingNotFound)
            {
                Log.Console($"expected ERR_HomeBuildingNotFound, got {notFoundResult}");
                return 8;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
