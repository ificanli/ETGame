using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 完整流程测试：起装选择 → 进局 → 拾取物品 → 撤离结算
    /// TDD: 验证整个局外起装功能链路端到端正确
    /// </summary>
    public class Equipment_FullFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_FullFlow_Test));
            Fiber testFiber = scope.TestFiber;

            // Step 1: 登录到 Gate
            Fiber robot = await testFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, nameof(Equipment_FullFlow_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> clientSceneRef = clientScene;

            await LoginHelper.Login(clientScene, "127.0.0.1:10101", nameof(Equipment_FullFlow_Test), "");
            clientScene = clientSceneRef;

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();
            EntityRef<ClientSenderComponent> senderRef = sender;

            // Step 2: 获取英雄列表
            G2C_GetHeroList heroListResp = await sender.Call(C2G_GetHeroList.Create()) as G2C_GetHeroList;
            clientScene = clientSceneRef;
            sender = senderRef;
            if (heroListResp == null || heroListResp.Heroes == null || heroListResp.Heroes.Count == 0)
            {
                Log.Console($"GetHeroList failed");
                return 1;
            }

            // Step 3: 确认起装
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = heroListResp.Heroes[0].HeroConfigId;
            confirmReq.MainWeaponConfigId = 10001;
            confirmReq.SubWeaponConfigId = 10002;
            confirmReq.ArmorConfigId = 10003;

            G2C_ConfirmLoadout confirmResp = await sender.Call(confirmReq) as G2C_ConfirmLoadout;
            clientScene = clientSceneRef;
            if (confirmResp == null || confirmResp.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"ConfirmLoadout failed, error={confirmResp?.Error}");
                return 2;
            }

            // Step 4: 进入地图
            await EnterMapHelper.EnterMapAsync(clientScene);
            clientScene = clientSceneRef;

            Unit unit = TestHelper.GetServerUnit(testFiber, robot);
            EntityRef<Unit> unitRef = unit;
            if (unit == null)
            {
                Log.Console($"server unit is null after entering map");
                return 3;
            }

            // Step 5: 验证起装装备已应用
            Server.EquipmentComponent equipComp = unit.GetComponent<Server.EquipmentComponent>();
            if (equipComp == null || !equipComp.HasEquippedItem(EquipmentSlotType.MainHand))
            {
                Log.Console($"loadout not applied: MainHand slot empty");
                return 4;
            }

            // Step 6: 局内拾取额外物品
            Server.ItemComponent serverItem = unit.GetComponent<Server.ItemComponent>();
            ItemHelper.AddItem(serverItem, 20001, 2, ItemChangeReason.MonsterDrop);

            clientScene = clientSceneRef;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            clientScene = clientSceneRef;

            unit = unitRef;
            // Step 7: 撤离结算
            await EvacuationSettlementHelper.Settle(unit);
            unit = unitRef;

            clientScene = clientSceneRef;
            Wait_M2C_EvacuationSettlement waitResult = await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_EvacuationSettlement>();
            clientScene = clientSceneRef;

            M2C_EvacuationSettlement settlement = waitResult.M2C_EvacuationSettlement;
            if (settlement == null || !settlement.Success)
            {
                Log.Console($"evacuation settlement failed");
                return 5;
            }

            // 验证结算包含起装装备 + 拾取物品
            bool hasPickedItem = false;
            bool hasEquipItem = false;
            foreach (ItemData item in settlement.Items)
            {
                if (item.ConfigId == 20001) hasPickedItem = true;
                if (item.ConfigId == 10001) hasEquipItem = true;
            }

            if (!hasPickedItem)
            {
                Log.Console($"settlement missing picked item ConfigId=20001");
                return 6;
            }

            if (!hasEquipItem)
            {
                Log.Console($"settlement missing equipped item ConfigId=10001");
                return 7;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
