namespace ET.Server
{
    [Invoke(SceneType.Match)]
    public class FiberInit_Match : AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();

            // 匹配队列
            MatchQueueComponent matchQueue = root.AddComponent<MatchQueueComponent>();
            matchQueue.TimerId = root.TimerComponent.NewRepeatedTimer(1000, TimerInvokeType.MatchTick, matchQueue);

            // 注册服务发现
            ServiceDiscoveryProxy serviceDiscoveryProxy =
                root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyRef = serviceDiscoveryProxy;
            await serviceDiscoveryProxy.RegisterToServiceDiscovery();

            // 订阅 Gate 服务，匹配成功后需要通知 Gate
            serviceDiscoveryProxy = serviceDiscoveryProxyRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Gate",
                new StringKV() { {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Gate)} });
        }
    }
}
