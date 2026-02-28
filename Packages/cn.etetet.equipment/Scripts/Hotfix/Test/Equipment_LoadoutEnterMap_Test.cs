using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证起装确认后进入地图，服务端 Unit 有正确的英雄配置和起装装备
    /// TDD: 此测试在步骤6（修改 C2G_EnterMapHandler + LoadoutHelper.ApplyLoadout）之前会失败
    /// </summary>
    public class Equipment_LoadoutEnterMap_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutEnterMap_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建 robot，只登录不进图
            Fiber robot = await testFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, nameof(Equipment_LoadoutEnterMap_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> rootRef = clientScene;

            await LoginHelper.Login(clientScene, "127.0.0.1:10101", nameof(Equipment_LoadoutEnterMap_Test), "");
            clientScene = rootRef;

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();
            EntityRef<ClientSenderComponent> senderRef = sender;

            // Step 1: 获取英雄列表
            G2C_GetHeroList heroListResp = await sender.Call(C2G_GetHeroList.Create()) as G2C_GetHeroList;
            if (heroListResp == null || heroListResp.Heroes == null || heroListResp.Heroes.Count == 0)
            {
                Log.Console($"failed to get hero list");
                return 1;
            }

            int heroConfigId = heroListResp.Heroes[0].HeroConfigId;
            int expectedUnitConfigId = heroListResp.Heroes[0].UnitConfigId;

            // Step 2: 确认起装
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = heroConfigId;
            confirmReq.MainWeaponConfigId = 10001;
            confirmReq.SubWeaponConfigId = 10002;
            confirmReq.ArmorConfigId = 10003;

            sender = senderRef;
            G2C_ConfirmLoadout confirmResp = await sender.Call(confirmReq) as G2C_ConfirmLoadout;
            clientScene = rootRef;
            sender = senderRef;
            if (confirmResp == null || confirmResp.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"ConfirmLoadout failed, error={confirmResp?.Error}");
                return 2;
            }

            // Step 3: 进入地图
            await EnterMapHelper.EnterMapAsync(clientScene);
            clientScene = rootRef;

            // Step 4: 验证服务端 Unit
            Unit unit = TestHelper.GetServerUnit(testFiber, robot);
            if (unit == null)
            {
                Log.Console($"server unit is null after entering map");
                return 3;
            }

            // 验证 Unit 使用了正确的英雄 ConfigId
            if (unit.ConfigId != expectedUnitConfigId)
            {
                Log.Console($"unit.ConfigId={unit.ConfigId}, expected={expectedUnitConfigId} (from hero HeroConfigId={heroConfigId})");
                return 4;
            }

            // 验证 EquipmentComponent 存在
            Server.EquipmentComponent equipComp = unit.GetComponent<Server.EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Console($"EquipmentComponent is null after enter map with loadout");
                return 5;
            }

            // 验证主武器槽位有装备
            if (!equipComp.HasEquippedItem(EquipmentSlotType.MainHand))
            {
                Log.Console($"MainHand slot is empty after ApplyLoadout");
                return 6;
            }

            Server.Item mainWeapon = equipComp.GetEquippedItem(EquipmentSlotType.MainHand);
            if (mainWeapon.ConfigId != 10001)
            {
                Log.Console($"MainHand item ConfigId={mainWeapon.ConfigId}, expected=10001");
                return 7;
            }

            // 验证护甲槽位有装备
            if (!equipComp.HasEquippedItem(EquipmentSlotType.Chest))
            {
                Log.Console($"Chest slot is empty after ApplyLoadout");
                return 8;
            }

            Server.Item armor = equipComp.GetEquippedItem(EquipmentSlotType.Chest);
            if (armor.ConfigId != 10003)
            {
                Log.Console($"Chest item ConfigId={armor.ConfigId}, expected=10003");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
