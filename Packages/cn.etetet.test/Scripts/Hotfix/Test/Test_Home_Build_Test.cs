using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试基地建造功能
    /// </summary>
    public class Test_Home_Build_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_Build_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_Build_Test));

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console("server unit is null");
                return 1;
            }

            // 验证 PlayerHomeComponent 存在
            Server.PlayerHomeComponent homeComp = serverUnit.GetComponent<Server.PlayerHomeComponent>();
            if (homeComp == null)
            {
                Log.Console("PlayerHomeComponent is null");
                return 2;
            }

            // 执行建造: slotId=1, configId=1
            int buildResult = Server.HomeBuildHelper.Build(serverUnit, 1, 1);
            if (buildResult != ErrorCode.ERR_Success)
            {
                Log.Console($"build failed, error={buildResult}");
                return 3;
            }

            // 验证建筑子Entity创建
            Server.HomeBuilding building = null;
            int buildingCount = 0;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding b)
                {
                    building = b;
                    buildingCount++;
                }
            }

            if (buildingCount != 1)
            {
                Log.Console($"expected 1 building, got {buildingCount}");
                return 4;
            }

            if (building.ConfigId != 1)
            {
                Log.Console($"expected configId=1, got {building.ConfigId}");
                return 5;
            }

            if (building.SlotId != 1)
            {
                Log.Console($"expected slotId=1, got {building.SlotId}");
                return 6;
            }

            if (building.Level != 1)
            {
                Log.Console($"expected level=1, got {building.Level}");
                return 7;
            }

            if (building.State != HomeBuildingState.Idle)
            {
                Log.Console($"expected state=Idle, got {building.State}");
                return 8;
            }

            // 验证槽位已占用：再次在同一槽位建造应失败
            int duplicateResult = Server.HomeBuildHelper.Build(serverUnit, 1, 1);
            if (duplicateResult != ErrorCode.ERR_HomeSlotOccupied)
            {
                Log.Console($"expected ERR_HomeSlotOccupied, got {duplicateResult}");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
