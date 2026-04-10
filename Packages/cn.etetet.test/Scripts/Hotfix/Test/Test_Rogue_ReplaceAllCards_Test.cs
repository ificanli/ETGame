using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_ReplaceAllCards_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_ReplaceAllCards_Test));
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

            HashSet<int> excludedOptionIds = new();
            if (!TestHelper.TryFindKeepableRogueOption(configCategory, excludedOptionIds, out int baseOptionId, out _, out _))
            {
                Log.Console("base keepable rogue option not found");
                return 3;
            }

            excludedOptionIds.Add(baseOptionId);
            if (!TestHelper.TryFindKeepableRogueOption(configCategory, excludedOptionIds, out int grantOptionId, out _, out _))
            {
                Log.Console("grant keepable rogue option not found");
                return 4;
            }

            excludedOptionIds.Add(grantOptionId);
            if (!TestHelper.TryFindExecutableRogueOption(configCategory, excludedOptionIds, out int resetOptionId, out RogueOptionConfig resetOptionConfig))
            {
                Log.Console("reset rogue option not found");
                return 5;
            }

            string oldResetBtConfig = resetOptionConfig.BTConfig;
            int oldResetEffectGroupId = resetOptionConfig.EffectGroupId;

            const int effectGroupId = 7012;

            try
            {
                resetOptionConfig.BTConfig = $"group:{effectGroupId}";
                resetOptionConfig.EffectGroupId = 0;

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = effectGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.ReplaceAllCards,
                        },
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.GrantRandomCard,
                            RefId = grantOptionId,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 6;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(baseOptionId);

                EntityRef<Unit> unitRef = unit;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, baseOptionId, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after first base choose");
                    return 7;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"first base choose failed, error={chooseError}");
                    return 8;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 2;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(baseOptionId);

                unitRef = unit;
                progressRef = progress;
                chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, baseOptionId, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after second base choose");
                    return 9;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"second base choose failed, error={chooseError}");
                    return 10;
                }

                if (progress.SelectedOptionIds.Count != 2)
                {
                    Log.Console($"selected option count mismatch before reset, count={progress.SelectedOptionIds.Count}");
                    return 11;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 3;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(resetOptionId);

                unitRef = unit;
                progressRef = progress;
                chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, resetOptionId, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after reset choose");
                    return 12;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"reset choose failed, error={chooseError}");
                    return 13;
                }

                if (progress.SelectedOptionIds.Count != 1)
                {
                    Log.Console($"selected option count mismatch after reset, count={progress.SelectedOptionIds.Count}");
                    return 14;
                }

                if (progress.SelectedOptionIds[0] != grantOptionId)
                {
                    Log.Console($"granted option mismatch, optionId={progress.SelectedOptionIds[0]}, expected={grantOptionId}");
                    return 15;
                }

                if (progress.SelectedOptionIds.Contains(resetOptionId))
                {
                    Log.Console("reset option should not remain in selected options");
                    return 16;
                }

                if (progress.AppliedBuffIds.Count != 1)
                {
                    Log.Console($"applied buff count mismatch after reset, count={progress.AppliedBuffIds.Count}");
                    return 17;
                }

                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null || runtimeComponent.ChildrenCount() != 1)
                {
                    Log.Console($"runtime component count mismatch after reset, count={runtimeComponent?.ChildrenCount() ?? 0}");
                    return 18;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                resetOptionConfig.BTConfig = oldResetBtConfig;
                resetOptionConfig.EffectGroupId = oldResetEffectGroupId;
                configCategory.RemoveEffectGroup(effectGroupId);
            }
        }

    }
}
