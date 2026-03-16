using System.Collections.Generic;
using System;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_GrantRandomCard_ByQuality_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_GrantRandomCard_ByQuality_Test));
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

            List<int> legacyOptions = new();
            foreach (KeyValuePair<int, RogueOptionConfig> kv in configCategory.GetOptions())
            {
                if (kv.Value == null ||
                    !kv.Value.TryGetLegacyBuffConfigId(out int buffConfigId) ||
                    !BuffConfigCategory.Instance.Contain(buffConfigId))
                {
                    continue;
                }

                legacyOptions.Add(kv.Key);
                if (legacyOptions.Count >= 2)
                {
                    break;
                }
            }

            if (legacyOptions.Count < 2)
            {
                Log.Console($"not enough legacy target options v2, count={legacyOptions.Count}");
                return 3;
            }

            int targetOptionId1 = legacyOptions[0];
            int targetOptionId2 = legacyOptions[1];
            int sourceOptionId = 0;
            foreach (KeyValuePair<int, RogueOptionConfig> kv in configCategory.GetOptions())
            {
                if (kv.Value == null || kv.Key == targetOptionId1 || kv.Key == targetOptionId2)
                {
                    continue;
                }

                sourceOptionId = kv.Key;
                break;
            }

            if (sourceOptionId <= 0)
            {
                Log.Console("source option not found for grant random card test");
                return 4;
            }

            RogueOptionConfig targetOptionConfig1 = configCategory.GetOptions()[targetOptionId1];
            RogueOptionConfig targetOptionConfig2 = configCategory.GetOptions()[targetOptionId2];
            RogueOptionConfig sourceOptionConfig = configCategory.GetOptions()[sourceOptionId];

            int oldQuality1 = targetOptionConfig1.Quality;
            int oldQuality2 = targetOptionConfig2.Quality;
            int[] oldShowTags1 = targetOptionConfig1.ShowTags;
            int[] oldHideTags1 = targetOptionConfig1.HideTags;
            int[] oldShowTags2 = targetOptionConfig2.ShowTags;
            int[] oldHideTags2 = targetOptionConfig2.HideTags;
            string oldBtConfig = sourceOptionConfig.BTConfig;
            int oldEffectGroupId = sourceOptionConfig.EffectGroupId;

            const int grantQuality = 88;
            const int effectGroupId = 7013;

            try
            {
                targetOptionConfig1.Quality = grantQuality;
                targetOptionConfig2.Quality = grantQuality;
                targetOptionConfig1.ShowTags = Array.Empty<int>();
                targetOptionConfig1.HideTags = Array.Empty<int>();
                targetOptionConfig2.ShowTags = Array.Empty<int>();
                targetOptionConfig2.HideTags = Array.Empty<int>();
                sourceOptionConfig.BTConfig = $"group:{effectGroupId}";
                sourceOptionConfig.EffectGroupId = 0;

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = effectGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.GrantRandomCard,
                            Value1 = grantQuality,
                            Value2 = 2,
                        },
                    },
                });

                HashSet<int> excludedOptionIds = new() { sourceOptionId };
                if (!RogueOptionRollHelper.TryRollOneOption(unit, configCategory, grantQuality, excludedOptionIds, out int previewOptionId))
                {
                    Log.Console($"preview roll failed, quality={grantQuality}");
                    return 5;
                }

                if (previewOptionId != targetOptionId1 && previewOptionId != targetOptionId2)
                {
                    Log.Console($"preview roll mismatch, optionId={previewOptionId}");
                    return 6;
                }

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 7;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(sourceOptionId);

                EntityRef<Unit> unitRef = unit;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, sourceOptionId, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after choose");
                    return 8;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"choose option failed, error={chooseError}");
                    return 9;
                }

                if (progress.SelectedOptionIds.Count != 3)
                {
                    Log.Console($"selected option count mismatch, count={progress.SelectedOptionIds.Count}");
                    return 10;
                }

                if (!progress.SelectedOptionIds.Contains(sourceOptionId))
                {
                    Log.Console($"source option missing, optionId={sourceOptionId}");
                    return 11;
                }

                if (!progress.SelectedOptionIds.Contains(targetOptionId1) || !progress.SelectedOptionIds.Contains(targetOptionId2))
                {
                    Log.Console($"granted options missing, option1={targetOptionId1}, option2={targetOptionId2}");
                    return 12;
                }

                int target1Count = 0;
                int target2Count = 0;
                foreach (int selectedOptionId in progress.SelectedOptionIds)
                {
                    if (selectedOptionId == targetOptionId1)
                    {
                        ++target1Count;
                    }

                    if (selectedOptionId == targetOptionId2)
                    {
                        ++target2Count;
                    }
                }

                if (target1Count != 1 || target2Count != 1)
                {
                    Log.Console($"granted option duplicate mismatch, option1Count={target1Count}, option2Count={target2Count}");
                    return 13;
                }

                if (progress.AppliedBuffIds.Count != 2)
                {
                    Log.Console($"applied buff count mismatch, count={progress.AppliedBuffIds.Count}");
                    return 14;
                }

                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null || runtimeComponent.ChildrenCount() != 2)
                {
                    Log.Console($"runtime component count mismatch, count={runtimeComponent?.ChildrenCount() ?? 0}");
                    return 15;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                targetOptionConfig1.Quality = oldQuality1;
                targetOptionConfig2.Quality = oldQuality2;
                targetOptionConfig1.ShowTags = oldShowTags1;
                targetOptionConfig1.HideTags = oldHideTags1;
                targetOptionConfig2.ShowTags = oldShowTags2;
                targetOptionConfig2.HideTags = oldHideTags2;
                sourceOptionConfig.BTConfig = oldBtConfig;
                sourceOptionConfig.EffectGroupId = oldEffectGroupId;
                configCategory.RemoveEffectGroup(effectGroupId);
            }
        }
    }
}
