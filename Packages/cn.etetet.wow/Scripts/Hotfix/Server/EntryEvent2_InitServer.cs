namespace ET.Server
{
    [Event(SceneType.WOW)]
    public class EntryEvent2_InitServer: AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            Fiber fiber = root.Fiber;
            EntityRef<Scene> rootRef = root;
            
            int process = Options.Instance.Process;
            StartProcessConfig startProcessConfig = StartProcessConfigCategory.Instance.Get(process);
            
            if (Options.Instance.IP == "")
            {
                Options.Instance.Address = startProcessConfig.Address;
            }

            await fiber.CreateFiberWithId(SceneType.NetInner, SchedulerType.ThreadPool, IdGenerater.Instance.GenerateId(), 0, SceneType.NetInner, "NetInner");

            if (startProcessConfig != null)
            {
                // 根据配置创建纤程
                var scenes = StartSceneConfigCategory.Instance.GetByProcess(process);

                foreach (StartSceneConfig startConfig in scenes)
                {
                    int sceneType = SceneTypeSingleton.Instance.GetSceneType(startConfig.SceneType);
                    await fiber.CreateFiberWithId(startConfig.Id, SchedulerType.ThreadPool, startConfig.Id, startConfig.Zone, sceneType,
                        startConfig.Name);
                }
            }

            root = rootRef;
            if (Options.Instance.Console == 1)
            {
                root.AddComponent<ConsoleComponent>();
            }
        }
    }
}