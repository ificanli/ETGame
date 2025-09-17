namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceSubscribeRequestHandler : MessageHandler<Scene, ServiceSubscribeRequest, ServiceSubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceSubscribeRequest request, ServiceSubscribeResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            if (serviceDiscovery == null)
            {
                Log.Error("ServiceDiscoveryComponent not found");
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ServiceDiscoveryComponent not found";
                return;
            }

            serviceDiscovery.SubscribeServiceChange(request.SceneTypes, request.SceneName);
            response.Error = ErrorCode.ERR_Success;

            Log.Debug($"Subscribe service change: {request.SceneName}");

            await ETTask.CompletedTask;
        }
    }
}