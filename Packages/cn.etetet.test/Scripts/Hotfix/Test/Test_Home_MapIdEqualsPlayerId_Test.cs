using ET.Client;

namespace ET.Test
{
    public class Test_Home_MapIdEqualsPlayerId_Test: ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_MapIdEqualsPlayerId_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Home_MapIdEqualsPlayerId_Test));
            Scene clientScene = robot.Root;

            PlayerComponent playerComponent = clientScene.GetComponent<PlayerComponent>();
            if (playerComponent == null)
            {
                Log.Console($"playerComponent is null");
                return 1;
            }

            Scene currentScene = clientScene.CurrentScene();
            if (currentScene == null)
            {
                Log.Console($"currentScene is null");
                return 2;
            }

            if (currentScene.Name.GetSceneConfigName() != "Home")
            {
                Log.Console($"current scene is not Home, sceneName={currentScene.Name}");
                return 3;
            }

            if (currentScene.Id != playerComponent.MyId)
            {
                Log.Console($"home map id is not player id, mapId={currentScene.Id}, playerId={playerComponent.MyId}");
                return 4;
            }

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console($"server unit is null");
                return 5;
            }

            if (serverUnit.Id != playerComponent.MyId)
            {
                Log.Console($"server unit id mismatch, unitId={serverUnit.Id}, playerId={playerComponent.MyId}");
                return 6;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
