using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ET.Server
{
    [HttpHandler(SceneType.RouterManager, "/get_router")]
    public class HttpGetRouterHandler : IHttpHandler
    {
        public async ETTask Handle(Scene scene, HttpListenerContext context)
        {
            HttpGetRouterResponse httpGetRouterResponse = HttpGetRouterResponse.Create();
            
            ServiceDiscoveryProxy serviceDiscoveryProxy = scene.GetComponent<ServiceDiscoveryProxy>();
            List<string> realmNames = serviceDiscoveryProxy.GetBySceneType(SceneType.Realm);
            foreach (string realmName in realmNames)
            {
                ServiceCacheInfo serviceCacheInfo = serviceDiscoveryProxy.GetByName(realmName);
                // 这里是要用InnerIP，因为云服务器上realm绑定不了OuterIP的,所以realm的内网外网的socket都是监听内网地址
                httpGetRouterResponse.Realms.Add(serviceCacheInfo.Metadata[ServiceMetaKey.InnerIPOuterPort]);
            }
            
            List<string> routerNames = serviceDiscoveryProxy.GetBySceneType(SceneType.Router);
            foreach (string routerName in routerNames)
            {
                ServiceCacheInfo serviceCacheInfo = serviceDiscoveryProxy.GetByName(routerName);
                httpGetRouterResponse.Routers.Add(serviceCacheInfo.Metadata[ServiceMetaKey.OuterIPOuterPort]);
            }
            
            HttpListenerRequest request = context.Request;
            using HttpListenerResponse response = context.Response;
            
            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                response.AddHeader("Access-Control-Max-Age", "1728000");
            }
            response.AppendHeader("Access-Control-Allow-Origin", "*");
            
            byte[] bytes = MongoHelper.ToJson(httpGetRouterResponse).ToUtf8();
            response.StatusCode = 200;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = bytes.Length;
            EntityRef<Scene> sceneRef = scene;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            scene = sceneRef;
            await scene.Root().GetComponent<TimerComponent>().WaitAsync(1000);
        }
    }
}