namespace ET.Test
{
    /// <summary>
    /// TimerComponent WaitTillAsync test
    /// </summary>
    public class Core_Timer_WaitTillAsync_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_Timer_WaitTillAsync_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            TimerComponent timerComponent = scene.TimerComponent;

            // Test 1: WaitTillAsync to future time
            {
                long now = TimeInfo.Instance.ServerNow();
                long targetTime = now + 100;
                long before = TimeInfo.Instance.ServerNow();
                await timerComponent.WaitTillAsync(targetTime);
                long after = TimeInfo.Instance.ServerNow();

                if (after < targetTime)
                {
                    Log.Console($"WaitTillAsync future: after {after} < targetTime {targetTime}");
                    return 1;
                }

                Log.Debug($"Test 1 passed: WaitTillAsync to future time, elapsed {after - before}ms");
            }

            // Test 2: WaitTillAsync to past time should return immediately
            {
                long now = TimeInfo.Instance.ServerNow();
                long pastTime = now - 1000;
                long before = TimeInfo.Instance.ServerNow();
                await timerComponent.WaitTillAsync(pastTime);
                long after = TimeInfo.Instance.ServerNow();
                long elapsed = after - before;

                if (elapsed > 50)
                {
                    Log.Console($"WaitTillAsync past: elapsed {elapsed}ms > 50ms, should return immediately");
                    return 2;
                }

                Log.Debug($"Test 2 passed: WaitTillAsync to past time, elapsed {elapsed}ms");
            }

            // Test 3: WaitTillAsync to current time should return immediately
            {
                long now = TimeInfo.Instance.ServerNow();
                long before = TimeInfo.Instance.ServerNow();
                await timerComponent.WaitTillAsync(now);
                long after = TimeInfo.Instance.ServerNow();
                long elapsed = after - before;

                if (elapsed > 50)
                {
                    Log.Console($"WaitTillAsync current: elapsed {elapsed}ms > 50ms, should return immediately");
                    return 3;
                }

                Log.Debug($"Test 3 passed: WaitTillAsync to current time, elapsed {elapsed}ms");
            }

            Log.Debug("Core_Timer_WaitTillAsync_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
