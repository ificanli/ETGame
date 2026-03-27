using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_MatchRobot_AutoChoose_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_MatchRobot_AutoChoose_Test));
            Scene scene = scope.TestFiber.Root;
            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

            EntityRef<Scene> sceneRef = scene;
            RogueEffectGroupLoader.RegisterAll();

            Unit unit = TestHelper.CreateServerUnit(scene, UnitType.Player, addBuffComponent: true, addProgress: true, campId: 1);
            if (unit == null)
            {
                Log.Console("server unit is null");
                return 1;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 2;
            }

            MatchRobotComponent matchRobot = unit.AddComponent<MatchRobotComponent>();
            matchRobot.AutoChooseDelayMinMs = 10;
            matchRobot.AutoChooseDelayMaxMs = 10;
            EntityRef<Unit> unitRef = unit;
            EntityRef<RogueProgressComponent> progressRef = progress;
            EntityRef<MatchRobotComponent> matchRobotRef = matchRobot;

            bool opened = RogueProgressHelper.TryOpenInitialChoiceIfNeeded(unit);
            if (!opened || !progress.ChoicePending || progress.PendingOptionIds.Count == 0)
            {
                Log.Console($"match robot choice did not open, opened={opened}, pending={progress.ChoicePending}, optionCount={progress.PendingOptionIds.Count}");
                return 3;
            }

            long choiceSerial = progress.ChoiceSerial;
            for (int retry = 0; retry < 20 && progress.ChoicePending; ++retry)
            {
                scene = sceneRef;
                if (scene == null || scene.IsDisposed)
                {
                    Log.Console("scene disposed while waiting match robot auto choose");
                    return 4;
                }

                await scene.TimerComponent.WaitAsync(50);

                unit = unitRef;
                progress = progressRef;
                matchRobot = matchRobotRef;
                if (unit == null || unit.IsDisposed || progress == null || matchRobot == null)
                {
                    Log.Console("entity disposed while waiting match robot auto choose");
                    return 5;
                }
            }

            unit = unitRef;
            progress = progressRef;
            matchRobot = matchRobotRef;
            if (unit == null || unit.IsDisposed || progress == null || matchRobot == null)
            {
                Log.Console("entity disposed after match robot auto choose");
                return 6;
            }

            if (progress.ChoicePending)
            {
                Log.Console($"match robot choice still pending, serial={progress.ChoiceSerial}, scheduled={matchRobot.AutoChooseScheduledSerial}, completed={matchRobot.AutoChooseCompletedSerial}");
                return 7;
            }

            if (progress.SelectedOptionIds.Count == 0)
            {
                Log.Console("match robot did not record any selected option");
                return 8;
            }

            if (matchRobot.AutoChooseScheduledSerial != choiceSerial || matchRobot.AutoChooseCompletedSerial != choiceSerial)
            {
                Log.Console($"match robot auto choose serial mismatch, scheduled={matchRobot.AutoChooseScheduledSerial}, completed={matchRobot.AutoChooseCompletedSerial}, expected={choiceSerial}");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
