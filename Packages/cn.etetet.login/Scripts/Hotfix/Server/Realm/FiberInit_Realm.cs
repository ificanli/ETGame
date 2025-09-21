using System.Collections.Generic;

namespace ET.Server
{
    [Invoke(SceneType.Realm)]
    public class FiberInit_Realm: AInvokeHandler<FiberInit, ETTask>
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
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(startSceneConfig.InnerIPPort));
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;

            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.InnerIPPort, $"{startSceneConfig.InnerIPPort}" }
            };
            await serviceDiscoveryProxy.RegisterToServiceDiscovery(metadata);
            
            // 订阅跟realm属于同一个zone的Gate
            Dictionary<string, string> filterMeta = new()
            {
                { ServiceMetaKey.Zone, $"{fiberInit.Fiber.Zone}" }
            };
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange(SceneType.Gate, filterMeta);
        }
    }
}