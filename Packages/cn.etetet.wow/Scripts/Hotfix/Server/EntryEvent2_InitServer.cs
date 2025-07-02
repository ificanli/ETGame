namespace ET.Server
{
    [Event(SceneType.WOW)]
    public class EntryEvent2_InitServer: AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            Fiber fiber = root.Fiber;
            EntityRef<Scene> rootRef = root;
            
            int process = root.Fiber.Process;
            StartProcessConfig startProcessConfig = StartProcessConfigCategory.Instance.Get(process);
            if (startProcessConfig.Port != 0)
            {
                await fiber.CreateFiber(SchedulerType.ThreadPool, SceneType.NetInner, 0, SceneType.NetInner, "NetInner");
            }

            // 根据配置创建纤程
            var scenes = StartSceneConfigCategory.Instance.GetByProcess(process);
            
            foreach (StartSceneConfig startConfig in scenes)
            {
                int sceneType = SceneTypeSingleton.Instance.GetSceneType(startConfig.SceneType);
                await fiber.CreateFiber(SchedulerType.ThreadPool, startConfig.Id, startConfig.Zone, sceneType, startConfig.Name);
            }
            
            root = rootRef;
            root.AddComponent<RobotManagerComponent>();
            if (Options.Instance.Console == 1)
            {
                root = rootRef;
                root.AddComponent<ConsoleComponent>();
            }
        }
    }
}