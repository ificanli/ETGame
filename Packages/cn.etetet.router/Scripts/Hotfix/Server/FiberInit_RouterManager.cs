using System;
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
            
            int outerPort = AddressSingleton.Instance.GetSceneOuterPort(root.Name);
            
            root.AddComponent<HttpComponent, string>($"http://*:{outerPort}/");

            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            Dictionary<string, string> metaData = new();
            await serviceDiscoveryProxy.RegisterToServiceDiscovery(metaData);
            // 订阅Router
            Dictionary<string, string> filterMeta = new();
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange(SceneType.Router, filterMeta);
            
            // 订阅Realm
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange(SceneType.Realm, filterMeta);
        }
    }
}