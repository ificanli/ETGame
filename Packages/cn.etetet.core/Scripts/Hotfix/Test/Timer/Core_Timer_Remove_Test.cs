namespace ET.Test
{
    /// <summary>
    /// TimerComponent Remove test
    /// </summary>
    public class Core_Timer_Remove_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_Timer_Remove_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            TimerComponent timerComponent = scene.TimerComponent;

            // Test 1: Remove existing timer returns true
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                long tillTime = TimeInfo.Instance.ServerNow() + 1000;
                long timerId = timerComponent.NewOnceTimer(tillTime, TimerInvokeType.TestOnceTimer, testEntity);

                bool removed = timerComponent.Remove(ref timerId);

                if (!removed)
                {
                    Log.Console("Remove existing: should return true");
                    return 1;
                }

                if (timerId != 0)
                {
                    Log.Console($"Remove existing: timerId should be 0, actual {timerId}");
                    return 2;
                }

                testEntity.Dispose();
                Log.Debug("Test 1 passed: Remove existing timer returns true and sets id to 0");
            }

            // Test 2: Remove with id=0 returns false
            {
                long zeroId = 0;
                bool removed = timerComponent.Remove(ref zeroId);

                if (removed)
                {
                    Log.Console("Remove id=0: should return false");
                    return 3;
                }

                if (zeroId != 0)
                {
                    Log.Console($"Remove id=0: id should stay 0, actual {zeroId}");
                    return 4;
                }

                Log.Debug("Test 2 passed: Remove with id=0 returns false");
            }

            // Test 3: Remove same timer twice
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                long tillTime = TimeInfo.Instance.ServerNow() + 1000;
                long timerId = timerComponent.NewOnceTimer(tillTime, TimerInvokeType.TestOnceTimer, testEntity);
                long originalId = timerId;

                // First remove
                bool removed1 = timerComponent.Remove(ref timerId);
                if (!removed1)
                {
                    Log.Console("Remove twice: first remove should return true");
                    return 5;
                }

                // Second remove with original id (timerId is now 0)
                bool removed2 = timerComponent.Remove(ref originalId);
                if (removed2)
                {
                    Log.Console("Remove twice: second remove should return false");
                    return 6;
                }

                testEntity.Dispose();
                Log.Debug("Test 3 passed: Remove same timer twice");
            }

            // Test 4: Remove after timer triggered (timer auto-removed)
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                EntityRef<TestTimerEntity> testEntityRef = testEntity;
                long tillTime = TimeInfo.Instance.ServerNow() + 50;
                long timerId = timerComponent.NewOnceTimer(tillTime, TimerInvokeType.TestOnceTimer, testEntity);

                // Wait for timer to trigger
                await timerComponent.WaitAsync(100);
                testEntity = testEntityRef;

                if (testEntity.TriggerCount != 1)
                {
                    Log.Console($"Remove after trigger: timer should have triggered, count {testEntity.TriggerCount}");
                    return 7;
                }

                // Try to remove already triggered timer
                bool removed = timerComponent.Remove(ref timerId);
                if (removed)
                {
                    Log.Console("Remove after trigger: should return false (timer already removed)");
                    return 8;
                }

                testEntity.Dispose();
                Log.Debug("Test 4 passed: Remove after timer triggered returns false");
            }

            Log.Debug("Core_Timer_Remove_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
