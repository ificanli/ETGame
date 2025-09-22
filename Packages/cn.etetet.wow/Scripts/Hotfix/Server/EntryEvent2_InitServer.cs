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
            
            // NetInner创建完成会设置Option.Instance.InnerIP跟Option.Instance.InnerPort
            await fiber.CreateFiberWithId(Const.NetInnerFiberId, SchedulerType.ThreadPool, Const.NetInnerFiberId, 0, SceneType.NetInner, $"NetInner_{process}_{Options.Instance.ReplicaIndex}");

            if (startProcessConfig != null)
            {
                // 根据配置创建纤程
                var scenes = StartSceneConfigCategory.Instance.GetByProcess(process);

                foreach (StartSceneConfig startConfig in scenes)
                {
                    int sceneType = SceneTypeSingleton.Instance.GetSceneType(startConfig.SceneType);
                    if (sceneType == SceneType.ServiceDiscovery)
                    {
                        await fiber.CreateFiberWithId(Const.ServiceDiscoveryFiberId, SchedulerType.ThreadPool, Const.ServiceDiscoveryFiberId, startConfig.Zone, sceneType, startConfig.Name);
                    }
                    else
                    {
                        await fiber.CreateFiber(SchedulerType.ThreadPool, startConfig.Id, startConfig.Zone, sceneType, startConfig.Name);
                    }
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