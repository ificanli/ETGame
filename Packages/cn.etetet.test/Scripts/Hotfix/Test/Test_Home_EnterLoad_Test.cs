using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 测试玩家进入Home后，PlayerHomeComponent正确加载
    /// </summary>
    public class Test_Home_EnterLoad_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_EnterLoad_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_EnterLoad_Test));
            Scene clientScene = robot.Root;

            // 验证当前场景是Home
            Scene currentScene = clientScene.CurrentScene();
            if (currentScene == null)
            {
                Log.Console("currentScene is null");
                return 1;
            }

            if (currentScene.Name.GetSceneConfigName() != "Home")
            {
                Log.Console($"current scene is not Home, sceneName={currentScene.Name}");
                return 2;
            }

            // 获取服务端Unit
            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console("server unit is null");
                return 3;
            }

            // 验证PlayerHomeComponent已加载
            Server.PlayerHomeComponent homeComp = serverUnit.GetComponent<Server.PlayerHomeComponent>();
            if (homeComp == null)
            {
                Log.Console("PlayerHomeComponent is null, HomeEnterHelper did not run");
                return 4;
            }

            // 验证首次进入时建筑数量为0
            int buildingCount = 0;
            foreach (Entity child in homeComp.Children.Values)
            {
                if (child is Server.HomeBuilding)
                {
                    buildingCount++;
                }
            }

            if (buildingCount != 0)
            {
                Log.Console($"first enter should have 0 buildings, got {buildingCount}");
                return 5;
            }

            // 验证LastSettleTime已初始化
            if (homeComp.LastSettleTime <= 0)
            {
                Log.Console($"LastSettleTime not initialized, value={homeComp.LastSettleTime}");
                return 6;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
