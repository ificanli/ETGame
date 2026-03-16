using System;
using ET.Client;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_ChooseOption_ClientFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_ChooseOption_ClientFlow_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_Rogue_ChooseOption_ClientFlow_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> clientSceneRef = clientScene;

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

            long choiceSerial = 0;
            int optionId = 0;
            bool popupReady = false;
            EntityRef<Scene> rootRef = clientScene.Root();
            for (int retry = 0; retry < 80; ++retry)
            {
                clientScene = clientSceneRef;
                if (clientScene == null || clientScene.IsDisposed)
                {
                    Log.Console("client scene disposed while waiting rogue popup");
                    return 4;
                }

                RogueClientComponent runtime = clientScene.GetComponent<RogueClientComponent>();
                if (runtime != null &&
                    runtime.ChoicePopupPending &&
                    runtime.ChoiceSerial > 0 &&
                    runtime.ChoiceOptions.Count > 0)
                {
                    choiceSerial = runtime.ChoiceSerial;
                    optionId = runtime.ChoiceOptions[0].OptionId;
                    popupReady = true;
                    break;
                }

                Scene root = rootRef;
                if (root == null || root.IsDisposed)
                {
                    Log.Console("root scene disposed while waiting rogue popup");
                    return 5;
                }

                await root.TimerComponent.WaitAsync(100);
            }

            if (!popupReady)
            {
                clientScene = clientSceneRef;
                RogueClientComponent runtime = clientScene?.GetComponent<RogueClientComponent>();
                Log.Console(
                    $"rogue popup not ready, map={clientScene?.CurrentScene()?.Name}, serial={runtime?.ChoiceSerial ?? 0}, optionCount={runtime?.ChoiceOptions.Count ?? 0}, pending={runtime?.ChoicePopupPending ?? false}");
                return 6;
            }

            try
            {
                clientScene = clientSceneRef;
                await RogueClientHelper.ChooseOption(clientScene, optionId);
            }
            catch (RpcException e)
            {
                Log.Console($"choose option rpc exception, optionId={optionId}, serial={choiceSerial}, error={e.Error}, msg={e.Message}");
                return 7;
            }
            catch (Exception e)
            {
                Log.Console($"choose option exception, optionId={optionId}, serial={choiceSerial}, error={e}");
                return 8;
            }

            clientScene = clientSceneRef;
            if (clientScene == null || clientScene.IsDisposed)
            {
                Log.Console("client scene disposed after choose option");
                return 9;
            }

            RogueClientComponent clientRuntime = clientScene.GetComponent<RogueClientComponent>();
            if (clientRuntime != null && (clientRuntime.ChoiceSerial != 0 || clientRuntime.ChoicePopupPending))
            {
                Log.Console(
                    $"client runtime not cleared after choose, serial={clientRuntime.ChoiceSerial}, pending={clientRuntime.ChoicePopupPending}, optionCount={clientRuntime.ChoiceOptions.Count}");
                return 10;
            }

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            if (serverUnit == null)
            {
                Log.Console("server unit is null after choose option");
                return 11;
            }

            RogueProgressComponent progress = serverUnit.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                Log.Console("rogue progress is null after choose option");
                return 12;
            }

            if (progress.ChoicePending)
            {
                Log.Console($"server progress still pending after choose, serial={progress.ChoiceSerial}");
                return 13;
            }

            if (!progress.SelectedOptionIds.Contains(optionId))
            {
                Log.Console($"server progress missing selected option, optionId={optionId}");
                return 14;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
