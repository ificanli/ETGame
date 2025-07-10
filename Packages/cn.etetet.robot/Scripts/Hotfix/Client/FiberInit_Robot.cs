namespace ET.Client
{
    [Invoke(SceneType.Robot)]
    public class FiberInit_Robot: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<ClientQuestComponent>();
            
            root.SceneType = SceneType.Client;

            EntityRef<Scene> rootRef = root;
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
            root = rootRef;
            await LoginHelper.Login(root, "127.0.0.1:10101", root.Name, "");
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
        }
    }
}