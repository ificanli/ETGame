using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_RemoveSelectedOption_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_RemoveSelectedOption_Test));
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

            int[] oldShowTags = optionConfig.ShowTags;
            int[] oldHideTags = optionConfig.HideTags;

            try
            {
                optionConfig.ShowTags = System.Array.Empty<int>();
                optionConfig.HideTags = new[] { 51 };

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
                    Log.Console("unit or progress disposed after first choose");
                    return 5;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"first choose failed, error={chooseError}");
                    return 6;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 2;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(1);

                unitRef = unit;
                progressRef = progress;
                chooseError = await RogueProgressHelper.ChooseOption(unit, progress.ChoiceSerial, 1, null);
                unit = unitRef;
                progress = progressRef;
                if (unit == null || progress == null)
                {
                    Log.Console("unit or progress disposed after second choose");
                    return 7;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"second choose failed, error={chooseError}");
                    return 8;
                }

                RogueObjectiveComponent objectiveComponent = unit.AddComponent<RogueObjectiveComponent>();
                objectiveComponent.AddObjective(1, 7001, 9001);

                int removedRuntimeCount = RogueEffectHelper.RemoveSelectedOption(unit, progress, 1);
                if (removedRuntimeCount != 1)
                {
                    Log.Console($"removed runtime count mismatch, count={removedRuntimeCount}");
                    return 9;
                }

                if (progress.SelectedOptionIds.Count != 1)
                {
                    Log.Console($"selected option count mismatch after remove, count={progress.SelectedOptionIds.Count}");
                    return 10;
                }

                if (!progress.CommonShowTagCounts.TryGetValue(51, out int countAfterRemove) || countAfterRemove != 1)
                {
                    Log.Console($"hidden tag count mismatch after remove, count={countAfterRemove}");
                    return 11;
                }

                RogueEffectRuntimeComponent runtimeComponent = unit.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null || runtimeComponent.ChildrenCount() != 1)
                {
                    Log.Console($"runtime component count mismatch after remove, count={runtimeComponent?.ChildrenCount() ?? 0}");
                    return 12;
                }

                if (unit.GetComponent<RogueObjectiveComponent>() != null)
                {
                    Log.Console("objective component should be removed when last objective is removed");
                    return 13;
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
