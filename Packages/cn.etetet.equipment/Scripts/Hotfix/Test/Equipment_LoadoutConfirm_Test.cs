using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证起装选择和确认流程：登录到 Gate → GetHeroList → ConfirmLoadout → 验证
    /// TDD: 此测试在实现 C2G_GetHeroListHandler / C2G_ConfirmLoadoutHandler 之前会失败
    /// </summary>
    public class Equipment_LoadoutConfirm_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutConfirm_Test));
            Fiber testFiber = scope.TestFiber;

            // 只登录，不进入地图（与 TestHelper.CreateRobot 的区别在于跳过 EnterMapAsync）
            Fiber robot = await testFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, nameof(Equipment_LoadoutConfirm_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> rootRef = clientScene;

            await LoginHelper.Login(clientScene, "127.0.0.1:10101", nameof(Equipment_LoadoutConfirm_Test), "");
            clientScene = rootRef;

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();
            EntityRef<ClientSenderComponent> senderRef = sender;

            // Step 1: 获取英雄列表
            G2C_GetHeroList heroListResp = await sender.Call(C2G_GetHeroList.Create()) as G2C_GetHeroList;
            if (heroListResp == null || heroListResp.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"GetHeroList failed or null response");
                return 1;
            }

            if (heroListResp.Heroes == null || heroListResp.Heroes.Count == 0)
            {
                Log.Console($"hero list is empty, expected at least 1 hero from config");
                return 2;
            }

            HeroInfoData firstHero = heroListResp.Heroes[0];
            int heroConfigId = firstHero.HeroConfigId;

            // Step 2: 确认起装（使用第一个英雄和默认武器/护甲）
            // 注：ConfigId 10001/10002/10003 需要在 EquipmentConfig 中存在
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = heroConfigId;
            confirmReq.MainWeaponConfigId = 10001;   // 主武器
            confirmReq.SubWeaponConfigId = 10002;    // 副武器
            confirmReq.ArmorConfigId = 10003;        // 护甲
            confirmReq.ConsumableConfigIds.Add(10004); // 消耗品

            sender = senderRef;
            G2C_ConfirmLoadout confirmResp = await sender.Call(confirmReq) as G2C_ConfirmLoadout;
            sender = senderRef;
            if (confirmResp == null || confirmResp.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"ConfirmLoadout failed, error={confirmResp?.Error}");
                return 3;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
