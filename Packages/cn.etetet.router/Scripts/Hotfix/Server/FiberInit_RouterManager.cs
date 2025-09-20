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
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(root.Name);
            root.AddComponent<HttpComponent, string>($"http://*:{startSceneConfig.Port}/");

            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxyComponent;
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            Dictionary<string, string> metaData = new();
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metaData);
            // 订阅Router
            Dictionary<string, string> filterMeta = new();
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.Router, filterMeta);
        }
    }
}