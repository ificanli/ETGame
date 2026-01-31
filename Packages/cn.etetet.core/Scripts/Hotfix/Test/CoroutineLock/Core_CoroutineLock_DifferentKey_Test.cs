namespace ET.Test
{
    /// <summary>
    /// CoroutineLockComponent DifferentKey test
    /// </summary>
    public class Core_CoroutineLock_DifferentKey_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_CoroutineLock_DifferentKey_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            scene.AddComponent<CoroutineLockComponent>();
            EntityRef<Scene> sceneRef = scene;

            const long key1 = 100;
            const long key2 = 200;

            // Acquire lock for key1
            EntityRef<CoroutineLock> lockRef1 = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, key1);
            scene = sceneRef;
            CoroutineLock lock1 = lockRef1;
            if (lock1 == null)
            {
                Log.Console("DifferentKey: should get lock for key1");
                return 1;
            }
            Log.Debug($"Lock acquired for key {key1}");

            // Acquire lock for key2 (should succeed immediately)
            EntityRef<CoroutineLock> lockRef2 = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, key2);
            scene = sceneRef;
            CoroutineLock lock2 = lockRef2;
            if (lock2 == null)
            {
                Log.Console("DifferentKey: should get lock for key2 (different keys should not block)");
                return 2;
            }
            Log.Debug($"Lock acquired for key {key2}");

            // Verify both locks are still valid
            lock1 = lockRef1;
            lock2 = lockRef2;
            if (lock1 == null || lock2 == null)
            {
                Log.Console("DifferentKey: both locks should remain valid");
                return 3;
            }
            Log.Debug("Both locks are valid simultaneously");

            // Release locks
            lockRef1.Dispose();
            lockRef2.Dispose();

            Log.Debug("Core_CoroutineLock_DifferentKey_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
