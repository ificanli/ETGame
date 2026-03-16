using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证肉鸽开局（1级）可触发一次初始选牌。
    /// </summary>
    public class Test_Rogue_InitialChoice_Level1_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_InitialChoice_Level1_Test));
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
            unit.AddComponent<NumericComponent>();
            unit.AddComponent<BuffComponent>();

            RogueEffectGroupLoader.RegisterAll();
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

            if (progress.Level != 1)
            {
                Log.Console($"unexpected start level: {progress.Level}");
                return 4;
            }

            bool opened = RogueProgressHelper.TryOpenInitialChoiceIfNeeded(unit);
            if (!opened)
            {
                Log.Console("initial choice should open at level 1");
                return 5;
            }

            int expectedCount = configCategory.GetChoiceOptionCount();
            if (!progress.ChoicePending || progress.PendingOptionIds.Count != expectedCount)
            {
                Log.Console($"initial choice pending/options invalid, count={progress.PendingOptionIds.Count}, expected={expectedCount}");
                return 6;
            }

            bool openedAgain = RogueProgressHelper.TryOpenInitialChoiceIfNeeded(unit);
            if (openedAgain)
            {
                Log.Console("initial choice should not open repeatedly while pending");
                return 7;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
