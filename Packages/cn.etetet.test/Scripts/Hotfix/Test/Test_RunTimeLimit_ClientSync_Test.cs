using ET.Client;
using ET.Server;

namespace ET.Test
{
    public class Test_RunTimeLimit_ClientSync_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_RunTimeLimit_ClientSync_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_RunTimeLimit_ClientSync_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> clientSceneRef = clientScene;
            EntityRef<Scene> rootRef = clientScene.Root();

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();
            EntityRef<ClientSenderComponent> senderRef = sender;
            M2C_TransferMap transferResponse = await sender.Call(C2M_TransferMap.Create()) as M2C_TransferMap;
            clientScene = clientSceneRef;
            sender = senderRef;
            if (transferResponse == null || transferResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"transfer map failed, error={transferResponse?.Error}");
                return 1;
            }

            await clientScene.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            clientScene = clientSceneRef;
            if (clientScene == null || clientScene.IsDisposed)
            {
                Log.Console("client scene disposed after transfer");
                return 2;
            }

            string mapName = clientScene.CurrentScene()?.Name.GetSceneConfigName() ?? string.Empty;
            if (mapName == "Home" || string.IsNullOrEmpty(mapName))
            {
                Log.Console($"unexpected map after transfer, map={mapName}");
                return 3;
            }

            Scene root = rootRef;
            await root.TimerComponent.WaitAsync(200);
            clientScene = clientSceneRef;

            RunTimeLimitClientComponent clientRuntime = clientScene.GetComponent<RunTimeLimitClientComponent>();
            if (clientRuntime == null || !clientRuntime.IsActive)
            {
                Log.Console("run time limit client runtime is not active after entering map");
                return 4;
            }

            long firstRemainMs = clientRuntime.RemainMs;
            if (firstRemainMs <= 0 || firstRemainMs > RunTimeLimitConst.PlayerTimeoutMs)
            {
                Log.Console($"run time limit remain ms is invalid after entering map: {firstRemainMs}");
                return 5;
            }

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console("server unit is null");
                return 6;
            }

            EntityRef<Unit> serverUnitRef = serverUnit;

            RunTimeLimitComponent runTimeLimit = serverUnit.GetComponent<RunTimeLimitComponent>();
            if (runTimeLimit == null)
            {
                Log.Console("server run time limit is null");
                return 7;
            }

            runTimeLimit.AdjustDuration(300000);
            root = rootRef;
            await root.TimerComponent.WaitAsync(200);
            clientScene = clientSceneRef;

            clientRuntime = clientScene.GetComponent<RunTimeLimitClientComponent>();
            if (clientRuntime == null || !clientRuntime.IsActive)
            {
                Log.Console("run time limit client runtime is inactive after adjust duration");
                return 8;
            }

            if (clientRuntime.RemainMs < firstRemainMs + 240000)
            {
                Log.Console($"run time limit remain ms did not increase enough: before={firstRemainMs}, after={clientRuntime.RemainMs}");
                return 9;
            }

            serverUnit = serverUnitRef;
            if (serverUnit == null || serverUnit.IsDisposed)
            {
                Log.Console("server unit disposed before clear runtime limit");
                return 10;
            }

            serverUnit.RemoveComponent<RunTimeLimitComponent>();
            root = rootRef;
            await root.TimerComponent.WaitAsync(200);
            clientScene = clientSceneRef;

            clientRuntime = clientScene.GetComponent<RunTimeLimitClientComponent>();
            if (clientRuntime == null)
            {
                Log.Console("run time limit client runtime is null after clear");
                return 11;
            }

            if (clientRuntime.IsActive || clientRuntime.RemainMs != 0 || clientRuntime.EndTimeMs != 0)
            {
                Log.Console($"run time limit client runtime should be cleared: active={clientRuntime.IsActive}, remain={clientRuntime.RemainMs}, end={clientRuntime.EndTimeMs}");
                return 12;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
