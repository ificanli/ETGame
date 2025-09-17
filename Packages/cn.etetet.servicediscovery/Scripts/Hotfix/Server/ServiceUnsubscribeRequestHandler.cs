namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnsubscribeRequestHandler : MessageHandler<Scene, ServiceUnsubscribeRequest, ServiceUnsubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnsubscribeRequest request, ServiceUnsubscribeResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            serviceDiscovery.UnsubscribeServiceChange(request.SceneName, request.SceneTypes);

            Log.Debug($"Unsubscribe service change: {request.SceneName}");

            await ETTask.CompletedTask;
        }
    }
}