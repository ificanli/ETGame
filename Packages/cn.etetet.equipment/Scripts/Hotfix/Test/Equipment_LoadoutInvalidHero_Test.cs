using ET.Client;

namespace ET.Test
{
    /// <summary>
    /// 验证无效英雄ID被拒绝：发送不存在的 HeroConfigId=99999 应该返回错误
    /// TDD: 此测试在 C2G_ConfirmLoadoutHandler 实现验证逻辑后通过
    /// </summary>
    public class Equipment_LoadoutInvalidHero_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutInvalidHero_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await testFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, nameof(Equipment_LoadoutInvalidHero_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> rootRef = clientScene;

            await LoginHelper.Login(clientScene, "127.0.0.1:10101", nameof(Equipment_LoadoutInvalidHero_Test), "");
            clientScene = rootRef;

            ClientSenderComponent sender = clientScene.GetComponent<ClientSenderComponent>();

            // 发送无效的 HeroConfigId
            C2G_ConfirmLoadout confirmReq = C2G_ConfirmLoadout.Create();
            confirmReq.HeroConfigId = 99999; // 不存在的英雄ID
            confirmReq.MainWeaponConfigId = 10001;
            confirmReq.SubWeaponConfigId = 10002;
            confirmReq.ArmorConfigId = 10003;

            G2C_ConfirmLoadout confirmResp = await sender.Call(confirmReq, needException: false) as G2C_ConfirmLoadout;
            if (confirmResp == null)
            {
                Log.Console($"expected error response, got null");
                return 1;
            }

            if (confirmResp.Error == ErrorCode.ERR_Success)
            {
                Log.Console($"expected error for invalid HeroConfigId=99999, but got ERR_Success");
                return 2;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
