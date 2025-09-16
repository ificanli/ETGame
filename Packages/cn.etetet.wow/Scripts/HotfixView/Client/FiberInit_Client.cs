namespace ET.Client
{
    [Invoke(SceneType.Client)]
    public class FiberInit_Client: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            World.Instance.AddSingleton<YIUIEventComponent>();
            
            EntityRef<Scene> rootRef = root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<ResourcesLoaderComponent>();
            root.AddComponent<GlobalComponent>();
            root.AddComponent<ClientQuestComponent>();

            bool result = await root.AddComponent<YIUIMgrComponent>().Initialize();
            if (!result)
            {
                Log.Error("初始化UI失败");
                return;
            }

            root = rootRef;
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}