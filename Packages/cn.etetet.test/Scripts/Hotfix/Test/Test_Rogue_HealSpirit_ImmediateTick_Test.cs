using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_HealSpirit_ImmediateTick_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_HealSpirit_ImmediateTick_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

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

            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            unit.UnitType = UnitType.Player;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                numeric.SetNoEvent(numericType, numericValue);
            }

            unit.AddComponent<BuffComponent>();

            RogueBuffConfigLoader.EnsureRegistered();

            BuffConfig buffConfig = BuffConfigCategory.Instance.Get(1045);
            if (buffConfig == null)
            {
                Log.Console("rogue buff config 1045 is null");
                return 2;
            }

            EffectServerBuffTick tickEffect = buffConfig.GetEffect<EffectServerBuffTick>();
            if (tickEffect == null)
            {
                Log.Console("rogue buff 1045 tick effect is null");
                return 3;
            }

            if (tickEffect.Unit != "Unit")
            {
                Log.Console($"rogue buff 1045 tick root unit key invalid: {tickEffect.Unit}");
                return 4;
            }

            if (tickEffect.Children == null || tickEffect.Children.Count != 1 || tickEffect.Children[0] is not BTRogueHealMaxHpPermille tickNode)
            {
                Log.Console($"rogue buff 1045 tick children invalid, count={tickEffect.Children?.Count ?? 0}");
                return 5;
            }

            if (tickNode.Unit != "Unit")
            {
                Log.Console($"rogue buff 1045 tick child unit key invalid: {tickNode.Unit}");
                return 6;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null || !configCategory.TryGetOption(1045, out RogueOptionConfig optionConfig) || optionConfig == null)
            {
                Log.Console("rogue option config 1045 is null");
                return 7;
            }

            if (!RogueOptionConfigHelper.TryGetBuffConfigId(optionConfig, out int resolvedBuffConfigId) || resolvedBuffConfigId != 1045)
            {
                Log.Console($"rogue option 1045 resolved buff invalid: {resolvedBuffConfigId}");
                return 8;
            }

            long maxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (maxHp <= 0)
            {
                Log.Console($"max hp invalid: {maxHp}");
                return 9;
            }

            long startHp = maxHp / 2;
            if (startHp <= 0)
            {
                startHp = 1;
            }

            numeric.Set(NumericType.HP, startHp);

            long expectedHeal = maxHp * tickNode.HealPermille / 1000;
            if (expectedHeal <= 0)
            {
                expectedHeal = 1;
            }

            long expectedHp = startHp + expectedHeal;
            if (expectedHp > maxHp)
            {
                expectedHp = maxHp;
            }

            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 10;
            }

            progress.ChoicePending = true;
            progress.ChoiceSerial = 1;
            progress.PendingOptionIds.Clear();
            progress.PendingOptionIds.Add(1045);

            EntityRef<Unit> unitRef = unit;
            EntityRef<RogueProgressComponent> progressRef = progress;
            int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 1045, null);
            unit = unitRef;
            progress = progressRef;
            if (unit == null || progress == null)
            {
                Log.Console("unit or progress disposed after choose");
                return 11;
            }

            if (chooseError != ErrorCode.ERR_Success)
            {
                Log.Console($"choose option 1045 failed, error={chooseError}");
                return 12;
            }

            long actualHp = unit.NumericComponent?.GetAsLong(NumericType.HP) ?? 0;
            if (actualHp != expectedHp)
            {
                Log.Console($"rogue option 1045 immediate heal mismatch, hp={actualHp}, expected={expectedHp}, startHp={startHp}, maxHp={maxHp}");
                return 13;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
