namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnregisterRequestHandler : MessageHandler<Scene, ServiceUnregisterRequest, ServiceUnregisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnregisterRequest request, ServiceUnregisterResponse response)
        {
            ServiceDiscovery serviceDiscovery = scene.GetComponent<ServiceDiscovery>();
            serviceDiscovery.UnregisterService(request.SceneName);

            await ETTask.CompletedTask;
        }
    }
}