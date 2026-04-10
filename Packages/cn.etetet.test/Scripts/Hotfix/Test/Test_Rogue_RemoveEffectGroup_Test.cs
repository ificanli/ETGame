using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_RemoveEffectGroup_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_RemoveEffectGroup_Test));
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

            if (!TestHelper.TryFindKeepableRogueOption(configCategory, null, out int optionId, out RogueOptionConfig optionConfig, out int buffConfigId))
            {
                Log.Console("failed to find keepable rogue option");
                return 3;
            }

            const int effectGroupId = 7011;
            const int objectiveId = 9011;
            string oldBtConfig = optionConfig.BTConfig;
            int oldEffectGroupId = optionConfig.EffectGroupId;

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
                            ExecuteType = RogueEffectExecuteType.AddBuff,
                            RefId = buffConfigId,
                        },
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.RegisterObjective,
                            RefId = objectiveId,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 5;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(optionId);

                EntityRef<Unit> unitRef = unit;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, optionId, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after choose");
                    return 6;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"choose option failed, error={chooseError}");
                    return 7;
                }

                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null || runtimeComponent.ChildrenCount() != 1)
                {
                    Log.Console($"runtime component count mismatch, count={runtimeComponent?.ChildrenCount() ?? 0}");
                    return 8;
                }

                RogueEffectRuntime runtime = null;
                foreach (Entity entity in runtimeComponent.Children.Values)
                {
                    runtime = entity as RogueEffectRuntime;
                    break;
                }

                if (runtime == null || runtime.AppliedBuffId <= 0)
                {
                    Log.Console($"runtime buff id invalid, buffId={runtime?.AppliedBuffId ?? 0}");
                    return 9;
                }

                RogueObjectiveComponent objectiveComponent = unit.GetComponent<RogueObjectiveComponent>();
                if (objectiveComponent == null || objectiveComponent.ChildrenCount() != 1)
                {
                    Log.Console($"objective component count mismatch, count={objectiveComponent?.ChildrenCount() ?? 0}");
                    return 10;
                }

                int removedOptionCount = RogueEffectHelper.RemoveEffectGroup(unit, progress, effectGroupId);
                if (removedOptionCount != 1)
                {
                    Log.Console($"removed option count mismatch, count={removedOptionCount}");
                    return 11;
                }

                if (progress.SelectedOptionIds.Count != 0)
                {
                    Log.Console($"selected option count mismatch after remove, count={progress.SelectedOptionIds.Count}");
                    return 12;
                }

                if (progress.AppliedBuffIds.Count != 0)
                {
                    Log.Console($"applied buff count mismatch after remove, count={progress.AppliedBuffIds.Count}");
                    return 13;
                }

                if (unit.GetComponent<RogueEffectRuntimeComponent>() != null)
                {
                    Log.Console("runtime component should be removed when group is removed");
                    return 14;
                }

                if (unit.GetComponent<RogueObjectiveComponent>() != null)
                {
                    Log.Console("objective component should be removed when group is removed");
                    return 15;
                }

                BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
                if (buffComponent?.GetChild<Buff>(runtime.AppliedBuffId) != null)
                {
                    Log.Console($"buff should be removed when group is removed, buffId={runtime.AppliedBuffId}");
                    return 16;
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
