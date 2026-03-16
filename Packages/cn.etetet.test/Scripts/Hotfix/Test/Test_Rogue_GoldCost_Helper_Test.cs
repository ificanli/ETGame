using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_GoldCost_Helper_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_GoldCost_Helper_Test));
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
            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 2;
            }

            int currentGold = RogueGoldHelper.AddGold(progress, 120);
            if (currentGold != 120 || progress.CurrentGold != 120)
            {
                Log.Console($"add gold failed, gold={progress.CurrentGold}");
                return 3;
            }

            if (!RogueGoldHelper.CanAfford(progress, 40))
            {
                Log.Console($"can afford failed, gold={progress.CurrentGold}");
                return 4;
            }

            if (!RogueGoldHelper.TryCostGold(progress, 40))
            {
                Log.Console($"cost gold failed, gold={progress.CurrentGold}");
                return 5;
            }

            if (progress.CurrentGold != 80)
            {
                Log.Console($"gold after cost mismatch, gold={progress.CurrentGold}");
                return 6;
            }

            if (RogueGoldHelper.TryCostGold(progress, 81))
            {
                Log.Console($"cost gold should fail when insufficient, gold={progress.CurrentGold}");
                return 7;
            }

            if (progress.CurrentGold != 80)
            {
                Log.Console($"gold should remain unchanged after failed cost, gold={progress.CurrentGold}");
                return 8;
            }

            RogueGoldHelper.AddGold(progress, -999);
            if (progress.CurrentGold != 0)
            {
                Log.Console($"gold should clamp to zero, gold={progress.CurrentGold}");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
