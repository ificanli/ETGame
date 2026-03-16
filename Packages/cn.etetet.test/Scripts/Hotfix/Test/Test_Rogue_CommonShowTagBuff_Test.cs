using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_CommonShowTagBuff_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_CommonShowTagBuff_Test));
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

            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 3;
            }

            int optionId = 0;
            List<int> commonShowTagIds = new();
            Dictionary<int, int> expectedTagBuffByTagId = new();
            bool foundOptionWithCommonTags = false;
            HashSet<int> missingTagBuffConfigIds = new();
            foreach (KeyValuePair<int, RogueOptionConfig> kv in configCategory.GetOptions())
            {
                RogueOptionConfig optionConfig = kv.Value;
                if (optionConfig == null || optionConfig.ShowTags == null || optionConfig.ShowTags.Length == 0)
                {
                    continue;
                }

                if (!RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, optionConfig, out int effectBuffConfigId) ||
                    !BuffConfigCategory.Instance.Contain(effectBuffConfigId))
                {
                    continue;
                }

                commonShowTagIds.Clear();
                expectedTagBuffByTagId.Clear();

                foreach (int tagId in optionConfig.ShowTags)
                {
                    if (!configCategory.TryGetTag(tagId, out RogueTagConfig tagConfig) || tagConfig == null)
                    {
                        continue;
                    }

                    if (tagConfig.TagType != 1 || tagConfig.ShowTagsBuffId == null || tagConfig.ShowTagsBuffId.Length == 0)
                    {
                        continue;
                    }

                    foundOptionWithCommonTags = true;
                    int buffConfigId = tagConfig.ShowTagsBuffId[0];
                    if (buffConfigId <= 0)
                    {
                        continue;
                    }

                    if (!BuffConfigCategory.Instance.Contain(buffConfigId))
                    {
                        missingTagBuffConfigIds.Add(buffConfigId);
                        continue;
                    }

                    commonShowTagIds.Add(tagId);
                    expectedTagBuffByTagId[tagId] = buffConfigId;
                }

                if (commonShowTagIds.Count > 0)
                {
                    optionId = kv.Key;
                    break;
                }
            }

            if (optionId <= 0)
            {
                if (foundOptionWithCommonTags && missingTagBuffConfigIds.Count > 0)
                {
                    Log.Console($"failed to find rogue option with valid common show tags, missing tag buff configs: {string.Join(',', missingTagBuffConfigIds)}");
                }
                else
                {
                    Log.Console("failed to find rogue option with valid common show tags");
                }
                return 4;
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
                Log.Console("unit or progress disposed after first choose");
                return 5;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                Log.Console("buff component is null after first choose");
                return 6;
            }

            if (chooseError != ErrorCode.ERR_Success)
            {
                Log.Console($"first choose option failed, error={chooseError}");
                return 7;
            }

            foreach (int tagId in commonShowTagIds)
            {
                if (!progress.CommonShowTagCounts.TryGetValue(tagId, out int count) || count != 1)
                {
                    Log.Console($"first choose common show tag count mismatch, tagId={tagId}, count={count}");
                    return 8;
                }
            }

            if (progress.AppliedShowTagBuffIds.Count != 0)
            {
                Log.Console($"tag buff should not apply after first choose, count={progress.AppliedShowTagBuffIds.Count}");
                return 9;
            }

            progress.ChoicePending = true;
            progress.ChoiceSerial = 2;
            progress.PendingOptionIds.Clear();
            progress.PendingOptionIds.Add(optionId);

            unitRef = unit;
            progressRef = progress;
            chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, optionId, null);

            unit = unitRef;
            progress = progressRef;
            if (unit == null || progress == null)
            {
                Log.Console("unit or progress disposed after second choose");
                return 10;
            }

            buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                Log.Console("buff component is null after second choose");
                return 11;
            }

            if (chooseError != ErrorCode.ERR_Success)
            {
                Log.Console($"second choose option failed, error={chooseError}");
                return 12;
            }

            if (progress.SelectedOptionIds.Count != 2)
            {
                Log.Console($"selected option count mismatch, count={progress.SelectedOptionIds.Count}");
                return 13;
            }

            foreach (int tagId in commonShowTagIds)
            {
                if (!progress.CommonShowTagCounts.TryGetValue(tagId, out int count) || count != 2)
                {
                    Log.Console($"second choose common show tag count mismatch, tagId={tagId}, count={count}");
                    return 14;
                }

                if (!expectedTagBuffByTagId.TryGetValue(tagId, out int expectedBuffConfigId))
                {
                    Log.Console($"expected tag buff config missing, tagId={tagId}");
                    return 15;
                }

                if (!progress.AppliedShowTagBuffIds.TryGetValue(tagId, out long buffId) || buffId <= 0)
                {
                    Log.Console($"applied tag buff id missing, tagId={tagId}, buffId={buffId}");
                    return 16;
                }

                Buff appliedBuff = buffComponent.GetChild<Buff>(buffId);
                if (appliedBuff == null)
                {
                    Log.Console($"applied tag buff entity missing, tagId={tagId}, buffId={buffId}");
                    return 17;
                }

                if (appliedBuff.ConfigId != expectedBuffConfigId)
                {
                    Log.Console($"applied tag buff config mismatch, tagId={tagId}, configId={appliedBuff.ConfigId}, expected={expectedBuffConfigId}");
                    return 18;
                }

                if (!buffComponent.HasBuff(expectedBuffConfigId))
                {
                    Log.Console($"buff component missing expected tag buff, tagId={tagId}, buffConfigId={expectedBuffConfigId}");
                    return 19;
                }
            }

            return ErrorCode.ERR_Success;
        }
    }
}
