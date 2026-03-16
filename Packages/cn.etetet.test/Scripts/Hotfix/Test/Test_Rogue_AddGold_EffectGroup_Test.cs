using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_AddGold_EffectGroup_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_AddGold_EffectGroup_Test));
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

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                numericComponent.SetNoEvent(numericType, numericValue);
            }

            unit.AddComponent<BuffComponent>();

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 2;
            }

            if (!configCategory.TryGetOption(1, out RogueOptionConfig optionConfig) || optionConfig == null)
            {
                Log.Console("rogue option config 1 is null");
                return 3;
            }

            string oldBtConfig = optionConfig.BTConfig;
            int oldEffectGroupId = optionConfig.EffectGroupId;
            const int effectGroupId = 7016;
            const int goldAmount = 123;

            try
            {
                optionConfig.BTConfig = $"group:{effectGroupId}";
                optionConfig.EffectGroupId = 0;

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = effectGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddGold,
                            Value1 = goldAmount,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 4;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(1);

                EntityRef<Unit> unitRef = unit;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 1, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after choose");
                    return 5;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"choose option failed, error={chooseError}");
                    return 6;
                }

                if (progress.CurrentGold != goldAmount)
                {
                    Log.Console($"gold mismatch, gold={progress.CurrentGold}, expected={goldAmount}");
                    return 7;
                }

                if (progress.SelectedOptionIds.Count != 1 || progress.SelectedOptionIds[0] != 1)
                {
                    Log.Console($"selected option mismatch, count={progress.SelectedOptionIds.Count}");
                    return 8;
                }

                if (progress.AppliedBuffIds.Count != 0)
                {
                    Log.Console($"applied buff count mismatch, count={progress.AppliedBuffIds.Count}");
                    return 9;
                }

                if (unit.GetComponent<RogueEffectRuntimeComponent>() != null)
                {
                    Log.Console("runtime component should remain null for pure add gold effect");
                    return 10;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                optionConfig.BTConfig = oldBtConfig;
                optionConfig.EffectGroupId = oldEffectGroupId;
                configCategory.RemoveEffectGroup(effectGroupId);
            }
        }
    }
}
