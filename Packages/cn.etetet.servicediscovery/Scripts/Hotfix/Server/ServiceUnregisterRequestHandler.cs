namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnregisterRequestHandler : MessageHandler<Scene, ServiceUnregisterRequest, ServiceUnregisterResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnregisterRequest request, ServiceUnregisterResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            if (serviceDiscovery == null)
            {
                Log.Error("ServiceDiscoveryComponent not found");
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ServiceDiscoveryComponent not found";
                return;
            }

            bool success = serviceDiscovery.UnregisterService(request.SceneName);
            if (success)
            {
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Service unregistered: {request.SceneName} {request.ActorId}");
            }
            else
            {
                response.Error = ErrorCode.ERR_ServiceNotFound;
                response.Message = "Service not found";
            }

            await ETTask.CompletedTask;
        }
    }
}