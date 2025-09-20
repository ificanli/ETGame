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
            
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(root.Name);
            
            // 开发期间使用OuterIPPort，云服务器因为本机没有OuterIP，所以要改成InnerIPPort，然后在云防火墙中端口映射到InnerIPPort
            StartProcessConfig startProcessConfig = StartProcessConfigCategory.Instance.Get(startSceneConfig.Process);
            IPEndPoint outIPPort = NetworkHelper.ToIPEndPoint($"{startProcessConfig.OuterIP}:{startSceneConfig.Port}");
            root.AddComponent<RouterComponent, IPEndPoint, string>(outIPPort, startProcessConfig.InnerIP);
            
            // 注册服务发现
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.AddComponent<ServiceDiscoveryProxyComponent>();
            Dictionary<string, string> metadata = new()
            {
                { ServiceMetaKey.OuterIPPort, $"{startSceneConfig.StartProcessConfig.OuterIP}:{startSceneConfig.Port}" }
            };
            await serviceDiscoveryProxyComponent.RegisterToServiceDiscovery(metadata);
        }
    }
}