using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 验证装备与槽位不匹配时被拒绝：把护甲 ConfigId 填入主武器槽位应该返回错误
    /// TDD: 此测试在 C2G_ConfirmLoadoutHandler 实现 EquipSlot 验证后通过
    /// 注：20001 是护甲类 ConfigId（EquipSlot=2），用于验证主武器槽(EquipSlot=6)拒绝错误类型物品
    /// </summary>
    public class Equipment_LoadoutSlotMismatch_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutSlotMismatch_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await testFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, nameof(Equipment_LoadoutSlotMismatch_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> rootRef = clientScene;

            await LoginHelper.Login(clientScene, "127.0.0.1:10101", nameof(Equipment_LoadoutSlotMismatch_Test), "");
            clientScene = rootRef;

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();
            EntityRef<ClientSenderComponent> senderRef = sender;

            // 先获取第一个英雄ID
            G2C_GetHeroList heroListResp = await sender.Call(C2G_GetHeroList.Create()) as G2C_GetHeroList;
            if (heroListResp == null || heroListResp.Heroes == null || heroListResp.Heroes.Count == 0)
            {
                Log.Console($"failed to get hero list");
                return 1;
            }

            // 把护甲ConfigId(20001)填入主武器槽位 — 应被拒绝
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = heroListResp.Heroes[0].HeroConfigId;
            confirmReq.MainWeaponConfigId = 20001; // 护甲ConfigId，不应被接受为主武器
            confirmReq.SubWeaponConfigId = 10002;
            confirmReq.ArmorConfigId = 10003;

            sender = senderRef;
            G2C_ConfirmLoadout confirmResp = await sender.Call(confirmReq, needException: false) as G2C_ConfirmLoadout;
            sender = senderRef;
            if (confirmResp == null)
            {
                Log.Console($"expected error response for slot mismatch, got null");
                return 2;
            }

            if (confirmResp.Error == ErrorCode.ERR_Success)
            {
                Log.Console($"expected error for slot mismatch (armor ConfigId in weapon slot), but got ERR_Success");
                return 3;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
