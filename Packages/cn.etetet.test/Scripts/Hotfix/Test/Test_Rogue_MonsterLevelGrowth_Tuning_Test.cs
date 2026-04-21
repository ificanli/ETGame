using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_MonsterLevelGrowth_Tuning_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_MonsterLevelGrowth_Tuning_Test));
            Scene scene = scope.TestFiber.Root;
            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            if (RogueRuntimeConfigCategory.Instance == null)
            {
                Log.Console("rogue runtime config category is null");
                return 1;
            }

            Unit player1 = TestHelper.CreateServerUnit(scene, UnitType.Player, addBuffComponent: true, addProgress: true, campId: 1);
            Unit player2 = TestHelper.CreateServerUnit(scene, UnitType.Player, addBuffComponent: true, addProgress: true, campId: 1);
            Unit monster = TestHelper.CreateServerUnit(scene, UnitType.Monster, addBuffComponent: true, campId: 2);
            Unit deadMonster = TestHelper.CreateServerUnit(scene, UnitType.Monster, addBuffComponent: true, campId: 2);
            if (player1 == null || player2 == null || monster == null || deadMonster == null)
            {
                Log.Console("failed to create test units");
                return 2;
            }

            RogueProgressComponent progress1 = player1.GetComponent<RogueProgressComponent>();
            RogueProgressComponent progress2 = player2.GetComponent<RogueProgressComponent>();
            NumericComponent monsterNumeric = monster.NumericComponent;
            NumericComponent deadMonsterNumeric = deadMonster.NumericComponent;
            if (progress1 == null || progress2 == null || monsterNumeric == null || deadMonsterNumeric == null)
            {
                Log.Console("test component missing");
                return 3;
            }

            deadMonsterNumeric.SetNoEvent(NumericType.HP, 0);

            progress1.Level = 5;
            progress2.Level = 5;
            int expectedLevelAtFive = RogueUnitDisplayLevelHelper.ConvertAlivePlayerAverageLevelToMonsterDisplayLevel(5);
            if (expectedLevelAtFive != 5)
            {
                Log.Console($"unexpected level mapping at avg=5, actual={expectedLevelAtFive}");
                return 4;
            }

            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevel(monster, false);

            int initialMonsterLevel = monster.GetComponent<UnitDisplayLevelComponent>()?.Level ?? 0;
            if (initialMonsterLevel != expectedLevelAtFive)
            {
                Log.Console($"monster level mismatch after initial refresh, expected={expectedLevelAtFive}, actual={initialMonsterLevel}");
                return 5;
            }

            long initialMaxHp = monsterNumeric.GetAsLong(NumericType.MaxHP);
            long initialHp = monsterNumeric.GetAsLong(NumericType.HP);
            if (initialMaxHp <= 0 || initialHp != initialMaxHp)
            {
                Log.Console($"monster should start full after first growth apply, hp={initialHp}, maxHp={initialMaxHp}");
                return 6;
            }

            monsterNumeric.SetNoEvent(NumericType.HP, 1);
            progress1.Level = 8;
            progress2.Level = 8;
            int expectedLevelAtEight = RogueUnitDisplayLevelHelper.ConvertAlivePlayerAverageLevelToMonsterDisplayLevel(8);
            if (expectedLevelAtEight != 8)
            {
                Log.Console($"unexpected level mapping at avg=8, actual={expectedLevelAtEight}");
                return 7;
            }

            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevels(scene, false);

            int upgradedMonsterLevel = monster.GetComponent<UnitDisplayLevelComponent>()?.Level ?? 0;
            long upgradedMaxHp = monsterNumeric.GetAsLong(NumericType.MaxHP);
            long upgradedHp = monsterNumeric.GetAsLong(NumericType.HP);
            if (upgradedMonsterLevel != expectedLevelAtEight)
            {
                Log.Console($"monster level mismatch after upgrade, expected={expectedLevelAtEight}, actual={upgradedMonsterLevel}");
                return 8;
            }

            if (upgradedMaxHp <= initialMaxHp)
            {
                Log.Console($"monster max hp should increase after level up, old={initialMaxHp}, new={upgradedMaxHp}");
                return 9;
            }

            if (upgradedHp != upgradedMaxHp)
            {
                Log.Console($"monster should heal to full after level up, hp={upgradedHp}, maxHp={upgradedMaxHp}");
                return 10;
            }

            progress1.Level = 2;
            progress2.Level = 2;
            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevels(scene, false);

            int downgradedMonsterLevel = monster.GetComponent<UnitDisplayLevelComponent>()?.Level ?? 0;
            long downgradedMaxHp = monsterNumeric.GetAsLong(NumericType.MaxHP);
            long downgradedHp = monsterNumeric.GetAsLong(NumericType.HP);
            if (downgradedMonsterLevel != 2)
            {
                Log.Console($"monster level mismatch after downgrade, expected=2, actual={downgradedMonsterLevel}");
                return 11;
            }

            if (downgradedHp > downgradedMaxHp)
            {
                Log.Console($"monster hp should clamp to max after downgrade, hp={downgradedHp}, maxHp={downgradedMaxHp}");
                return 12;
            }

            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevel(deadMonster, false);
            if (deadMonster.GetComponent<UnitDisplayLevelComponent>() != null)
            {
                Log.Console("dead monster should not receive display level refresh");
                return 13;
            }

            if (deadMonsterNumeric.GetAsLong(NumericType.HP) != 0)
            {
                Log.Console($"dead monster hp should remain zero, hp={deadMonsterNumeric.GetAsLong(NumericType.HP)}");
                return 14;
            }

            if (RobotAutoLevelHelper.GetScaledElapsedSec(90) != 72)
            {
                Log.Console($"robot auto level scaled elapsed mismatch at 90s, actual={RobotAutoLevelHelper.GetScaledElapsedSec(90)}");
                return 15;
            }

            if (RobotAutoLevelHelper.ResolveTargetLevel(90) != 3)
            {
                Log.Console($"robot auto level target mismatch at 90s, actual={RobotAutoLevelHelper.ResolveTargetLevel(90)}");
                return 16;
            }

            if (RobotAutoLevelHelper.ResolveTargetLevel(120) != 5)
            {
                Log.Console($"robot auto level target mismatch at 120s, actual={RobotAutoLevelHelper.ResolveTargetLevel(120)}");
                return 17;
            }

            Log.Console("Test_Rogue_MonsterLevelGrowth_Tuning_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
