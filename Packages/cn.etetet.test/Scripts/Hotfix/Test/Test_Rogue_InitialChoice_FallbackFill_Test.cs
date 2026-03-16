using System;
using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证开局选牌在首选品质候选不足时，仍会通过回退逻辑补齐到配置数量。
    /// </summary>
    public class Test_Rogue_InitialChoice_FallbackFill_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_InitialChoice_FallbackFill_Test));
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

            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            unit.UnitType = UnitType.Player;

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                numericComponent.SetNoEvent(numericType, numericValue);
            }

            unit.AddComponent<BuffComponent>();

            RogueEffectGroupLoader.RegisterAll();
            RogueBuffConfigLoader.EnsureRegistered();

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 2;
            }

            List<int> executableOptionIds = new List<int>();
            List<int> allOptionIds = new List<int>(configCategory.GetOptions().Keys);
            allOptionIds.Sort();
            foreach (int optionId in allOptionIds)
            {
                if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) ||
                    optionConfig == null ||
                    !RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, optionConfig, out _))
                {
                    continue;
                }

                executableOptionIds.Add(optionId);
                if (executableOptionIds.Count >= 3)
                {
                    break;
                }
            }

            if (executableOptionIds.Count < 3)
            {
                Log.Console($"not enough executable options, count={executableOptionIds.Count}");
                return 3;
            }

            int preferredOptionId = executableOptionIds[0];
            int fallbackOptionId1 = executableOptionIds[1];
            int fallbackOptionId2 = executableOptionIds[2];

            RogueOptionConfig preferredOption = configCategory.GetOptions()[preferredOptionId];
            RogueOptionConfig fallbackOption1 = configCategory.GetOptions()[fallbackOptionId1];
            RogueOptionConfig fallbackOption2 = configCategory.GetOptions()[fallbackOptionId2];

            int oldPreferredQuality = preferredOption.Quality;
            int[] oldPreferredShowTags = preferredOption.ShowTags;
            int[] oldPreferredHideTags = preferredOption.HideTags;
            int oldFallbackQuality1 = fallbackOption1.Quality;
            int[] oldFallbackShowTags1 = fallbackOption1.ShowTags;
            int[] oldFallbackHideTags1 = fallbackOption1.HideTags;
            int oldFallbackQuality2 = fallbackOption2.Quality;
            int[] oldFallbackShowTags2 = fallbackOption2.ShowTags;
            int[] oldFallbackHideTags2 = fallbackOption2.HideTags;

            const int preferredQuality = 88;
            try
            {
                preferredOption.Quality = preferredQuality;
                preferredOption.ShowTags = Array.Empty<int>();
                preferredOption.HideTags = Array.Empty<int>();

                fallbackOption1.Quality = 3;
                fallbackOption1.ShowTags = Array.Empty<int>();
                fallbackOption1.HideTags = Array.Empty<int>();

                fallbackOption2.Quality = 2;
                fallbackOption2.ShowTags = Array.Empty<int>();
                fallbackOption2.HideTags = Array.Empty<int>();

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 4;
                }

                RogueChoiceQualityComponent qualityComponent = RogueChoiceQualityHelper.GetOrCreate(unit);
                if (qualityComponent == null)
                {
                    Log.Console("choice quality component is null");
                    return 5;
                }

                qualityComponent.ChoiceQualities.Clear();
                qualityComponent.ChoiceQualities.Add(preferredQuality);
                qualityComponent.ChoiceQualities.Add(1);
                qualityComponent.ChoiceQualities.Add(1);
                qualityComponent.ChoiceQualities.Add(1);

                bool opened = RogueProgressHelper.TryOpenInitialChoiceIfNeeded(unit);
                if (!opened)
                {
                    Log.Console("initial choice should open with fallback fill");
                    return 6;
                }

                int expectedCount = configCategory.GetChoiceOptionCount();
                if (!progress.ChoicePending || progress.PendingOptionIds.Count != expectedCount)
                {
                    Log.Console($"pending option count mismatch, count={progress.PendingOptionIds.Count}, expected={expectedCount}");
                    return 7;
                }

                if (!progress.PendingOptionIds.Contains(preferredOptionId))
                {
                    Log.Console($"preferred option missing, optionId={preferredOptionId}");
                    return 8;
                }

                HashSet<int> uniqueOptionIds = new HashSet<int>();
                bool hasFallbackOption = false;
                foreach (int pendingOptionId in progress.PendingOptionIds)
                {
                    if (!uniqueOptionIds.Add(pendingOptionId))
                    {
                        Log.Console($"duplicate pending option, optionId={pendingOptionId}");
                        return 9;
                    }

                    if (!configCategory.TryGetOption(pendingOptionId, out RogueOptionConfig pendingOption) || pendingOption == null)
                    {
                        Log.Console($"pending option config missing, optionId={pendingOptionId}");
                        return 10;
                    }

                    if (pendingOption.Quality != preferredQuality)
                    {
                        hasFallbackOption = true;
                    }
                }

                if (!hasFallbackOption)
                {
                    Log.Console("fallback options were not appended");
                    return 11;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                preferredOption.Quality = oldPreferredQuality;
                preferredOption.ShowTags = oldPreferredShowTags;
                preferredOption.HideTags = oldPreferredHideTags;
                fallbackOption1.Quality = oldFallbackQuality1;
                fallbackOption1.ShowTags = oldFallbackShowTags1;
                fallbackOption1.HideTags = oldFallbackHideTags1;
                fallbackOption2.Quality = oldFallbackQuality2;
                fallbackOption2.ShowTags = oldFallbackShowTags2;
                fallbackOption2.HideTags = oldFallbackHideTags2;
            }
        }
    }
}
