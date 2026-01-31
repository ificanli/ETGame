namespace ET.Test
{
    /// <summary>
    /// CoroutineLockComponent Wait test
    /// </summary>
    public class Core_CoroutineLock_Wait_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_CoroutineLock_Wait_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            scene.AddComponent<CoroutineLockComponent>();

            // Test 1: Acquire and release lock
            {
                EntityRef<CoroutineLock> lockRef;
                using (lockRef = await scene.CoroutineLockComponent.Wait(CoroutineLockType.TestLock, 1))
                {
                    CoroutineLock coroutineLock = lockRef;
                    if (coroutineLock == null)
                    {
                        Log.Console("Wait: should return valid lock");
                        return 1;
                    }
                    Log.Debug("Test 1: Lock acquired successfully");
                }

                CoroutineLock releasedLock = lockRef;
                if (releasedLock != null)
                {
                    Log.Console("Wait: lock should be disposed after using block");
                    return 2;
                }
                Log.Debug("Test 1 passed: Lock acquired and released correctly");
            }

            Log.Debug("Core_CoroutineLock_Wait_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
