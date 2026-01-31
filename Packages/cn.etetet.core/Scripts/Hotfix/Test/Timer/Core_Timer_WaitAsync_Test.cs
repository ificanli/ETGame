namespace ET.Test
{
    /// <summary>
    /// TimerComponent WaitAsync test
    /// </summary>
    public class Core_Timer_WaitAsync_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_Timer_WaitAsync_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            TimerComponent timerComponent = scene.TimerComponent;

            // Test 1: WaitAsync 100ms should wait at least 100ms
            {
                long before = TimeInfo.Instance.ServerNow();
                await timerComponent.WaitAsync(100);
                long after = TimeInfo.Instance.ServerNow();
                long elapsed = after - before;

                if (elapsed < 100)
                {
                    Log.Console($"WaitAsync 100ms: elapsed {elapsed}ms < 100ms");
                    return 1;
                }

                Log.Debug($"Test 1 passed: WaitAsync 100ms, elapsed {elapsed}ms");
            }

            // Test 2: WaitAsync 0ms should return immediately
            {
                long before = TimeInfo.Instance.ServerNow();
                await timerComponent.WaitAsync(0);
                long after = TimeInfo.Instance.ServerNow();
                long elapsed = after - before;

                if (elapsed > 50)
                {
                    Log.Console($"WaitAsync 0ms: elapsed {elapsed}ms > 50ms, should return immediately");
                    return 2;
                }

                Log.Debug($"Test 2 passed: WaitAsync 0ms, elapsed {elapsed}ms");
            }

            // Test 3: Multiple WaitAsync in sequence
            {
                long before = TimeInfo.Instance.ServerNow();
                await timerComponent.WaitAsync(50);
                await timerComponent.WaitAsync(50);
                await timerComponent.WaitAsync(50);
                long after = TimeInfo.Instance.ServerNow();
                long elapsed = after - before;

                if (elapsed < 150)
                {
                    Log.Console($"Multiple WaitAsync: elapsed {elapsed}ms < 150ms");
                    return 3;
                }

                Log.Debug($"Test 3 passed: Multiple WaitAsync, elapsed {elapsed}ms");
            }

            Log.Debug("Core_Timer_WaitAsync_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
