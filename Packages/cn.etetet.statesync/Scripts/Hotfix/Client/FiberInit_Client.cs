namespace ET.Client
{
    [Invoke(SceneType.Client)]
    public class FiberInit_Client: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            EntityRef<Scene> rootRef = root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<QuestComponent>();
            root.AddComponent<ItemComponent>();
            
            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}