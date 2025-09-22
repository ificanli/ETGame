using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.Router)]
    public class FiberInit_Router: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            
            string innerIP = AddressSingleton.Instance.InnerIP;
            string outerIP = AddressSingleton.Instance.OuterIP;
            int outerPort = AddressSingleton.Instance.OuterPort;
            
            IPEndPoint outerIPOutPort;
            if (outerPort > 0)
            {
                outerIPOutPort = new Address(outerIP, outerPort);
            }
            else
            {
                StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(root.Name);
                outerIPOutPort = startSceneConfig.OuterIPOuterPort;
                innerIP = startSceneConfig.StartProcessConfig.InnerIP;
            }
            
            
            // 开发期间使用OuterIPPort，云服务器因为本机没有OuterIP，所以要改成InnerIPPort，然后在云防火墙中端口映射到InnerIPPort
            root.AddComponent<RouterComponent, IPEndPoint, string>(outerIPOutPort, innerIP);
            
            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.OuterIPOuterPort, $"{outerIPOutPort}" }
            };
            await serviceDiscoveryProxy.RegisterToServiceDiscovery(metadata);
        }
    }
}