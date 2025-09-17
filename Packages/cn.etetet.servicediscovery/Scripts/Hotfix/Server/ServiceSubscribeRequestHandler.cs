namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceSubscribeRequestHandler : MessageHandler<Scene, ServiceSubscribeRequest, ServiceSubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceSubscribeRequest request, ServiceSubscribeResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            serviceDiscovery.SubscribeServiceChange(request.SceneName, request.SceneTypes);

            Log.Debug($"Subscribe service change: {request.SceneName}");

            await ETTask.CompletedTask;
        }
    }
}