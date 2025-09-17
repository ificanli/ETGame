namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceRegisterRequestHandler : MessageHandler<Scene, ServiceRegisterRequest, ServiceRegisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceRegisterRequest request, ServiceRegisterResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            if (serviceDiscovery == null)
            {
                Log.Error("ServiceDiscoveryComponent not found");
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ServiceDiscoveryComponent not found";
                return;
            }

            serviceDiscovery.RegisterService(request.SceneName, request.SceneType, request.ActorId);

            await ETTask.CompletedTask;
        }
    }
}