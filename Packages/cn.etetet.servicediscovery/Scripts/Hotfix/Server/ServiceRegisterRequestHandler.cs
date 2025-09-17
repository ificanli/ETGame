namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceRegisterRequestHandler : MessageHandler<Scene, ServiceRegisterRequest, ServiceRegisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceRegisterRequest request, ServiceRegisterResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            serviceDiscovery.RegisterService(request.SceneName, request.SceneType, request.ActorId);
            await ETTask.CompletedTask;
        }
    }
}