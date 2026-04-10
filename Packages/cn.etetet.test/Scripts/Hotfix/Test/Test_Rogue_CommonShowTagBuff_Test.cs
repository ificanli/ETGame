using System;
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

            if (!TestHelper.TryFindKeepableRogueOption(configCategory, null, out int optionId, out RogueOptionConfig optionConfig, out _))
            {
                Log.Console("failed to find keepable rogue option for common show tag test");
                return 4;
            }

            if (!TestHelper.TryFindCommonShowTagWithBuff(configCategory, null, out int commonShowTagId, out int expectedBuffConfigId))
            {
                Log.Console("failed to find common show tag with buff");
                return 5;
            }

            int[] oldShowTags = optionConfig.ShowTags;
            int[] oldHideTags = optionConfig.HideTags;

            try
            {
                optionConfig.ShowTags = Array.Empty<int>();
                optionConfig.HideTags = new[] { commonShowTagId };

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
                    return 6;
                }

                BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
                if (buffComponent == null)
                {
                    Log.Console("buff component is null after first choose");
                    return 7;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"first choose option failed, error={chooseError}");
                    return 8;
                }

                if (!progress.CommonShowTagCounts.TryGetValue(commonShowTagId, out int firstCount) || firstCount != 1)
                {
                    Log.Console($"first choose common show tag count mismatch, tagId={commonShowTagId}, count={firstCount}");
                    return 9;
                }

                if (progress.AppliedShowTagBuffIds.Count != 0)
                {
                    Log.Console($"tag buff should not apply after first choose, count={progress.AppliedShowTagBuffIds.Count}");
                    return 10;
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
                    return 11;
                }

                buffComponent = unit.GetComponent<BuffComponent>();
                if (buffComponent == null)
                {
                    Log.Console("buff component is null after second choose");
                    return 12;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"second choose option failed, error={chooseError}");
                    return 13;
                }

                if (progress.SelectedOptionIds.Count != 2)
                {
                    Log.Console($"selected option count mismatch, count={progress.SelectedOptionIds.Count}");
                    return 14;
                }

                if (!progress.CommonShowTagCounts.TryGetValue(commonShowTagId, out int secondCount) || secondCount != 2)
                {
                    Log.Console($"second choose common show tag count mismatch, tagId={commonShowTagId}, count={secondCount}");
                    return 15;
                }

                if (!progress.AppliedShowTagBuffIds.TryGetValue(commonShowTagId, out long buffId) || buffId <= 0)
                {
                    Log.Console($"applied tag buff id missing, tagId={commonShowTagId}, buffId={buffId}");
                    return 16;
                }

                Buff appliedBuff = buffComponent.GetChild<Buff>(buffId);
                if (appliedBuff == null)
                {
                    Log.Console($"applied tag buff entity missing, tagId={commonShowTagId}, buffId={buffId}");
                    return 17;
                }

                if (appliedBuff.ConfigId != expectedBuffConfigId)
                {
                    Log.Console($"applied tag buff config mismatch, tagId={commonShowTagId}, configId={appliedBuff.ConfigId}, expected={expectedBuffConfigId}");
                    return 18;
                }

                if (!buffComponent.HasBuff(expectedBuffConfigId))
                {
                    Log.Console($"buff component missing expected tag buff, tagId={commonShowTagId}, buffConfigId={expectedBuffConfigId}");
                    return 19;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                optionConfig.ShowTags = oldShowTags;
                optionConfig.HideTags = oldHideTags;
            }
        }
    }
}
