using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.Gate)]
    public class FiberInit_Gate: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<GateSessionKeyComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.Get((int)root.Id);
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(startSceneConfig.InnerIPPort));
            
            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxyComponent;
            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.InnerIPPort, $"{startSceneConfig.InnerIPPort}" }
            };
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metadata);
            
            // 订阅location
            Dictionary<string, string> filterMeta = new();
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.Location, filterMeta);
            
            // 订阅mapmanager
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.MapManager, filterMeta);
        }
    }
}