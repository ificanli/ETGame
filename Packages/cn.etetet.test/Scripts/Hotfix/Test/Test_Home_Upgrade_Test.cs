using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试建筑升级功能
    /// </summary>
    public class Test_Home_Upgrade_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_Upgrade_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_Upgrade_Test));
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

            // 建造一个建筑
            int buildResult = Server.HomeBuildHelper.Build(serverUnit, 1, 1);
            if (buildResult != ErrorCode.ERR_Success)
            {
                Log.Console($"build failed, error={buildResult}");
                return 3;
            }

            // 找到建筑
            Server.HomeBuilding building = null;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding b && b.SlotId == 1)
                {
                    building = b;
                    break;
                }
            }

            if (building == null)
            {
                Log.Console("building not found after build");
                return 4;
            }

            // 升级建筑
            int upgradeResult = Server.HomeUpgradeHelper.Upgrade(serverUnit, building.Id);
            if (upgradeResult != ErrorCode.ERR_Success)
            {
                Log.Console($"upgrade failed, error={upgradeResult}");
                return 5;
            }

            if (building.Level != 2)
            {
                Log.Console($"expected level=2 after upgrade, got {building.Level}");
                return 6;
            }

            // 验证最大等级限制（假设最大5级）
            building.Level = 5;
            int maxResult = Server.HomeUpgradeHelper.Upgrade(serverUnit, building.Id);
            if (maxResult != ErrorCode.ERR_HomeBuildingMaxLevel)
            {
                Log.Console($"expected ERR_HomeBuildingMaxLevel at level 5, got {maxResult}");
                return 7;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
