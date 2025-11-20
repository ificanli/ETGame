using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 每个测试用例都是一个全新的环境，全新的服务器环境
    /// </summary>
    [Invoke(SceneType.TestCase)]
    public class FiberInit_TestCase: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            LogMsg.Instance.AddIgnore(typeof(ServiceHeartbeatRequest));
            LogMsg.Instance.AddIgnore(typeof(ServiceHeartbeatResponse));
            
            Fiber fiber = fiberInit.Fiber;
            
            int process = Options.Instance.Process;
            
            StartProcessConfig startProcessConfig = StartProcessConfigCategory.Instance.Get(process);
            // 先看环境变量是否有地址传过来，如果没有，则使用StartProcessConfig的地址跟端口
            AddressSingleton addressSingleton = World.Instance.AddSingleton<AddressSingleton>();
            addressSingleton.SetInnerIPInnerPortOuterIP(startProcessConfig);

            // 根据配置创建纤程
            var scenes = StartSceneConfigCategory.Instance.GetByProcess(process);
            
            foreach (StartSceneConfig startConfig in scenes)
            {
                int sceneType = SceneTypeSingleton.Instance.GetSceneType(startConfig.SceneType);
                if (sceneType == SceneType.ServiceDiscovery)
                {
                    await fiber.CreateFiberWithId(Const.ServiceDiscoveryFiberId, startConfig.Id, startConfig.Zone, sceneType, startConfig.Name);
                }
                else
                {
                    await fiber.CreateFiber(startConfig.Id, startConfig.Zone, sceneType, startConfig.Name);   
                }
            }
        }
    }
}