using System.Collections.Generic;

namespace ET.Server
{
    [MessageHandler(SceneType.ServiceDiscovery)]
    public class ServiceQueryRequestHandler : MessageHandler<Scene, ServiceQueryRequest, ServiceQueryResponse>
    {
        protected override async ETTask Run(Scene scene, ServiceQueryRequest request, ServiceQueryResponse response)
        {
            ServiceDiscovery serviceDiscovery = scene.GetComponent<ServiceDiscovery>();
            List<ServiceInfo> services = serviceDiscovery.GetServiceInfoByFilter(request.Filter);
            foreach (ServiceInfo serviceInfo in services)
            {
                response.Services.Add(serviceInfo.ToProto());
            }

            Log.Debug($"Query services: {request} found {response}");

            await ETTask.CompletedTask;
        }
    }
}