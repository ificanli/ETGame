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
            root.AddComponent<Server.ConsoleComponent>();

            World.Instance.AddSingleton<TestDispatcher>();

            await ETTask.CompletedTask;
        }
    }
}