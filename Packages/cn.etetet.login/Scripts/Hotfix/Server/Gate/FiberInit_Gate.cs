using System;
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

            IPEndPoint innerIPOuterPort;
            if (Options.Instance.OuterPort > 0)
            {
                innerIPOuterPort = new Address(Options.Instance.InnerIP, Options.Instance.OuterPort);
            }
            else
            {
                StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(root.Name);
                innerIPOuterPort = startSceneConfig.InnerIPOuterPort;
            }
            
            NetComponent netComponent = root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(innerIPOuterPort));
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;
            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.InnerIPOuterPort, $"{netComponent.GetBindPoint()}" }
            };
            await serviceDiscoveryProxy.RegisterToServiceDiscovery(metadata);
            
            // 订阅location
            Dictionary<string, string> filterMeta = new();
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange(SceneType.Location, filterMeta);
        }
    }
}