namespace ET.Test
{
    /// <summary>
    /// CoroutineLockComponent Release test
    /// </summary>
    public class Core_CoroutineLock_Release_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_CoroutineLock_Release_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            scene.AddComponent<CoroutineLockComponent>();
            EntityRef<Scene> sceneRef = scene;

            const long testKey = 777;

            // Test 1: using syntax auto release
            {
                EntityRef<CoroutineLock> lockRef;
                using (lockRef = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey))
                {
                    scene = sceneRef;
                    CoroutineLock coroutineLock = lockRef;
                    if (coroutineLock == null)
                    {
                        Log.Console("Release: should get lock in using block");
                        return 1;
                    }
                }

                CoroutineLock releasedLock = lockRef;
                if (releasedLock != null)
                {
                    Log.Console("Release: lock should be disposed after using block");
                    return 2;
                }
                Log.Debug("Test 1 passed: using syntax releases lock correctly");
            }

            // Test 2: manual Dispose release
            {
                EntityRef<CoroutineLock> lockRef = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
                scene = sceneRef;
                CoroutineLock coroutineLock = lockRef;
                if (coroutineLock == null)
                {
                    Log.Console("Release: should get lock for manual dispose test");
                    return 3;
                }

                lockRef.Dispose();

                coroutineLock = lockRef;
                if (coroutineLock != null)
                {
                    Log.Console("Release: lock should be disposed after manual Dispose()");
                    return 4;
                }
                Log.Debug("Test 2 passed: manual Dispose releases lock correctly");
            }

            // Test 3: multiple waiters acquire lock in FIFO order
            {
                int acquireOrder = 0;
                int coroutine1Order = 0;
                int coroutine2Order = 0;
                int coroutine3Order = 0;

                // First coroutine acquires lock
                EntityRef<CoroutineLock> lockRef1 = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
                scene = sceneRef;
                coroutine1Order = ++acquireOrder;
                Log.Debug($"Coroutine 1 acquired lock, order: {coroutine1Order}");

                // Start second coroutine
                async ETTask Coroutine2()
                {
                    Scene s = sceneRef;
                    using EntityRef<CoroutineLock> lockRef = await s.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
                    coroutine2Order = ++acquireOrder;
                    Log.Debug($"Coroutine 2 acquired lock, order: {coroutine2Order}");
                }
                Coroutine2().Coroutine();

                // Wait to ensure coroutine 2 is queued first
                await scene.TimerComponent.WaitAsync(20);
                scene = sceneRef;

                // Start third coroutine
                async ETTask Coroutine3()
                {
                    Scene s = sceneRef;
                    using EntityRef<CoroutineLock> lockRef = await s.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
                    coroutine3Order = ++acquireOrder;
                    Log.Debug($"Coroutine 3 acquired lock, order: {coroutine3Order}");
                }
                Coroutine3().Coroutine();

                // Wait to ensure coroutine 2 and 3 are both waiting
                await scene.TimerComponent.WaitAsync(50);
                scene = sceneRef;

                // Release first lock
                lockRef1.Dispose();
                await scene.TimerComponent.WaitAsync(50);
                scene = sceneRef;

                // Wait for coroutine 2 to release (it's in using block)
                await scene.TimerComponent.WaitAsync(50);
                scene = sceneRef;

                // Verify order
                if (coroutine1Order != 1)
                {
                    Log.Console($"Release: coroutine 1 should be first, actual order: {coroutine1Order}");
                    return 5;
                }
                if (coroutine2Order != 2)
                {
                    Log.Console($"Release: coroutine 2 should be second, actual order: {coroutine2Order}");
                    return 6;
                }
                if (coroutine3Order != 3)
                {
                    Log.Console($"Release: coroutine 3 should be third, actual order: {coroutine3Order}");
                    return 7;
                }
                Log.Debug("Test 3 passed: waiters acquire lock in FIFO order");
            }

            Log.Debug("Core_CoroutineLock_Release_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
