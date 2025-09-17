using System.Collections.Generic;

namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceQueryRequestHandler : MessageHandler<Scene, ServiceQueryRequest, ServiceQueryResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceQueryRequest request, ServiceQueryResponse response)
        {
            ServiceDiscoveryComponent serviceDiscovery = scene.GetComponent<ServiceDiscoveryComponent>();
            if (serviceDiscovery == null)
            {
                Log.Error("ServiceDiscoveryComponent not found");
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ServiceDiscoveryComponent not found";
                return;
            }

            List<ServiceInfo> services = serviceDiscovery.GetServicesBySceneType(request.SceneType);
            foreach (ServiceInfo serviceInfo in services)
            {
                response.Services.Add(serviceInfo.ToProto());
            }

            Log.Debug($"Query services: {request.SceneType} found {services.Count} services");

            await ETTask.CompletedTask;
        }
    }
}