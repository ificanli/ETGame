using System;

namespace ET.Test
{
    [Invoke(SceneType.Test)]
    public class FiberInit_Test: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<ProcessInnerSender>();

            World.Instance.AddSingleton<TestDispatcher>();

            if (string.IsNullOrWhiteSpace(Options.Instance.TestName))
            {
                root.AddComponent<Server.ConsoleComponent>();
            }
            else
            {
                AutoRunTests(fiber).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        private static async ETTask AutoRunTests(Fiber fiber)
        {
            try
            {
                await fiber.WaitFrameFinish();

                string testName = Options.Instance.TestName;
                Log.Console($"auto run tests: {testName}");
                await TestRunnerHelper.RunAndExit(fiber, new TestArgs() { Name = testName });
            }
            catch (Exception e)
            {
                Log.Console(e.ToString());
                Environment.Exit(1);
            }
        }
    }
}
