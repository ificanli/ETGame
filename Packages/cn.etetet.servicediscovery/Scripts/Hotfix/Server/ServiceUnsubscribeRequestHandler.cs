namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnsubscribeRequestHandler : MessageHandler<Scene, ServiceUnsubscribeRequest, ServiceUnsubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnsubscribeRequest request, ServiceUnsubscribeResponse response)
        {
            ServiceDiscovery serviceDiscovery = scene.GetComponent<ServiceDiscovery>();
            serviceDiscovery.UnsubscribeServiceChange(request.SceneName, request.SceneType);

            Log.Debug($"Unsubscribe service change: {request.SceneName}");

            await ETTask.CompletedTask;
        }
    }
}