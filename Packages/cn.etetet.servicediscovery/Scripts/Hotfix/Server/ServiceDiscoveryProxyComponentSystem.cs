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
        private static void Awake(this ServiceDiscoveryProxyComponent self)
        {
            self.LastHeartbeatTime = TimeInfo.Instance.ServerNow();

            Log.Debug($"ServiceDiscoveryProxyComponent Awake");
        }

        [EntitySystem]
        private static void Update(this ServiceDiscoveryProxyComponent self)
        {
            if (!self.IsRegistered)
            {
                return;
            }

            long now = TimeInfo.Instance.ServerNow();
            if (now - self.LastHeartbeatTime >= self.HeartbeatInterval)
            {
                self.SendHeartbeat().NoContext();
                self.LastHeartbeatTime = now;
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
            request.SceneType = root.SceneType;
            request.SceneName = root.Name;
            request.ActorId = root.GetActorId();

            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;

            ServiceRegisterResponse response = await self.Root().GetComponent<MessageSender>().Call(self.ServiceDiscoveryActorId, request) as ServiceRegisterResponse;

            self = selfRef;
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"Service register failed: {response.Error} {response.Message}");
                return;
            }

            self.IsRegistered = true;
            self.LastHeartbeatTime = TimeInfo.Instance.ServerNow();
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

            await self.Root().GetComponent<MessageSender>().Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        public static async ETTask SendHeartbeat(this ServiceDiscoveryProxyComponent self)
        {
            ServiceHeartbeatRequest request = ServiceHeartbeatRequest.Create();
            request.SceneName = self.Root().Name;
            await self.Root().GetComponent<MessageSender>().Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 查询服务列表
        /// </summary>
        public static async ETTask<List<ServiceInfoProto>> QueryServices(this ServiceDiscoveryProxyComponent self, int sceneType)
        {
            ServiceQueryRequest request = ServiceQueryRequest.Create();
            request.SceneType = sceneType;

            ServiceQueryResponse response = await self.Root().GetComponent<MessageSender>().Call(self.ServiceDiscoveryActorId, request) as ServiceQueryResponse;
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

            await self.Root().GetComponent<MessageSender>().Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static async ETTask UnsubscribeServiceChange(this ServiceDiscoveryProxyComponent self, int[] sceneTypes)
        {
            ServiceUnsubscribeRequest request = ServiceUnsubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            request.SceneTypes.AddRange(sceneTypes);
            await self.Root().GetComponent<MessageSender>().Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 处理服务变更通知
        /// </summary>
        public static void OnServiceChangeNotification(this ServiceDiscoveryProxyComponent self, ServiceChangeNotification notification)
        {
            if (notification.ChangeType == 1) // 添加
            {
                self.CachedServices.Add(notification.SceneType, notification.SceneName);
            }
            else if (notification.ChangeType == 2) // 删除
            {
                self.CachedServices.Remove(notification.SceneType, notification.SceneName);
            }
        }

        /// <summary>
        /// 获取缓存的服务列表
        /// </summary>
        public static string[] GetCachedServices(this ServiceDiscoveryProxyComponent self, int sceneType)
        {
            return self.CachedServices.GetAll(sceneType);
        }
    }
}