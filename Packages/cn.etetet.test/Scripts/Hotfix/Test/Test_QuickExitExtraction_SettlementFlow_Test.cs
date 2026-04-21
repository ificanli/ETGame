using ET.Client;
using ET.Server;

namespace ET.Test
{
    public class Test_QuickExitExtraction_SettlementFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            int testItemConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(10001);
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_QuickExitExtraction_SettlementFlow_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_QuickExitExtraction_SettlementFlow_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> clientSceneRef = clientScene;

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();
            M2C_TransferMap transferResponse = await sender.Call(C2M_TransferMap.Create()) as M2C_TransferMap;
            clientScene = clientSceneRef;
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
            if (string.IsNullOrWhiteSpace(mapName) || mapName == "Home")
            {
                Log.Console($"unexpected map after transfer, map={mapName}");
                return 3;
            }

            await clientScene.Root().TimerComponent.WaitAsync(200);
            clientScene = clientSceneRef;

            Unit serverUnit = TestHelper.GetServerUnit(testFiber, robot);
            EntityRef<Unit> serverUnitRef = serverUnit;
            if (serverUnit == null)
            {
                Log.Console("server unit is null");
                return 4;
            }

            Server.ItemComponent serverItem = serverUnit.GetComponent<Server.ItemComponent>();
            if (serverItem == null)
            {
                Log.Console("ItemComponent is null on unit");
                return 5;
            }

            ItemHelper.AddItem(serverItem, 10001, 3, ItemChangeReason.QuestReward);
            clientScene = clientSceneRef;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            clientScene = clientSceneRef;
            serverUnit = serverUnitRef;

            SettlementClientComponent settlementRuntime = clientScene.GetComponent<SettlementClientComponent>();
            settlementRuntime?.ResetRuntime();

            ETTask<Wait_M2C_EvacuationSettlement> settlementWaitTask =
                    clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_EvacuationSettlement>().NewContext(null);
            ETTask<Wait_SceneChangeFinish> sceneChangeWaitTask =
                    clientScene.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>().NewContext(null);

            sender = clientScene.GetComponent<ClientSenderComponent>();
            M2C_QuickExitExtraction quickExitResponse = await sender.Call(C2M_QuickExitExtraction.Create()) as M2C_QuickExitExtraction;
            clientScene = clientSceneRef;
            if (quickExitResponse == null || quickExitResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"quick exit failed, error={quickExitResponse?.Error}, msg={quickExitResponse?.Message}");
                return 6;
            }

            Wait_M2C_EvacuationSettlement settlementWait = await settlementWaitTask;
            clientScene = clientSceneRef;
            if (settlementWait.M2C_EvacuationSettlement == null || !settlementWait.M2C_EvacuationSettlement.Success)
            {
                Log.Console("evacuation settlement failed");
                return 7;
            }

            bool foundCarryItem = false;
            foreach (ItemData itemData in settlementWait.M2C_EvacuationSettlement.Items)
            {
                if (LegacyItemConfigIdHelper.MatchesConfigId(itemData.ConfigId, testItemConfigId) && itemData.Count == 3)
                {
                    foundCarryItem = true;
                    break;
                }
            }

            if (!foundCarryItem)
            {
                Log.Console($"evacuation settlement missing test item ConfigId={testItemConfigId} Count=3");
                return 8;
            }

            if (settlementWait.M2C_EvacuationSettlement.TotalWealth <= 0)
            {
                Log.Console($"evacuation settlement wealth invalid: {settlementWait.M2C_EvacuationSettlement.TotalWealth}");
                return 9;
            }

            await sceneChangeWaitTask;
            clientScene = clientSceneRef;
            if (clientScene == null || clientScene.IsDisposed)
            {
                Log.Console("client scene disposed after quick exit scene change");
                return 10;
            }

            mapName = clientScene.CurrentScene()?.Name.GetSceneConfigName() ?? string.Empty;
            if (mapName != "Home")
            {
                Log.Console($"quick exit should transfer to Home, actual={mapName}");
                return 11;
            }

            settlementRuntime = clientScene.GetComponent<SettlementClientComponent>();
            if (settlementRuntime == null || !settlementRuntime.HasSettlement || !settlementRuntime.IsSuccess)
            {
                Log.Console(
                    $"settlement runtime invalid: has={settlementRuntime?.HasSettlement ?? false}, success={settlementRuntime?.IsSuccess ?? false}");
                return 12;
            }

            if (settlementRuntime.TotalWealth != settlementWait.M2C_EvacuationSettlement.TotalWealth)
            {
                Log.Console(
                    $"settlement runtime wealth mismatch: runtime={settlementRuntime.TotalWealth}, msg={settlementWait.M2C_EvacuationSettlement.TotalWealth}");
                return 13;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
