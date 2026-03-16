using System;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_HiddenTagCount_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_HiddenTagCount_Test));
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

            if (!configCategory.TryGetTag(51, out RogueTagConfig hiddenTagConfig) || hiddenTagConfig == null || hiddenTagConfig.ShowTagsBuffId.Length == 0)
            {
                Log.Console("hidden tag config 51 is invalid");
                return 3;
            }

            int expectedBuffConfigId = hiddenTagConfig.ShowTagsBuffId[0];
            if (expectedBuffConfigId <= 0)
            {
                Log.Console($"hidden tag expected buff config invalid, buffConfigId={expectedBuffConfigId}");
                return 4;
            }

            int optionId = 0;
            RogueOptionConfig optionConfig = null;
            foreach (var kv in configCategory.GetOptions())
            {
                if (kv.Value == null ||
                    !RogueOptionConfigHelper.TryGetPreviewBuffConfigId(configCategory, kv.Value, out int effectBuffConfigId) ||
                    !BuffConfigCategory.Instance.Contain(effectBuffConfigId))
                {
                    continue;
                }

                optionId = kv.Key;
                optionConfig = kv.Value;
                break;
            }

            if (optionId <= 0 || optionConfig == null)
            {
                Log.Console("failed to find rogue option for hidden tag test");
                return 5;
            }

            int[] oldShowTags = optionConfig.ShowTags;
            int[] oldHideTags = optionConfig.HideTags;

            try
            {
                optionConfig.ShowTags = Array.Empty<int>();
                optionConfig.HideTags = new[] { 51 };

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 6;
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
                    return 7;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"first choose failed, error={chooseError}");
                    return 8;
                }

                if (!progress.CommonShowTagCounts.TryGetValue(51, out int firstCount) || firstCount != 1)
                {
                    Log.Console($"hidden tag count mismatch after first choose, count={firstCount}");
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

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"second choose failed, error={chooseError}");
                    return 11;
                }

                if (!progress.CommonShowTagCounts.TryGetValue(51, out int secondCount) || secondCount != 2)
                {
                    Log.Console($"hidden tag count mismatch after second choose, count={secondCount}");
                    return 12;
                }

                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null)
                {
                    Log.Console("rogue effect runtime component is null");
                    return 13;
                }

                if (runtimeComponent.Children == null || runtimeComponent.Children.Count != 2)
                {
                    Log.Console($"rogue effect runtime count mismatch, count={runtimeComponent.Children?.Count ?? 0}");
                    return 14;
                }

                if (!BuffConfigCategory.Instance.Contain(expectedBuffConfigId))
                {
                    return ErrorCode.ERR_Success;
                }

                if (!progress.AppliedShowTagBuffIds.TryGetValue(51, out long buffId) || buffId <= 0)
                {
                    Log.Console($"hidden tag buff id missing, buffId={buffId}");
                    return 15;
                }

                BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
                if (buffComponent == null)
                {
                    Log.Console("buff component is null");
                    return 16;
                }

                Buff buff = buffComponent.GetChild<Buff>(buffId);
                if (buff == null)
                {
                    Log.Console($"hidden tag buff entity missing, buffId={buffId}");
                    return 17;
                }

                if (buff.ConfigId != expectedBuffConfigId)
                {
                    Log.Console($"hidden tag buff config mismatch, configId={buff.ConfigId}");
                    return 18;
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
