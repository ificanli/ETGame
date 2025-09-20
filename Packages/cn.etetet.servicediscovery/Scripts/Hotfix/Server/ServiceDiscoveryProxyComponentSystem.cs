using System;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ServiceCacheInfo))]
    public static partial class ServiceCacheInfoSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceCacheInfo self)
        {
        }

        public static int ServiceZone(this ServiceCacheInfo self)
        {
            return int.Parse(self.Metadata[ServiceMetaKey.Zone]);
        }
    }


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
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(nameof(SceneType.ServiceDiscovery));
            self.ServiceDiscoveryActorId = new ActorId(startSceneConfig.Address, new FiberInstanceId(Const.ServiceDiscoveryFiberId));
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
        /// 注册到服务发现服务器
        /// </summary>
        public static async ETTask RegisterToServiceDiscovery(this ServiceDiscoveryProxyComponent self, Dictionary<string, string> metadata)
        {
            ServiceRegisterRequest request = ServiceRegisterRequest.Create();
            Scene root = self.Root();
            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;
            request.SceneType = root.SceneType;
            request.SceneName = root.Name;
            request.ActorId = root.GetActorId();
            request.Metadata.Add(ServiceMetaKey.Zone, $"{self.Zone()}");
            foreach (var item in metadata)
            {
                request.Metadata.Add(item.Key, item.Value);
            }
            
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
        public static async ETTask SubscribeServiceChange(this ServiceDiscoveryProxyComponent self, int sceneType, Dictionary<string, string> filterMeta)
        {
            ServiceSubscribeRequest request = ServiceSubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            request.SceneType = sceneType;
            foreach (var kv in filterMeta)
            {
                request.FilterMetadata.Add(kv.Key, kv.Value);
            }

            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static async ETTask UnsubscribeServiceChange(this ServiceDiscoveryProxyComponent self, int sceneType)
        {
            ServiceUnsubscribeRequest request = ServiceUnsubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            request.SceneType = sceneType;
            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }
        

        private static void OnServiceChangeNotification(this ServiceDiscoveryProxyComponent self, int changeType, ServiceInfoProto serviceInfo)
        {
            switch (changeType)
            {
                // 添加
                case 1:
                {
                    ServiceCacheInfo serviceCacheInfo = self.AddChild<ServiceCacheInfo>();
                    serviceCacheInfo.SceneName = serviceInfo.SceneName;
                    serviceCacheInfo.SceneType = serviceInfo.SceneType;
                    serviceCacheInfo.ActorId = serviceInfo.ActorId;
                    serviceCacheInfo.Metadata = new Dictionary<string, string>(serviceInfo.Metadata);
                    self.SceneNameServices.Add(serviceInfo.SceneName, serviceCacheInfo);
                    self.SceneTypeServices.Add(serviceInfo.SceneType, serviceInfo.SceneName);
                    MultiMap<int, string> map = null;
                    if (!self.ZoneSceneTypeServices.TryGetValue(serviceCacheInfo.ServiceZone(), out map))
                    {
                        map = new MultiMap<int, string>();
                        self.ZoneSceneTypeServices.Add(serviceCacheInfo.ServiceZone(), map);
                    }
                    map.Add(serviceCacheInfo.SceneType, serviceCacheInfo.SceneName);
                    

                    EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeAddService()
                    {
                        SceneType = serviceInfo.SceneType, ServiceName = serviceInfo.SceneName
                    });
                    break;
                }
                // 删除
                case 2:
                {
                    if (!self.SceneNameServices.TryGetValue(serviceInfo.SceneName, out var serviceCacheInfoRef))
                    {
                        return;
                    }
                    
                    using ServiceCacheInfo serviceCacheInfo = serviceCacheInfoRef;
                    
                    self.SceneTypeServices.Remove(serviceCacheInfo.SceneType, serviceCacheInfo.SceneName);
                    self.SceneNameServices.Remove(serviceCacheInfo.SceneName);
                    if (self.ZoneSceneTypeServices.TryGetValue(serviceCacheInfo.ServiceZone(), out MultiMap<int, string> map))
                    {
                        map.Remove(serviceCacheInfo.SceneType, serviceCacheInfo.SceneName);
                    }

                    if (map.Count == 0)
                    {
                        self.ZoneSceneTypeServices.Remove(serviceCacheInfo.ServiceZone());
                    }
                    
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
        
        #region 获取ServiceInfo
        
        public static async ETTask<ServiceCacheInfo> GetServiceInfo(this ServiceDiscoveryProxyComponent self, string sceneName)
        {
            if (self.SceneNameServices.TryGetValue(sceneName, out var serviceCacheInfoRef))
            {
                return serviceCacheInfoRef;
            }

            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;
            ServiceQueryBySceneNameRequest request = ServiceQueryBySceneNameRequest.Create();
            request.SceneName = sceneName;
            
            ServiceQueryBySceneNameResponse response = await self.MessageSender.Call(self.ServiceDiscoveryActorId, request) as ServiceQueryBySceneNameResponse;
            
            self = selfRef;
            self.OnServiceChangeNotification(1, response.Services);
            self.SceneNameServices.TryGetValue(sceneName, out var serviceCacheInfoRef2);
            return serviceCacheInfoRef2;
        }
        
        public static List<string> GetByZoneSceneType(this ServiceDiscoveryProxyComponent self, int zone, int type)
        {
            return self.ZoneSceneTypeServices[zone][type];
        }
        
        public static List<string> GetBySceneType(this ServiceDiscoveryProxyComponent self, int type)
        {
            try
            {
                return self.SceneTypeServices[type];
            }
            catch (Exception e)
            {
                string sceneName = SceneTypeSingleton.Instance.GetSceneName(type);
                throw new Exception($"{self.Root().Name} not found scene type {sceneName},", e);
            }
        }
        
        public static string GetOneByZoneSceneType(this ServiceDiscoveryProxyComponent self, int zone, int type)
        {
            return self.ZoneSceneTypeServices[zone][type][0];
        }

        public static ServiceCacheInfo GetByName(this ServiceDiscoveryProxyComponent self, string name)
        {
            return self.SceneNameServices[name];
        }
        
        #endregion


        #region 发消息
        
        /// <summary>
        /// 同步发送消息到指定的SceneName
        /// 如果ActorId未知，消息会被加入队列，等待ActorId获取后发送
        /// </summary>
        /// <param name="self">ServiceMessageSender实例</param>
        /// <param name="sceneName">目标服务的SceneName</param>
        /// <param name="message">要发送的消息</param>
        public static void Send(this ServiceDiscoveryProxyComponent self, string sceneName, IMessage message)
        {
            // 获取或创建该SceneName的队列
            if (!self.PendingMessages.TryGetValue(sceneName, out Queue<IMessage> queue))
            {
                queue = new Queue<IMessage>();
                self.PendingMessages[sceneName] = queue;
            }

            queue.Enqueue(message);

            // 开始获取ActorId（如果还未开始）
            if (queue.Count == 1)
            {
                self.StartFetchActorId(sceneName).NoContext();
            }
        }

        /// <summary>
        /// 发送请求消息到指定的SceneName并等待响应
        /// 如果ActorId未知，会先异步获取ActorId再发送请求
        /// </summary>
        /// <param name="self">ServiceMessageSender实例</param>
        /// <param name="sceneName">目标服务的SceneName</param>
        /// <param name="request">要发送的请求消息</param>
        /// <param name="needException">是否需要抛出异常</param>
        /// <returns>响应消息</returns>
        public static async ETTask<IResponse> Call(this ServiceDiscoveryProxyComponent self, string sceneName, IRequest request, bool needException = true)
        {
            // ActorId未知，先获取ActorId
            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;

            ServiceCacheInfo serviceCacheInfo = await self.GetServiceInfo(sceneName);
            if (serviceCacheInfo.ActorId == default)
            {
                throw new System.Exception($"Failed to get ActorId for scene: {sceneName}");
            }

            self = selfRef;
            return await self.MessageSender.Call(serviceCacheInfo.ActorId, request, needException);
        }

        /// <summary>
        /// 开始异步获取ActorId
        /// </summary>
        private static async ETTask StartFetchActorId(this ServiceDiscoveryProxyComponent self, string sceneName)
        {
            EntityRef<ServiceDiscoveryProxyComponent> selfRef = self;

            ServiceCacheInfo serviceCacheInfo = await self.GetServiceInfo(sceneName);

            self = selfRef;
            if (self == null)
            {
                return;
            }

            if (serviceCacheInfo.ActorId == default)
            {
                throw new System.Exception($"Failed to get ActorId for scene: {sceneName}");
            }

            // 处理待发送的消息
            self.ProcessPendingMessages(sceneName, serviceCacheInfo.ActorId);
        }

        /// <summary>
        /// 处理待发送的消息
        /// </summary>
        private static void ProcessPendingMessages(this ServiceDiscoveryProxyComponent self, string sceneName, ActorId actorId)
        {
            if (!self.PendingMessages.TryGetValue(sceneName, out Queue<IMessage> queue))
            {
                return;
            }

            MessageSender messageSender = self.MessageSender;

            while (queue.Count > 0)
            {
                IMessage pendingMessage = queue.Dequeue();

                try
                {
                    // 只处理Send消息，Call消息不会进入队列
                    messageSender.Send(actorId, pendingMessage);
                }
                catch (System.Exception e)
                {
                    Log.Error($"Error processing pending message for scene {sceneName}: {e}");
                }
            }
        }
        #endregion
    }
}