using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 服务发现代理组件系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryProxy))]
    public static partial class ServiceDiscoveryProxySystem
    {
        [Event(SceneType.All)]
        public class FiberDestroyEvent_UnRegisterService: AEvent<Scene, FiberDestroyEvent>
        {
            protected override async ETTask Run(Scene scene, FiberDestroyEvent fiberDestroyEvent)
            {
                ServiceDiscoveryProxy serviceDiscoveryProxy = scene.GetComponent<ServiceDiscoveryProxy>();
                await serviceDiscoveryProxy.DestroyAsync();
            }
        }
        
        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryProxy self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            root.GetComponent<TimerComponent>().Remove(ref self.HeartbeatTimer);
        }
        
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryProxy self)
        {
            self.MessageSender = self.Root().GetComponent<MessageSender>();
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(nameof(SceneType.ServiceDiscovery));
            self.ServiceDiscoveryActorId = new ActorId(startSceneConfig.Address, new FiberInstanceId(Const.ServiceDiscoveryFiberId));
        }

        [Invoke(TimerInvokeType.ServiceDiscoveryProxyHeartbeat)]
        public class ServiceDiscoveryProxyHeartbeat : ATimer<ServiceDiscoveryProxy>
        {
            protected override void Run(ServiceDiscoveryProxy self)
            {
                self.SendHeartbeat().NoContext();
            }
        }

        private static async ETTask DestroyAsync(this ServiceDiscoveryProxy self)
        {
            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            await self.UnsubscribeServiceChange();
            
            self = selfRef;
            await self.UnregisterFromServiceDiscovery();
        }

        /// <summary>
        /// 注册到服务发现服务器
        /// </summary>
        public static async ETTask RegisterToServiceDiscovery(this ServiceDiscoveryProxy self, Dictionary<string, string> metadata)
        {
            ServiceRegisterRequest request = ServiceRegisterRequest.Create();
            Scene root = self.Root();
            EntityRef<ServiceDiscoveryProxy> selfRef = self;
            request.SceneName = root.Name;
            request.ActorId = root.GetActorId();
            request.Metadata.Add(ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(self.Scene().SceneType));
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
        public static async ETTask UnregisterFromServiceDiscovery(this ServiceDiscoveryProxy self)
        {
            Scene root = self.Root();

            ServiceUnregisterRequest request = ServiceUnregisterRequest.Create();
            request.SceneName = root.Name;
            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        private static async ETTask SendHeartbeat(this ServiceDiscoveryProxy self)
        {
            ServiceHeartbeatRequest request = ServiceHeartbeatRequest.Create();
            request.SceneName = self.Root().Name;
            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 订阅服务变更
        /// </summary>
        public static async ETTask SubscribeServiceChange(this ServiceDiscoveryProxy self, string filterName, Dictionary<string, string> filterMeta)
        {
            ServiceSubscribeRequest request = ServiceSubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            request.FilterName = filterName;

            foreach (var kv in filterMeta)
            {
                request.FilterMetadata.Add(kv.Key, kv.Value);
            }

            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static async ETTask UnsubscribeServiceChange(this ServiceDiscoveryProxy self)
        {
            ServiceUnsubscribeRequest request = ServiceUnsubscribeRequest.Create();
            request.SceneName = self.Root().Name;
            await self.MessageSender.Call(self.ServiceDiscoveryActorId, request);
        }
        

        private static void OnServiceChangeNotification(this ServiceDiscoveryProxy self, int changeType, ServiceInfoProto serviceInfoProto)
        {
            switch (changeType)
            {
                // 添加
                case 1:
                {
                    ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, ActorId>(serviceInfoProto.SceneName, serviceInfoProto.ActorId);
                    serviceInfo.Metadata = new Dictionary<string, string>(serviceInfoProto.Metadata);
                    self.SceneNameServices.Add(serviceInfo.SceneName, serviceInfo);

                    foreach ((string key, var value) in serviceInfo.Metadata)
                    {
                        if (!self.Indexs.Contains(key))
                        {
                            continue;
                        }

                        if (!self.ServicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index))
                        {
                            index = new MultiMapSet<string, string>();
                            self.ServicesIndexs.Add(key, index);
                        }
                        index.Add(value, serviceInfo.SceneName);
                    }

                    EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeAddService()
                    {
                        ServiceName = serviceInfo.SceneName
                    });
                    break;
                }
                // 删除
                case 2:
                {
                    if (!self.SceneNameServices.TryGetValue(serviceInfoProto.SceneName, out var serviceInfoRef))
                    {
                        return;
                    }
                    
                    using ServiceInfo serviceInfo = serviceInfoRef;
                    
                    self.SceneNameServices.Remove(serviceInfo.SceneName);

                    foreach ((string key, string value) in serviceInfo.Metadata)
                    {
                        if (!self.Indexs.Contains(key))
                        {
                            continue;
                        }

                        self.ServicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index2);
                        {
                            index2.Remove(value, serviceInfo.SceneName);
                        }
                    }
                    
                    
                    EventSystem.Instance.Publish(self.Scene(), new OnServiceChangeRemoveService()
                    {
                        ServiceName = serviceInfo.SceneName
                    });
                    break;
                }
            }
        }
        
        /// <summary>
        /// 处理服务变更通知
        /// </summary>
        public static void OnServiceChangeNotification(this ServiceDiscoveryProxy self, int changeType, List<ServiceInfoProto> serviceInfos)
        {
            foreach (ServiceInfoProto serviceInfo in serviceInfos)
            {
                self.OnServiceChangeNotification(changeType, serviceInfo);
            }
        }
        
        #region 获取ServiceInfo
        
        public static ServiceInfo GetServiceInfo(this ServiceDiscoveryProxy self, string sceneName)
        {
            if (!self.SceneNameServices.TryGetValue(sceneName, out var serviceInfoRef))
            {
                throw new Exception("not found scene name: " + sceneName);
            }
            return serviceInfoRef;
        }

        public static List<ServiceInfo> GetBySceneTypeAndZone(this ServiceDiscoveryProxy self, int sceneType, int zone)
        {
            return self.GetServiceInfoByFilter(new Dictionary<string, string>()
            {
                {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(sceneType)},
                {ServiceMetaKey.Zone, zone.ToString()},
            });
        }
        
        public static List<ServiceInfo> GetBySceneType(this ServiceDiscoveryProxy self, int sceneType)
        {
            return self.GetServiceInfoByFilter(new Dictionary<string, string>()
            {
                {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(sceneType)},
            });
        }
        
        /// <summary>
        /// 查询服务
        /// </summary>
        public static List<ServiceInfo> GetServiceInfoByFilter(this ServiceDiscoveryProxy self, Dictionary<string, string> filterMetadata)
        {
            return ServiceDiscoveryHelper.GetServiceInfoByFilter(self.SceneNameServices, self.ServicesIndexs, filterMetadata);
        }

        public static ServiceInfo GetByName(this ServiceDiscoveryProxy self, string name)
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
        public static void Send(this ServiceDiscoveryProxy self, string sceneName, IMessage message)
        {
            ServiceInfo serviceInfo = self.GetServiceInfo(sceneName);
            if (serviceInfo.ActorId == default)
            {
                throw new Exception($"Failed to get ActorId for scene: {sceneName}");
            }
            self.MessageSender.Send(serviceInfo.ActorId, message);
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
        public static async ETTask<IResponse> Call(this ServiceDiscoveryProxy self, string sceneName, IRequest request, bool needException = true)
        {
            // ActorId未知，先获取ActorId
            EntityRef<ServiceDiscoveryProxy> selfRef = self;

            ServiceInfo serviceInfo = self.GetServiceInfo(sceneName);
            if (serviceInfo.ActorId == default)
            {
                throw new Exception($"Failed to get ActorId for scene: {sceneName}");
            }

            self = selfRef;
            return await self.MessageSender.Call(serviceInfo.ActorId, request, needException);
        }
        #endregion
    }
}