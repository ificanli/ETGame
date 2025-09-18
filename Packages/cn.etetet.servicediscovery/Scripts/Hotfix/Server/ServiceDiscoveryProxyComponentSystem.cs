using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现代理组件系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryProxyComponent))]
    public static partial class ServiceDiscoveryProxyComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryProxyComponent self)
        {
            Scene root = self.Root();
            if (root.IsDisposed)
            {
                return;
            }
            root.GetComponent<TimerComponent>().Remove(ref self.HeartbeatTimer);
        }
        
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryProxyComponent self)
        {
            self.MessageSender = self.Root().GetComponent<MessageSender>();
        }

        [Invoke(TimerInvokeType.ServiceDiscoveryProxyHeartbeat)]
        public class ServiceDiscoveryProxyHeartbeat : ATimer<ServiceDiscoveryProxyComponent>
        {
            protected override void Run(ServiceDiscoveryProxyComponent self)
            {
                self.SendHeartbeat().NoContext();
            }
        }

        /// <summary>
        /// 设置服务发现服务器地址
        /// </summary>
        public static void SetServiceDiscoveryServer(this ServiceDiscoveryProxyComponent self, ActorId serviceDiscoveryActorId)
        {
            self.ServiceDiscoveryActorId = serviceDiscoveryActorId;
            Log.Debug($"Set ServiceDiscovery server: {serviceDiscoveryActorId}");
        }

        /// <summary>
        /// 注册到服务发现服务器
        /// </summary>
        public static async ETTask RegisterToServiceDiscovery(this ServiceDiscoveryProxyComponent self)
        {
            ServiceRegisterRequest request = ServiceRegisterRequest.Create();
            Scene root = self.Root();
            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;
            request.SceneType = root.SceneType;
            request.SceneName = root.Name;
            request.ActorId = root.GetActorId();
            
            ServiceRegisterResponse response = await self.MessageSender.Call(self.ServiceDiscoveryActorId, request) as ServiceRegisterResponse;

            self = selfRef;
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"Service register failed: {response.Error} {response.Message}");
                return;
            }

            self.HeartbeatTimer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(self.HeartbeatInterval, TimerInvokeType.ServiceDiscoveryProxyHeartbeat, self);
            Log.Debug($"Service registered successfully: {request}");
        }

        /// <summary>
        /// 从服务发现服务器注销
        /// </summary>
        public static async ETTask UnregisterFromServiceDiscovery(this ServiceDiscoveryProxyComponent self)
        {
            Scene root = self.Root();

            ServiceUnregisterRequest request = ServiceUnregisterRequest.Create();
            request.SceneName = root.Name;
            request.SceneType = root.SceneType;
            request.ActorId = root.GetActorId();

            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        private static async ETTask SendHeartbeat(this ServiceDiscoveryProxyComponent self)
        {
            ServiceHeartbeatRequest request = ServiceHeartbeatRequest.Create();
            request.SceneName = self.Root().Name;
            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 查询服务列表
        /// </summary>
        public static async ETTask<List<ServiceInfoProto>> QueryServices(this ServiceDiscoveryProxyComponent self, int sceneType)
        {
            ServiceQueryRequest request = ServiceQueryRequest.Create();
            request.SceneType = sceneType;

            ServiceQueryResponse response = await self.MessageSender.Call(self.ServiceDiscoveryActorId, request) as ServiceQueryResponse;
            return response.Services;
        }

        /// <summary>
        /// 订阅服务变更
        /// </summary>
        public static async ETTask SubscribeServiceChange(this ServiceDiscoveryProxyComponent self, int[] sceneTypes)
        {
            ServiceSubscribeRequest request = ServiceSubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            request.SceneTypes.AddRange(sceneTypes);

            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static async ETTask UnsubscribeServiceChange(this ServiceDiscoveryProxyComponent self, int[] sceneTypes)
        {
            ServiceUnsubscribeRequest request = ServiceUnsubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            request.SceneTypes.AddRange(sceneTypes);
            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        public static void OnServiceChangeNotification(this ServiceDiscoveryProxyComponent self, int changeType, ServiceInfoProto serviceInfo)
        {
            switch (changeType)
            {
                // 添加
                case 1:
                {
                    self.CachedSceneTypeServices.Add(serviceInfo.SceneType, serviceInfo.SceneName);
                    self.CachedSceneNameServices.Add(serviceInfo.SceneName, new ServiceCacheInfo()
                    {
                        SceneName = serviceInfo.SceneName,
                        SceneType = serviceInfo.SceneType,
                        ActorId = serviceInfo.ActorId,
                    });

                    EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeAddService()
                    {
                        SceneType = serviceInfo.SceneType, ServiceName = serviceInfo.SceneName
                    });
                    break;
                }
                // 删除
                case 2:
                {
                    self.CachedSceneTypeServices.Remove(serviceInfo.SceneType, serviceInfo.SceneName);
                    self.CachedSceneNameServices.Remove(serviceInfo.SceneName);

                    EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeRemoveService()
                    {
                        SceneType = serviceInfo.SceneType, ServiceName = serviceInfo.SceneName
                    });
                    break;
                }
            }
        }
        
        /// <summary>
        /// 处理服务变更通知
        /// </summary>
        public static void OnServiceChangeNotification(this ServiceDiscoveryProxyComponent self, int changeType, List<ServiceInfoProto> serviceInfos)
        {
            foreach (ServiceInfoProto serviceInfo in serviceInfos)
            {
                self.OnServiceChangeNotification(changeType, serviceInfo);
            }
        }

        /// <summary>
        /// 获取缓存的服务列表
        /// </summary>
        public static string[] GetCachedServices(this ServiceDiscoveryProxyComponent self, int sceneType)
        {
            return self.CachedSceneTypeServices.GetAll(sceneType);
        }
        
        public static async ETTask<ActorId> GetServiceActorId(this ServiceDiscoveryProxyComponent self, string sceneName)
        {
            if (self.CachedSceneNameServices.TryGetValue(sceneName, out ServiceCacheInfo info))
            {
                return info.ActorId;
            }

            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;
            ServiceQueryBySceneNameRequest request = ServiceQueryBySceneNameRequest.Create();
            request.SceneName = sceneName;
            
            ServiceQueryBySceneNameResponse response = await self.MessageSender.Call(self.ServiceDiscoveryActorId, request) as ServiceQueryBySceneNameResponse;
            
            self = selfRef;
            self.OnServiceChangeNotification(1, response.Services);
            return response.Services.ActorId;
        }
    }
}