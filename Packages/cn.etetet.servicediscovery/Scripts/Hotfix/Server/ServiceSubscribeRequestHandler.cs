namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceSubscribeRequestHandler : MessageHandler<Scene, ServiceSubscribeRequest, ServiceSubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceSubscribeRequest request, ServiceSubscribeResponse response)
        {
            ServiceDiscovery serviceDiscovery = scene.GetComponent<ServiceDiscovery>();
            serviceDiscovery.SubscribeServiceChange(request.SceneName, request.SceneType, request.FilterMetadata);

            Log.Debug($"Subscribe service change: {request.SceneName}");

            await ETTask.CompletedTask;
        }
    }
}