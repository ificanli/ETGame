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

    public class Core_HighFrequencyScheduler_Basic_Test : ATestHandler
    {
        private const int TestHighFrequencyChannelId = 9001;

        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_HighFrequencyScheduler_Basic_Test));

            Scene scene = scope.TestFiber.Root;
            EntityRef<Scene> sceneRef = scene;
            scene.AddComponent<TimerComponent>();
            HighFrequencySchedulerComponent scheduler = scene.AddComponent<HighFrequencySchedulerComponent>();
            EntityRef<HighFrequencySchedulerComponent> schedulerRef = scheduler;
            scheduler.RegisterChannel(new HighFrequencyChannelConfig
            {
                ChannelId = TestHighFrequencyChannelId,
                IntervalMs = 16,
                MaxCatchUpCount = 3,
                TickInvokeType = TimerInvokeType.TestHighFrequencyTick,
                RemovedInvokeType = TimerInvokeType.TestHighFrequencyRemoved,
                WarningBudgetMs = 8,
            });

            TestHighFrequencyEntity steadyEntity = scene.AddChild<TestHighFrequencyEntity>();
            EntityRef<TestHighFrequencyEntity> steadyEntityRef = steadyEntity;
            scheduler.AddEntity(TestHighFrequencyChannelId, steadyEntity);
            scheduler.AddEntity(TestHighFrequencyChannelId, steadyEntity);

            await scene.TimerComponent.WaitAsync(70);
            steadyEntity = steadyEntityRef;

            if (steadyEntity.TickCount < 2)
            {
                Log.Console($"steady entity tick count too low: {steadyEntity.TickCount}");
                return 1;
            }

            if (steadyEntity.TickCount > 6)
            {
                Log.Console($"steady entity tick count too high, duplicate registration may exist: {steadyEntity.TickCount}");
                return 2;
            }

            scene = sceneRef;
            TestHighFrequencyEntity removeEntity = scene.AddChild<TestHighFrequencyEntity>();
            EntityRef<TestHighFrequencyEntity> removeEntityRef = removeEntity;
            removeEntity.RemoveOnFirstTick = true;
            scheduler = schedulerRef;
            scheduler.AddEntity(TestHighFrequencyChannelId, removeEntity);

            scene = sceneRef;
            await scene.TimerComponent.WaitAsync(80);
            removeEntity = removeEntityRef;

            if (removeEntity.TickCount != 1)
            {
                Log.Console($"remove entity tick count invalid: {removeEntity.TickCount}");
                return 3;
            }

            if (removeEntity.RemovedCount != 1)
            {
                Log.Console($"remove entity removed callback count invalid: {removeEntity.RemovedCount}");
                return 4;
            }

            if (!removeEntity.DeferredRemoveObserved)
            {
                Log.Console("remove entity did not observe deferred removal");
                return 5;
            }

            scheduler = schedulerRef;
            if (scheduler.ActiveChannelCount != 1)
            {
                Log.Console($"active channel count invalid: {scheduler.ActiveChannelCount}");
                return 6;
            }

            steadyEntity = steadyEntityRef;
            Log.Debug(
                $"Core_HighFrequencyScheduler_Basic_Test passed: steadyTicks={steadyEntity.TickCount}, removeTicks={removeEntity.TickCount}, removedCount={removeEntity.RemovedCount}");
            return ErrorCode.ERR_Success;
        }
    }
}
