namespace ET.Test
{
    /// <summary>
    /// CoroutineLockComponent Concurrency test
    /// </summary>
    public class Core_CoroutineLock_Concurrency_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_CoroutineLock_Concurrency_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            scene.AddComponent<CoroutineLockComponent>();
            EntityRef<Scene> sceneRef = scene;

            const long testKey = 999;
            const int maxConcurrency = 2;

            // Set max concurrency to 2
            scene.CoroutineLockComponent.SetMaxConcurrency(CoroutineLockType.TestLock, testKey, maxConcurrency);
            Log.Debug($"Set maxConcurrency to {maxConcurrency}");

            // Acquire first lock
            EntityRef<CoroutineLock> lockRef1 = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
            scene = sceneRef;
            CoroutineLock lock1 = lockRef1;
            if (lock1 == null)
            {
                Log.Console("Concurrency: first lock should be acquired");
                return 1;
            }
            Log.Debug("Lock 1 acquired");

            // Acquire second lock (should succeed immediately since maxConcurrency=2)
            EntityRef<CoroutineLock> lockRef2 = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
            scene = sceneRef;
            CoroutineLock lock2 = lockRef2;
            if (lock2 == null)
            {
                Log.Console("Concurrency: second lock should be acquired (maxConcurrency=2)");
                return 2;
            }
            Log.Debug("Lock 2 acquired");

            // Start third coroutine (should wait)
            bool thirdCoroutineGotLock = false;
            async ETTask ThirdCoroutine()
            {
                Scene s = sceneRef;
                using EntityRef<CoroutineLock> lockRef3 = await s.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
                thirdCoroutineGotLock = true;
                Log.Debug("Lock 3 acquired");
            }
            ThirdCoroutine().Coroutine();

            // Wait a short time
            await scene.TimerComponent.WaitAsync(50);
            scene = sceneRef;

            // Verify third coroutine is still waiting
            if (thirdCoroutineGotLock)
            {
                Log.Console("Concurrency: third coroutine should wait (maxConcurrency=2, already 2 locks held)");
                return 3;
            }
            Log.Debug("Verified: Third coroutine is waiting");

            // Release first lock
            lockRef1.Dispose();
            Log.Debug("Lock 1 released");

            // Wait for next frame
            await scene.TimerComponent.WaitAsync(50);
            scene = sceneRef;

            // Verify third coroutine got lock
            if (!thirdCoroutineGotLock)
            {
                Log.Console("Concurrency: third coroutine should get lock after one is released");
                return 4;
            }
            Log.Debug("Verified: Third coroutine got lock");

            // Cleanup
            lockRef2.Dispose();

            Log.Debug("Core_CoroutineLock_Concurrency_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
