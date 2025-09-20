using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.RouterManager)]
    public class FiberInit_RouterManager: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<TimerComponent>();
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.Get((int)root.Id);
            root.AddComponent<HttpComponent, string>($"http://*:{startSceneConfig.Port}/");

            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxyComponent;
            
            // 订阅Router
            Dictionary<string, string> filterMeta = new();
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.Router, filterMeta);
        }
    }
}