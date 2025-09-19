using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.Location)]
    public class FiberInit_Location: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            root.AddComponent<LocationManagerComoponent>();
            
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.Get((int)root.Id);
            
            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.Zone, $"{startSceneConfig.Zone}" },
                { ServiceMetaKey.InnerIPPort, $"{startSceneConfig.InnerIPPort}" }
            };
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metadata);
        }
    }
}