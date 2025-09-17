namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceUnsubscribeRequestHandler : MessageHandler<Scene, ServiceUnsubscribeRequest, ServiceUnsubscribeResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceUnsubscribeRequest request, ServiceUnsubscribeResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            if (serviceDiscovery == null)
            {
                Log.Error("ServiceDiscoveryComponent not found");
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ServiceDiscoveryComponent not found";
                return;
            }

            serviceDiscovery.UnsubscribeServiceChange(request.SceneName);
            response.Error = ErrorCode.ERR_Success;

            Log.Debug($"Unsubscribe service change: {request.SceneName}");

            await ETTask.CompletedTask;
        }
    }
}