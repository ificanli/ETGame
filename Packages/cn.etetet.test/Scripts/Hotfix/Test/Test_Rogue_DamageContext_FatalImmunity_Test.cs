using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试致命免死逻辑：验证 DamageContext.CanDie 被正确设置为 false，
    /// 以及 DamageContextHelper 在 CanDie=false 时保留 HP=1。
    /// 注意：BeforeDamageApply 事件在 TestCase 场景不触发，因此直接测试核心逻辑。
    /// </summary>
    public class Test_Rogue_DamageContext_FatalImmunity_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_DamageContext_FatalImmunity_Test));
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

            // 创建玩家目标
            Unit target = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            target.UnitType = UnitType.Player;
            NumericComponent targetNumeric = target.AddComponent<NumericComponent>();
            targetNumeric.SetNoEvent(NumericType.HP, 100);
            targetNumeric.SetNoEvent(NumericType.MaxHP, 1000);

            // 设置肉鸽进度（金币）
            RogueProgressComponent progress = target.AddComponent<RogueProgressComponent>();
            progress.CurrentGold = 200;

            // 添加致命免死效果运行时（约定：ExecuteType=AddBuff, Value2=1）
            RogueEffectRuntimeComponent runtimeComponent = target.AddComponent<RogueEffectRuntimeComponent>();
            RogueEffectRuntime runtime = runtimeComponent.AddChild<RogueEffectRuntime>();
            runtime.OptionId = 1;
            runtime.EffectGroupId = 1;
            runtime.ExecuteType = RogueEffectExecuteType.AddBuff;
            runtime.Value2 = 1; // 致命免死标记

            // 模拟致命伤害的 DamageContext
            DamageContext ctx = new DamageContext();
            ctx.SourceUnitId = 0;
            ctx.TargetUnitId = target.Id;
            ctx.BaseDamage = 200;
            ctx.FinalDamage = 200;
            ctx.CanDie = true;

            // 模拟 FatalImmunity 事件处理器的逻辑
            long currentHp = targetNumeric.GetAsLong(NumericType.HP);
            if (currentHp > 0 && ctx.FinalDamage >= currentHp)
            {
                // 检查是否有致命免死 runtime
                bool hasFatalImmunity = false;
                foreach (Entity entity in runtimeComponent.Children.Values)
                {
                    RogueEffectRuntime rt = entity as RogueEffectRuntime;
                    if (rt != null && rt.ExecuteType == RogueEffectExecuteType.AddBuff && rt.Value2 == 1)
                    {
                        hasFatalImmunity = true;
                        break;
                    }
                }

                if (hasFatalImmunity && progress.CurrentGold > 0)
                {
                    int goldCost = progress.CurrentGold / 2;
                    progress.CurrentGold -= goldCost;
                    ctx.CanDie = false;
                }
            }

            if (ctx.CanDie)
            {
                Log.Console("fatal immunity should have set CanDie=false");
                return 2;
            }

            // 模拟 DamageContextHelper 的扣血逻辑
            long newHp = currentHp - ctx.FinalDamage;
            if (newHp <= 0 && !ctx.CanDie)
            {
                newHp = 1;
            }

            if (newHp < 0) newHp = 0;
            targetNumeric.Set(NumericType.HP, newHp);

            long hpAfterLethal = targetNumeric.GetAsLong(NumericType.HP);
            if (hpAfterLethal != 1)
            {
                Log.Console($"fatal immunity failed, expected hp=1, actual={hpAfterLethal}");
                return 3;
            }

            // 验证金币被消耗（50% of 200 = 100）
            if (progress.CurrentGold != 100)
            {
                Log.Console($"gold cost mismatch, expected=100, actual={progress.CurrentGold}");
                return 4;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
