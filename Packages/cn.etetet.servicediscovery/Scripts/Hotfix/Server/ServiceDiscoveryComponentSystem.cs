using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现组件系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscoveryComponent))]
    public static partial class ServiceDiscoveryComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceDiscoveryComponent self)
        {
            self.LastHeartbeatCheckTime = TimeInfo.Instance.ServerNow();

            Log.Debug($"ServiceDiscoveryComponent Awake");
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscoveryComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this ServiceDiscoveryComponent self)
        {
            long now = TimeInfo.Instance.ServerNow();
            if (now - self.LastHeartbeatCheckTime >= self.HeartbeatCheckInterval)
            {
                self.CheckHeartbeatTimeout();
                self.LastHeartbeatCheckTime = now;
            }
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        public static void RegisterService(this ServiceDiscoveryComponent self, string sceneName, int sceneType, ActorId actorId)
        {
            // 已存在则注销之前的
            if (self.Services.ContainsKey(sceneName))
            {
                self.UnregisterService(sceneName);
            }

            // 创建服务信息
            ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, int, ActorId>(sceneName, sceneType, actorId);
            self.Services[sceneName] = serviceInfo;

            // 添加到按类型分组的字典
            self.ServicesByType.Add(sceneType, sceneName);

            Log.Debug($"Service registered: {sceneType} {sceneName}");

            // 通知订阅者
            self.NotifyServiceChange(sceneType, 1, serviceInfo);
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        public static void UnregisterService(this ServiceDiscoveryComponent self, string sceneName)
        {
            if (!self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                Log.Warning($"Service not found for unregister: {sceneName}");
                return;
            }

            ServiceInfo serviceInfo = serviceRef;
            if (serviceInfo == null)
            {
                Log.Warning($"Service info is null for unregister: {sceneName}");
                return;
            }

            int sceneType = serviceInfo.SceneType;

            // 从字典中移除
            self.Services.Remove(sceneName);
            self.ServicesByType.Remove(sceneType, sceneName);
            foreach (int subSceneType in serviceInfo.SubSceneTypes)
            {
                self.Subscribers.Remove(subSceneType, sceneName);
            }
            
            Log.Debug($"Service unregistered: {sceneType} {sceneName}");

            // 通知订阅者
            self.NotifyServiceChange(sceneType, 2, serviceInfo);

            // 销毁服务信息实体
            serviceInfo.Dispose();
        }

        /// <summary>
        /// 更新服务心跳
        /// </summary>
        public static void UpdateServiceHeartbeat(this ServiceDiscoveryComponent self, string sceneName)
        {
            if (!self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                Log.Warning($"Service not found for heartbeat: {sceneName}");
                return;
            }

            ServiceInfo serviceInfo = serviceRef;
            if (serviceInfo == null)
            {
                Log.Warning($"Service info is null for heartbeat: {sceneName}");
                return;
            }

            serviceInfo.UpdateHeartbeat();
            Log.Debug($"Service heartbeat updated: {sceneName}");
        }

        /// <summary>
        /// 查询指定类型的服务列表
        /// </summary>
        public static List<ServiceInfo> GetServicesBySceneType(this ServiceDiscoveryComponent self, int sceneType)
        {
            List<ServiceInfo> result = new();
            if (!self.ServicesByType.TryGetValue(sceneType, out HashSet<string> serviceKeys))
            {
                return result;
            }

            foreach (string serviceKey in serviceKeys)
            {
                if (self.Services.TryGetValue(serviceKey, out EntityRef<ServiceInfo> serviceRef))
                {
                    ServiceInfo serviceInfo = serviceRef;
                    if (serviceInfo != null)
                    {
                        result.Add(serviceInfo);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 订阅服务变更
        /// </summary>
        public static void SubscribeServiceChange(this ServiceDiscoveryComponent self, string sceneName, List<int> sceneTypes)
        {
            ServiceInfo serviceInfo = self.Services[sceneName];
            
            ServiceChangeNotification notification = ServiceChangeNotification.Create();
            notification.ChangeType = 1;
            
            foreach (int sceneType in sceneTypes)
            {
                if (!serviceInfo.SubSceneTypes.Add(sceneType))
                {
                    continue;
                }
                
                self.Subscribers.Add(sceneType, sceneName);
                
                List<ServiceInfo> serviceInfos = self.GetServicesBySceneType(sceneType);
                foreach (ServiceInfo sInfo in serviceInfos)
                {
                    notification.ServiceInfo.Add(sInfo.ToProto());
                }
            }
            
            self.Root().GetComponent<MessageSender>().Send(serviceInfo.ActorId, notification);

            Log.Debug($"Subscribe service change: {sceneName} {sceneTypes.ListToString()}");
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static void UnsubscribeServiceChange(this ServiceDiscoveryComponent self, string sceneName, List<int> sceneTypes)
        {
            ServiceInfo serviceInfo = self.Services[sceneName];
            foreach (int sceneType in sceneTypes)
            {
                self.Subscribers.Remove(sceneType, sceneName);
                serviceInfo.SubSceneTypes.Remove(sceneType);
            }
            Log.Debug($"Unsubscribe service change: {sceneName} {sceneTypes.ListToString()}");
        }

        /// <summary>
        /// 通知服务变更
        /// </summary>
        private static void NotifyServiceChange(this ServiceDiscoveryComponent self, int sceneType, int changeType, ServiceInfo serviceInfo)
        {
            if (!self.Subscribers.TryGetValue(sceneType, out HashSet<string> subscribers))
            {
                return;
            }

            ServiceChangeNotification notification = ServiceChangeNotification.Create();
            notification.ChangeType = changeType;
            notification.ServiceInfo.Add(serviceInfo.ToProto());

            // 给订阅者广播
            foreach (string sceneName in subscribers)
            {
                self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef);
                ServiceInfo subServiceInfo = serviceRef;
                self.Root().GetComponent<MessageSender>().Send(subServiceInfo.ActorId, notification);
            }

            Log.Debug($"Notified service change: {sceneType} changeType={changeType} to {subscribers.Count} subscribers");
        }

        /// <summary>
        /// 检查心跳超时
        /// </summary>
        private static void CheckHeartbeatTimeout(this ServiceDiscoveryComponent self)
        {
            using ListComponent<string> list = ListComponent<string>.Create();

            foreach (var kvp in self.Services)
            {
                ServiceInfo serviceInfo = kvp.Value;
                if (serviceInfo != null && serviceInfo.IsHeartbeatTimeout(self.HeartbeatTimeout))
                {
                    list.Add(kvp.Key);
                }
            }

            // 移除超时的服务
            foreach (string sceneName in list)
            {
                if (!self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
                {
                    continue;
                }

                ServiceInfo serviceInfo = serviceRef;
                if (serviceInfo == null)
                {
                    continue;
                }

                Log.Warning($"Service heartbeat timeout, removing: {sceneName} {serviceInfo.SceneType} ");
                self.UnregisterService(sceneName);
            }
        }
    }
}