namespace ET.Test
{
    /// <summary>
    /// TimerComponent NewOnceTimer test
    /// </summary>
    public class Core_Timer_OnceTimer_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_Timer_OnceTimer_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            TimerComponent timerComponent = scene.TimerComponent;

            // Test 1: OnceTimer should trigger once
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                EntityRef<TestTimerEntity> testEntityRef = testEntity;
                long tillTime = TimeInfo.Instance.ServerNow() + 100;
                long timerId = timerComponent.NewOnceTimer(tillTime, TimerInvokeType.TestOnceTimer, testEntity);

                if (timerId == 0)
                {
                    Log.Console("OnceTimer: timerId should not be 0");
                    return 1;
                }

                await timerComponent.WaitAsync(150);
                testEntity = testEntityRef;

                if (testEntity.TriggerCount != 1)
                {
                    Log.Console($"OnceTimer: TriggerCount should be 1, actual {testEntity.TriggerCount}");
                    return 2;
                }

                // Wait more to ensure it doesn't trigger again
                await timerComponent.WaitAsync(150);
                testEntity = testEntityRef;

                if (testEntity.TriggerCount != 1)
                {
                    Log.Console($"OnceTimer: TriggerCount should still be 1, actual {testEntity.TriggerCount}");
                    return 3;
                }

                testEntity.Dispose();
                Log.Debug("Test 1 passed: OnceTimer triggers once");
            }

            // Test 2: Remove OnceTimer before trigger
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                EntityRef<TestTimerEntity> testEntityRef = testEntity;
                long tillTime = TimeInfo.Instance.ServerNow() + 200;
                long timerId = timerComponent.NewOnceTimer(tillTime, TimerInvokeType.TestOnceTimer, testEntity);

                // Remove before trigger
                bool removed = timerComponent.Remove(ref timerId);

                if (!removed)
                {
                    Log.Console("OnceTimer Remove: should return true");
                    return 4;
                }

                if (timerId != 0)
                {
                    Log.Console("OnceTimer Remove: timerId should be set to 0");
                    return 5;
                }

                await timerComponent.WaitAsync(250);
                testEntity = testEntityRef;

                if (testEntity.TriggerCount != 0)
                {
                    Log.Console($"OnceTimer Remove: TriggerCount should be 0, actual {testEntity.TriggerCount}");
                    return 6;
                }

                testEntity.Dispose();
                Log.Debug("Test 2 passed: Remove OnceTimer before trigger");
            }

            // Test 3: OnceTimer with immediate trigger (tillTime in past)
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                EntityRef<TestTimerEntity> testEntityRef = testEntity;
                long tillTime = TimeInfo.Instance.ServerNow() - 100; // past time
                timerComponent.NewOnceTimer(tillTime, TimerInvokeType.TestOnceTimer, testEntity);

                // Need to wait for next update cycle
                await timerComponent.WaitAsync(50);
                testEntity = testEntityRef;

                if (testEntity.TriggerCount != 1)
                {
                    Log.Console($"OnceTimer past: TriggerCount should be 1, actual {testEntity.TriggerCount}");
                    return 7;
                }

                testEntity.Dispose();
                Log.Debug("Test 3 passed: OnceTimer with past tillTime triggers immediately");
            }

            Log.Debug("Core_Timer_OnceTimer_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
