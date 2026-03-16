using System;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_GoldGainBonus_ByTag_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_GoldGainBonus_ByTag_Test));
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
            unit.AddComponent<BuffComponent>();

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                numericComponent.SetNoEvent(numericType, numericValue);
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 2;
            }

            int goldTagId = 0;
            foreach (RogueTagConfig tagConfig in configCategory.GetTags().Values)
            {
                if (tagConfig != null && tagConfig.TagType == 1 && string.Equals(tagConfig.ShowTagsName, "1"))
                {
                    goldTagId = tagConfig.Id;
                    break;
                }
            }

            if (goldTagId <= 0)
            {
                Log.Console("gold archetype tag id not found");
                return 3;
            }

            if (!configCategory.TryGetOption(1, out RogueOptionConfig tagOptionConfig) || tagOptionConfig == null)
            {
                Log.Console("rogue option config 1 is null");
                return 4;
            }

            if (!configCategory.TryGetOption(2, out RogueOptionConfig goldOptionConfig) || goldOptionConfig == null)
            {
                Log.Console("rogue option config 2 is null");
                return 5;
            }

            string oldTagBtConfig = tagOptionConfig.BTConfig;
            int oldTagEffectGroupId = tagOptionConfig.EffectGroupId;
            int[] oldTagShowTags = tagOptionConfig.ShowTags;
            int[] oldTagHideTags = tagOptionConfig.HideTags;
            string oldGoldBtConfig = goldOptionConfig.BTConfig;
            int oldGoldEffectGroupId = goldOptionConfig.EffectGroupId;
            int[] oldGoldShowTags = goldOptionConfig.ShowTags;
            int[] oldGoldHideTags = goldOptionConfig.HideTags;

            const int tagEffectGroupId = 7201;
            const int goldEffectGroupId = 7202;

            try
            {
                tagOptionConfig.BTConfig = $"group:{tagEffectGroupId}";
                tagOptionConfig.EffectGroupId = 0;
                tagOptionConfig.ShowTags = Array.Empty<int>();
                tagOptionConfig.HideTags = new[] { goldTagId };

                goldOptionConfig.BTConfig = $"group:{goldEffectGroupId}";
                goldOptionConfig.EffectGroupId = 0;
                goldOptionConfig.ShowTags = Array.Empty<int>();
                goldOptionConfig.HideTags = Array.Empty<int>();

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = tagEffectGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddGold,
                            Value1 = 0,
                        },
                    },
                });

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = goldEffectGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddGold,
                            Value1 = 100,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 6;
                }

                EntityRef<Unit> unitRef = unit;
                EntityRef<RogueProgressComponent> progressRef = progress;

                for (int i = 0; i < 2; ++i)
                {
                    progress.ChoicePending = true;
                    progress.ChoiceSerial += 1;
                    progress.PendingOptionIds.Clear();
                    progress.PendingOptionIds.Add(1);

                    int chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 1, null);
                    unit = unitRef;
                    progress = progressRef;
                    if (unit == null || progress == null)
                    {
                        Log.Console("unit or progress disposed after tag choose");
                        return 7 + i;
                    }

                    if (chooseError != ErrorCode.ERR_Success)
                    {
                        Log.Console($"tag choose failed, error={chooseError}, index={i}");
                        return 9 + i;
                    }
                }

                if (!progress.CommonShowTagCounts.TryGetValue(goldTagId, out int countAfterTwoTags) || countAfterTwoTags != 2)
                {
                    Log.Console($"gold tag count after two tags mismatch, count={countAfterTwoTags}");
                    return 11;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial += 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(2);

                int firstGoldError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 2, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after first gold choose");
                    return 12;
                }

                if (firstGoldError != ErrorCode.ERR_Success)
                {
                    Log.Console($"first gold choose failed, error={firstGoldError}");
                    return 13;
                }

                if (progress.CurrentGold != 115)
                {
                    Log.Console($"gold after two-tag bonus mismatch, gold={progress.CurrentGold}");
                    return 14;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial += 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(1);

                int thirdTagError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 1, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after third tag choose");
                    return 15;
                }

                if (thirdTagError != ErrorCode.ERR_Success)
                {
                    Log.Console($"third tag choose failed, error={thirdTagError}");
                    return 16;
                }

                if (!progress.CommonShowTagCounts.TryGetValue(goldTagId, out int countAfterThreeTags) || countAfterThreeTags != 3)
                {
                    Log.Console($"gold tag count after three tags mismatch, count={countAfterThreeTags}");
                    return 17;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial += 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(2);

                int secondGoldError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 2, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after second gold choose");
                    return 18;
                }

                if (secondGoldError != ErrorCode.ERR_Success)
                {
                    Log.Console($"second gold choose failed, error={secondGoldError}");
                    return 19;
                }

                if (progress.CurrentGold != 245)
                {
                    Log.Console($"gold after three-tag bonus mismatch, gold={progress.CurrentGold}");
                    return 20;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                tagOptionConfig.BTConfig = oldTagBtConfig;
                tagOptionConfig.EffectGroupId = oldTagEffectGroupId;
                tagOptionConfig.ShowTags = oldTagShowTags;
                tagOptionConfig.HideTags = oldTagHideTags;
                goldOptionConfig.BTConfig = oldGoldBtConfig;
                goldOptionConfig.EffectGroupId = oldGoldEffectGroupId;
                goldOptionConfig.ShowTags = oldGoldShowTags;
                goldOptionConfig.HideTags = oldGoldHideTags;
                configCategory.RemoveEffectGroup(tagEffectGroupId);
                configCategory.RemoveEffectGroup(goldEffectGroupId);
            }
        }
    }
}
