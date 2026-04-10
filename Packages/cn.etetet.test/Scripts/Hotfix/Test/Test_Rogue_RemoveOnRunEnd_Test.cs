using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_RemoveOnRunEnd_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_RemoveOnRunEnd_Test));
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
            if (!TestHelper.TryFindKeepableRogueOption(configCategory, excludedOptionIds, out int optionId1, out RogueOptionConfig optionConfig1, out int buffConfigId1))
            {
                Log.Console("first keepable rogue option not found");
                return 3;
            }

            excludedOptionIds.Add(optionId1);
            if (!TestHelper.TryFindKeepableRogueOption(configCategory, excludedOptionIds, out int optionId2, out RogueOptionConfig optionConfig2, out int buffConfigId2))
            {
                Log.Console("second keepable rogue option not found");
                return 4;
            }

            string oldBtConfig1 = optionConfig1.BTConfig;
            string oldBtConfig2 = optionConfig2.BTConfig;
            int oldEffectGroupId1 = optionConfig1.EffectGroupId;
            int oldEffectGroupId2 = optionConfig2.EffectGroupId;

            const int removeOnRunEndGroupId = 7014;
            const int keepOnRunEndGroupId = 7015;

            try
            {
                optionConfig1.BTConfig = $"group:{removeOnRunEndGroupId}";
                optionConfig1.EffectGroupId = 0;
                optionConfig2.BTConfig = $"group:{keepOnRunEndGroupId}";
                optionConfig2.EffectGroupId = 0;

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = removeOnRunEndGroupId,
                    RemoveOnRunEnd = true,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddBuff,
                            RefId = buffConfigId1,
                        },
                    },
                });

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = keepOnRunEndGroupId,
                    RemoveOnRunEnd = false,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddBuff,
                            RefId = buffConfigId2,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 7;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(optionId1);

                EntityRef<Unit> unitRef = unit;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, optionId1, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after first choose");
                    return 8;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"first choose failed, error={chooseError}");
                    return 9;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 2;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(optionId2);

                unitRef = unit;
                progressRef = progress;
                chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, optionId2, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after second choose");
                    return 10;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"second choose failed, error={chooseError}");
                    return 11;
                }

                if (progress.SelectedOptionIds.Count != 2)
                {
                    Log.Console($"selected option count mismatch before run-end remove, count={progress.SelectedOptionIds.Count}");
                    return 12;
                }

                int removedCount = RogueEffectHelper.RemoveRunEndEffectGroups(unit, progress);
                if (removedCount != 1)
                {
                    Log.Console($"remove on run end count mismatch, count={removedCount}");
                    return 13;
                }

                if (progress.SelectedOptionIds.Count != 1 || progress.SelectedOptionIds[0] != optionId2)
                {
                    Log.Console($"selected options mismatch after run-end remove, count={progress.SelectedOptionIds.Count}");
                    return 14;
                }

                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null || runtimeComponent.ChildrenCount() != 1)
                {
                    Log.Console($"runtime count mismatch after run-end remove, count={runtimeComponent?.ChildrenCount() ?? 0}");
                    return 15;
                }

                RogueEffectRuntime runtime = null;
                foreach (Entity entity in runtimeComponent.Children.Values)
                {
                    runtime = entity as RogueEffectRuntime;
                    break;
                }

                if (runtime == null || runtime.EffectGroupId != keepOnRunEndGroupId)
                {
                    Log.Console($"remaining runtime mismatch, groupId={runtime?.EffectGroupId ?? 0}");
                    return 16;
                }

                if (progress.AppliedBuffIds.Count != 1)
                {
                    Log.Console($"applied buff count mismatch after run-end remove, count={progress.AppliedBuffIds.Count}");
                    return 17;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                optionConfig1.BTConfig = oldBtConfig1;
                optionConfig1.EffectGroupId = oldEffectGroupId1;
                optionConfig2.BTConfig = oldBtConfig2;
                optionConfig2.EffectGroupId = oldEffectGroupId2;
                configCategory.RemoveEffectGroup(removeOnRunEndGroupId);
                configCategory.RemoveEffectGroup(keepOnRunEndGroupId);
            }
        }

    }
}
