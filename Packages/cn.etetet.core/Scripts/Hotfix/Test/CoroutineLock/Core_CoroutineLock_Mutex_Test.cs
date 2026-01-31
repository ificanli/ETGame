namespace ET.Test
{
    /// <summary>
    /// CoroutineLockComponent Mutex test
    /// </summary>
    public class Core_CoroutineLock_Mutex_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_CoroutineLock_Mutex_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            scene.AddComponent<CoroutineLockComponent>();
            EntityRef<Scene> sceneRef = scene;

            const long testKey = 12345;
            bool secondCoroutineGotLock = false;
            bool secondCoroutineStarted = false;

            // Coroutine A acquires lock
            EntityRef<CoroutineLock> lockRefA = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
            scene = sceneRef;
            CoroutineLock lockA = lockRefA;
            if (lockA == null)
            {
                Log.Console("Mutex: first coroutine should get lock");
                return 1;
            }
            Log.Debug("Coroutine A acquired lock");

            // Start coroutine B (don't await, let it wait in background)
            async ETTask SecondCoroutine()
            {
                secondCoroutineStarted = true;
                Scene s = sceneRef;
                using EntityRef<CoroutineLock> lockRefB = await s.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, testKey);
                secondCoroutineGotLock = true;
                Log.Debug("Coroutine B acquired lock");
            }
            SecondCoroutine().Coroutine();

            // Wait a short time to ensure coroutine B has started waiting
            await scene.TimerComponent.WaitAsync(50);
            scene = sceneRef;

            // Verify coroutine B is still waiting
            if (!secondCoroutineStarted)
            {
                Log.Console("Mutex: second coroutine should have started");
                return 2;
            }
            if (secondCoroutineGotLock)
            {
                Log.Console("Mutex: second coroutine should NOT get lock while first holds it");
                return 2;
            }
            Log.Debug("Verified: Coroutine B is waiting");

            // Coroutine A releases lock
            lockRefA.Dispose();
            Log.Debug("Coroutine A released lock");

            // Wait for next frame to process nextFrameRun queue
            await scene.TimerComponent.WaitAsync(50);
            scene = sceneRef;

            // Verify coroutine B got lock
            if (!secondCoroutineGotLock)
            {
                Log.Console("Mutex: second coroutine should get lock after first releases");
                return 3;
            }
            Log.Debug("Verified: Coroutine B got lock after A released");

            Log.Debug("Core_CoroutineLock_Mutex_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
