namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceHeartbeatRequestHandler : MessageHandler<Scene, ServiceHeartbeatRequest, ServiceHeartbeatResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceHeartbeatRequest request, ServiceHeartbeatResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            serviceDiscovery.UpdateServiceHeartbeat(request.SceneName);

            await ETTask.CompletedTask;
        }
    }
}