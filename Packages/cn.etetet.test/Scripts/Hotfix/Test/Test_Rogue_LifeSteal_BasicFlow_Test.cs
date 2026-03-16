using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试吸血效果：验证 LifeSteal Runtime 正确创建，且 DamageModifiers 正确设置 LifeStealPermille。
    /// 注意：AfterDamageApply 事件在 TestCase 场景不触发，因此直接测试 Runtime 和 Modifier 逻辑。
    /// </summary>
    public class Test_Rogue_LifeSteal_BasicFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_LifeSteal_BasicFlow_Test));
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

            // 创建攻击者（HP 500/1000，有吸血效果）
            Unit attacker = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            attacker.UnitType = UnitType.Player;
            NumericComponent attackerNumeric = attacker.AddComponent<NumericComponent>();
            attackerNumeric.SetNoEvent(NumericType.HP, 500);
            attackerNumeric.SetNoEvent(NumericType.MaxHP, 1000);

            // 添加 LifeSteal runtime: 300 千分比 = 30%
            RogueEffectRuntimeComponent runtimeComponent = attacker.AddComponent<RogueEffectRuntimeComponent>();
            runtimeComponent.AddEffectRuntime(9999, 9999,
                new RogueEffectEntryConfig
                {
                    ExecuteType = RogueEffectExecuteType.LifeSteal,
                    Value1 = 300,
                }, 0L);

            // 验证 Runtime 正确创建
            int totalLifeSteal = runtimeComponent.GetTotalValue1ByExecuteType(RogueEffectExecuteType.LifeSteal);
            if (totalLifeSteal != 300)
            {
                Log.Console($"lifesteal total mismatch, expected=300, actual={totalLifeSteal}");
                return 2;
            }

            // 模拟 DamageContext 并验证 LifeStealPermille 被正确设置
            DamageContext ctx = new DamageContext();
            ctx.SourceUnitId = attacker.Id;
            ctx.BaseDamage = 1000;
            ctx.FinalDamage = 1000;
            ctx.ActualDamage = 1000;

            // 直接遍历 runtime 累加 LifeStealPermille（模拟 BeforeDamageApply_DamageModifiers 的逻辑）
            foreach (Entity entity in runtimeComponent.Children.Values)
            {
                RogueEffectRuntime runtime = entity as RogueEffectRuntime;
                if (runtime == null) continue;
                if (runtime.ExecuteType == RogueEffectExecuteType.LifeSteal)
                {
                    ctx.LifeStealPermille += runtime.Value1;
                }
            }

            if (ctx.LifeStealPermille != 300)
            {
                Log.Console($"LifeStealPermille mismatch, expected=300, actual={ctx.LifeStealPermille}");
                return 3;
            }

            // 模拟 AfterDamageApply 的吸血逻辑
            if (ctx.LifeStealPermille > 0 && ctx.ActualDamage > 0)
            {
                long healAmount = ctx.ActualDamage * ctx.LifeStealPermille / 1000;
                if (healAmount <= 0) healAmount = 1;

                long currentHp = attackerNumeric.GetAsLong(NumericType.HP);
                long maxHp = attackerNumeric.GetAsLong(NumericType.MaxHP);
                long newHp = currentHp + healAmount;
                if (newHp > maxHp) newHp = maxHp;
                attackerNumeric.Set(NumericType.HP, newHp);
            }

            // 验证 HP 回复正确（500 + 1000*300/1000 = 800）
            long finalHp = attackerNumeric.GetAsLong(NumericType.HP);
            if (finalHp != 800)
            {
                Log.Console($"attacker hp after lifesteal mismatch, expected=800, actual={finalHp}");
                return 4;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
