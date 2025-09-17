namespace ET.Server
{
    [MessageHandler(SceneType.All)]
    public class ServiceChangeNotificationHandler : MessageHandler<Scene, ServiceChangeNotification>
    {
        protected override async ETTask Run(Scene scene, ServiceChangeNotification message)
        {
            ServiceDiscoveryProxyComponent proxy = scene.GetComponent<ServiceDiscoveryProxyComponent>();
            if (proxy != null)
            {
                proxy.OnServiceChangeNotification(message);
            }

            await ETTask.CompletedTask;
        }
    }
}