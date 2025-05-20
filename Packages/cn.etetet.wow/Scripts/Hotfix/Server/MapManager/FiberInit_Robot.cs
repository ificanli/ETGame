namespace ET.Server
{
    [Invoke(SceneType.MapManager)]
    public class FiberInit_MapManager: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<MapManagerComponent>();
            await ETTask.CompletedTask;
        }
    }
}