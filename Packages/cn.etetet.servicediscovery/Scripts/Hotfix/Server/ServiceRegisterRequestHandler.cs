namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceRegisterRequestHandler : MessageHandler<Scene, ServiceRegisterRequest, ServiceRegisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceRegisterRequest request, ServiceRegisterResponse response)
        {
            ServiceDiscovery serviceDiscovery = scene.GetComponent<ServiceDiscovery>();
            serviceDiscovery.RegisterService(request.SceneName, request.SceneType, request.ActorId, request.Metadata);
            await ETTask.CompletedTask;
        }
    }
}