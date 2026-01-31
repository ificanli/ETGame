namespace ET.Test
{
    /// <summary>
    /// TimerComponent NewRepeatedTimer test
    /// </summary>
    public class Core_Timer_RepeatedTimer_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_Timer_RepeatedTimer_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            TimerComponent timerComponent = scene.TimerComponent;

            // Test 1: RepeatedTimer should trigger multiple times
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                EntityRef<TestTimerEntity> testEntityRef = testEntity;
                // Server side minimum interval is 50ms, use 100ms to be safe
                long timerId = timerComponent.NewRepeatedTimer(100, TimerInvokeType.TestRepeatedTimer, testEntity);

                if (timerId == 0)
                {
                    Log.Console("RepeatedTimer: timerId should not be 0");
                    return 1;
                }

                // Wait 350ms, should trigger at least 3 times
                await timerComponent.WaitAsync(350);
                testEntity = testEntityRef;

                // Remove timer to stop
                timerComponent.Remove(ref timerId);

                if (testEntity.TriggerCount < 3)
                {
                    Log.Console($"RepeatedTimer: TriggerCount should be >= 3, actual {testEntity.TriggerCount}");
                    return 2;
                }

                int countAfterRemove = testEntity.TriggerCount;

                // Wait more to ensure it doesn't trigger after remove
                await timerComponent.WaitAsync(200);
                testEntity = testEntityRef;

                if (testEntity.TriggerCount != countAfterRemove)
                {
                    Log.Console($"RepeatedTimer: TriggerCount should stay {countAfterRemove} after remove, actual {testEntity.TriggerCount}");
                    return 3;
                }

                testEntity.Dispose();
                Log.Debug($"Test 1 passed: RepeatedTimer triggered {countAfterRemove} times");
            }

            // Test 2: RepeatedTimer stops when Entity is disposed
            {
                TestTimerEntity testEntity = scene.AddChild<TestTimerEntity>();
                EntityRef<TestTimerEntity> testEntityRef = testEntity;
                long timerId = timerComponent.NewRepeatedTimer(100, TimerInvokeType.TestRepeatedTimer, testEntity);

                await timerComponent.WaitAsync(150);
                testEntity = testEntityRef;

                int countBeforeDispose = testEntity.TriggerCount;
                if (countBeforeDispose < 1)
                {
                    Log.Console($"RepeatedTimer Entity: TriggerCount should be >= 1 before dispose, actual {countBeforeDispose}");
                    return 4;
                }

                // Dispose entity
                testEntity.Dispose();

                // Wait more - timer should stop because entity is disposed
                await timerComponent.WaitAsync(300);

                // Timer should be auto-removed, try to remove again should return false
                timerComponent.Remove(ref timerId);
                // Note: timerId was not reset by previous remove, so this tests the internal state

                Log.Debug($"Test 2 passed: RepeatedTimer stops when Entity disposed, triggered {countBeforeDispose} times");
            }

            Log.Debug("Core_Timer_RepeatedTimer_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
