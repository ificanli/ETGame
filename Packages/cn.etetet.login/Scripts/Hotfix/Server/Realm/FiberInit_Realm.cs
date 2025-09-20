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
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.Get(root.Fiber.Id);
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(startSceneConfig.InnerIPPort));
            
            root.AddComponent<ServiceMessageSender>();
            
            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxyComponent;

            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.InnerIPPort, $"{startSceneConfig.InnerIPPort}" }
            };
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metadata);
            
            // 订阅跟realm属于同一个zone的Gate
            Dictionary<string, string> filterMeta = new()
            {
                { ServiceMetaKey.Zone, $"{fiberInit.Fiber.Zone}" }
            };
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.Gate, filterMeta);
        }
    }
}