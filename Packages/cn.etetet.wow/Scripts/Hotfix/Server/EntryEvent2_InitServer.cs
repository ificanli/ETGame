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

            // 先看环境变量是否有地址传过来，如果没有，则使用StartProcessConfig的地址
            AddressSingleton addressSingleton = World.Instance.AddSingleton<AddressSingleton>();
            if (addressSingleton.InnerAddress == null)
            {
                addressSingleton.InnerAddress = startProcessConfig.Address;
            }
            
            // 因为bind的地址有可能是0.0.0.0:0,NetInner创建完成会设置具体的地址到AddressSingleton中
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