namespace ET.Test
{
    /// <summary>
    /// TimerComponent Cancellation test
    /// </summary>
    public class Core_Timer_Cancellation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Core_Timer_Cancellation_Test));

            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<TimerComponent>();
            TimerComponent timerComponent = scene.TimerComponent;

            // Test 1: Cancel WaitAsync with ETCancellationToken
            {
                ETCancellationToken cancellationToken = new();
                bool waitCompleted = false;

                // Start a long wait in background
                async ETTask WaitTask()
                {
                    await timerComponent.WaitAsync(5000).NewContext(cancellationToken);
                    waitCompleted = true;
                }

                WaitTask().Coroutine();

                // Wait a bit then cancel
                await timerComponent.WaitAsync(100);
                cancellationToken.Cancel();

                // Wait for task to complete
                await timerComponent.WaitAsync(50);

                if (!waitCompleted)
                {
                    Log.Console("Cancel WaitAsync: wait should complete after cancel");
                    return 1;
                }

                Log.Debug("Test 1 passed: Cancel WaitAsync with ETCancellationToken");
            }

            // Test 2: Cancel WaitTillAsync with ETCancellationToken
            {
                ETCancellationToken cancellationToken = new();
                bool waitCompleted = false;

                async ETTask WaitTask()
                {
                    long tillTime = TimeInfo.Instance.ServerNow() + 5000;
                    await timerComponent.WaitTillAsync(tillTime).NewContext(cancellationToken);
                    waitCompleted = true;
                }

                WaitTask().Coroutine();

                // Wait a bit then cancel
                await timerComponent.WaitAsync(100);
                cancellationToken.Cancel();

                // Wait for task to complete
                await timerComponent.WaitAsync(50);

                if (!waitCompleted)
                {
                    Log.Console("Cancel WaitTillAsync: wait should complete after cancel");
                    return 2;
                }

                Log.Debug("Test 2 passed: Cancel WaitTillAsync with ETCancellationToken");
            }

            // Test 3: Multiple cancellations
            {
                ETCancellationToken cancellationToken = new();
                int completedCount = 0;

                async ETTask WaitTask1()
                {
                    await timerComponent.WaitAsync(5000).NewContext(cancellationToken);
                    completedCount++;
                }

                async ETTask WaitTask2()
                {
                    await timerComponent.WaitAsync(5000).NewContext(cancellationToken);
                    completedCount++;
                }

                WaitTask1().Coroutine();
                WaitTask2().Coroutine();

                await timerComponent.WaitAsync(100);
                cancellationToken.Cancel();

                await timerComponent.WaitAsync(50);

                if (completedCount != 2)
                {
                    Log.Console($"Multiple cancellations: completedCount should be 2, actual {completedCount}");
                    return 3;
                }

                Log.Debug("Test 3 passed: Multiple cancellations");
            }

            Log.Debug("Core_Timer_Cancellation_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
