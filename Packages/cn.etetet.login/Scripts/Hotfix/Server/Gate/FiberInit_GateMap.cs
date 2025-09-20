using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.GateMap)]
    public class FiberInit_GateMap: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<UnitComponent>();
            
            root.AddComponent<AOIManagerComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxyComponent;
            Dictionary<string, string> metaData = new();
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metaData);
            // 订阅location,并未注册Map
            Dictionary<string, string> filterMeta = new();
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.Location, filterMeta);
            
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.MapManager, filterMeta);
        }
    }
}