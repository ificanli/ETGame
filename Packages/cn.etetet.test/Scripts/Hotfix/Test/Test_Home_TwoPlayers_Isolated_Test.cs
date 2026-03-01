using ET.Client;

namespace ET.Test
{
    public class Test_Home_TwoPlayers_Isolated_Test: ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Home_TwoPlayers_Isolated_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, $"{nameof(Test_Home_TwoPlayers_Isolated_Test)}_1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, $"{nameof(Test_Home_TwoPlayers_Isolated_Test)}_2");
            scene1 = scene1Ref;
            Scene scene2 = robot2.Root;

            PlayerComponent playerComponent1 = scene1.GetComponent<PlayerComponent>();
            PlayerComponent playerComponent2 = scene2.GetComponent<PlayerComponent>();
            if (playerComponent1 == null || playerComponent2 == null)
            {
                Log.Console($"playerComponent is null, p1Null={playerComponent1 == null}, p2Null={playerComponent2 == null}");
                return 1;
            }

            long playerId1 = playerComponent1.MyId;
            long playerId2 = playerComponent2.MyId;
            if (playerId1 == playerId2)
            {
                Log.Console($"player ids should be different, playerId1={playerId1}, playerId2={playerId2}");
                return 2;
            }

            Scene currentScene1 = scene1.CurrentScene();
            Scene currentScene2 = scene2.CurrentScene();
            if (currentScene1 == null || currentScene2 == null)
            {
                Log.Console($"currentScene is null, s1Null={currentScene1 == null}, s2Null={currentScene2 == null}");
                return 3;
            }

            if (currentScene1.Name.GetSceneConfigName() != "Home" || currentScene2.Name.GetSceneConfigName() != "Home")
            {
                Log.Console($"scene is not Home, scene1={currentScene1.Name}, scene2={currentScene2.Name}");
                return 4;
            }

            if (currentScene1.Id == currentScene2.Id)
            {
                Log.Console($"two players share same home map id={currentScene1.Id}");
                return 5;
            }

            if (currentScene1.Id != playerId1 || currentScene2.Id != playerId2)
            {
                Log.Console($"home map id mismatch, mapId1={currentScene1.Id}, playerId1={playerId1}, mapId2={currentScene2.Id}, playerId2={playerId2}");
                return 6;
            }

            UnitComponent unitComponent1 = currentScene1.GetComponent<UnitComponent>();
            UnitComponent unitComponent2 = currentScene2.GetComponent<UnitComponent>();
            if (unitComponent1 == null || unitComponent2 == null)
            {
                Log.Console($"unitComponent is null, u1Null={unitComponent1 == null}, u2Null={unitComponent2 == null}");
                return 7;
            }

            if (unitComponent1.Get(playerId2) != null)
            {
                Log.Console($"player2 is visible in player1 home, player2Id={playerId2}");
                return 8;
            }

            if (unitComponent2.Get(playerId1) != null)
            {
                Log.Console($"player1 is visible in player2 home, player1Id={playerId1}");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
