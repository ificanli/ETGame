using System.Collections.Generic;

namespace ET.Server
{
    [Invoke(SceneType.MapManager)]
    public class FiberInit_MapManager: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            
            root.AddComponent<MapManagerComponent>();
            
            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxyComponent;
            Dictionary<string, string> metadata = new();
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metadata);
            
            serviceDiscoveryProxyComponent = serviceDiscoveryProxyComponentRef;
            // 订阅location
            Dictionary<string, string> filterMeta = new();
            await serviceDiscoveryProxyComponent.SubscribeServiceChange(SceneType.Location, filterMeta);
            
            await ETTask.CompletedTask;
        }
    }
}