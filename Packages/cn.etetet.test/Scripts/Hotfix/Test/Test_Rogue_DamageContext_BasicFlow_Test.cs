using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试 DamageContext 基本伤害流程：伤害写入 HP，目标死亡时发布 UnitDie。
    /// </summary>
    public class Test_Rogue_DamageContext_BasicFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_DamageContext_BasicFlow_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();

            UnitConfig playerConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            // 创建攻击者
            Unit attacker = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            attacker.UnitType = UnitType.Player;
            NumericComponent attackerNumeric = attacker.AddComponent<NumericComponent>();
            attackerNumeric.SetNoEvent(NumericType.HP, 1000);

            // 创建目标
            Unit target = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            target.UnitType = UnitType.Monster;
            NumericComponent targetNumeric = target.AddComponent<NumericComponent>();
            targetNumeric.SetNoEvent(NumericType.HP, 500);

            // 施加 200 点伤害
            DamageContextHelper.ApplyDamage(scene, attacker, target, 200);

            long hpAfterDamage = targetNumeric.GetAsLong(NumericType.HP);
            if (hpAfterDamage != 300)
            {
                Log.Console($"hp after damage mismatch, expected=300, actual={hpAfterDamage}");
                return 2;
            }

            // 施加致命伤害（600 > 300 剩余HP）
            DamageContextHelper.ApplyDamage(scene, attacker, target, 600);

            long hpAfterLethal = targetNumeric.GetAsLong(NumericType.HP);
            if (hpAfterLethal != 0)
            {
                Log.Console($"hp after lethal damage mismatch, expected=0, actual={hpAfterLethal}");
                return 3;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
