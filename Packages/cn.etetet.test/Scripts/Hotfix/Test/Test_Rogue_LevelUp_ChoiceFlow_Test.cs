using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_LevelUp_ChoiceFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_LevelUp_ChoiceFlow_Test));
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

            int currentLevel = progress.Level;
            int nextNeedExp = progress.NeedExp;
            int expToChoice = 0;
            int targetChoiceLevel = 0;

            for (int i = 0; i < 50; ++i)
            {
                int nextLevel = currentLevel + 1;
                if (!configCategory.TryGetLevel(nextLevel, out RogueLevelConfig levelConfig) || levelConfig == null)
                {
                    Log.Console($"no next level config found from level={currentLevel}");
                    return 4;
                }

                expToChoice += nextNeedExp;
                if (levelConfig.TriggerChoice)
                {
                    targetChoiceLevel = nextLevel;
                    break;
                }

                currentLevel = nextLevel;
                nextNeedExp = RogueProgressComponentSystem.NormalizeNeedExp(levelConfig.NeedExp);
            }

            if (targetChoiceLevel == 0 || expToChoice <= 0)
            {
                Log.Console($"failed to compute choice trigger targetLevel={targetChoiceLevel} expToChoice={expToChoice}");
                return 5;
            }

            RogueProgressHelper.AddExp(unit, expToChoice);

            if (progress.Level < targetChoiceLevel)
            {
                Log.Console($"level not reached target, current={progress.Level}, target={targetChoiceLevel}");
                return 6;
            }

            if (!progress.ChoicePending)
            {
                Log.Console("choice popup should be pending");
                return 7;
            }

            if (progress.PendingOptionIds == null || progress.PendingOptionIds.Count == 0)
            {
                Log.Console("pending option ids is empty");
                return 8;
            }

            long choiceSerial = progress.ChoiceSerial;
            int optionId = progress.PendingOptionIds[0];
            if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) || optionConfig == null)
            {
                Log.Console($"option config missing, optionId={optionId}");
                return 9;
            }

            int appliedBuffConfigId = 0;
            EntityRef<Unit> unitRef = unit;
            EntityRef<RogueProgressComponent> progressRef = progress;
            int chooseError = await RogueProgressHelper.ChooseOption(unit, choiceSerial, optionId, (buffConfigId, _) =>
            {
                appliedBuffConfigId = buffConfigId;
            });

            unit = unitRef;
            progress = progressRef;
            if (unit == null || progress == null)
            {
                Log.Console("unit or progress disposed after choose option");
                return 10;
            }

            if (chooseError != ErrorCode.ERR_Success)
            {
                Log.Console($"choose option failed, error={chooseError}");
                return 11;
            }

            if (!optionConfig.TryGetEffectBuffConfigId(out int expectedBuffConfigId) || appliedBuffConfigId <= 0 || appliedBuffConfigId != expectedBuffConfigId)
            {
                Log.Console($"applied buff mismatch, applied={appliedBuffConfigId}, expected={expectedBuffConfigId}");
                return 12;
            }

            if (progress.ChoicePending)
            {
                Log.Console("choice should not remain pending after choose");
                return 13;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                Log.Console("buff component is null");
                return 14;
            }

            if (!buffComponent.HasBuff(appliedBuffConfigId))
            {
                Log.Console($"expected unit has buff, buffConfigId={appliedBuffConfigId}");
                return 15;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
