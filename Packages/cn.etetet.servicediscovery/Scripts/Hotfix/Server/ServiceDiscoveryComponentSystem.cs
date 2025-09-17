using System.Collections.Generic;
using System.Linq;

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
            self.Services?.Clear();
            self.ServicesByType?.Clear();
            self.Subscribers?.Clear();
            Log.Debug($"ServiceDiscoveryComponent Destroy");
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
        public static bool RegisterService(this ServiceDiscoveryComponent self, string sceneName, int sceneType, ActorId actorId)
        {
            // 检查是否已存在
            if (self.Services.ContainsKey(sceneName))
            {
                Log.Warning($"Service already registered: {sceneName}");
                return false;
            }

            // 创建服务信息
            ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, int, ActorId>(sceneName, sceneType, actorId);
            self.Services[sceneName] = serviceInfo;

            // 添加到按类型分组的字典
            self.ServicesByType[sceneType].Add(sceneName);

            Log.Debug($"Service registered: {sceneType} {sceneName}");

            // 通知订阅者
            self.NotifyServiceChange(sceneType, 1, serviceInfo);

            return true;
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        public static bool UnregisterService(this ServiceDiscoveryComponent self, string sceneName)
        {
            if (!self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                Log.Warning($"Service not found for unregister: {sceneName}");
                return false;
            }

            ServiceInfo serviceInfo = serviceRef;
            if (serviceInfo == null)
            {
                Log.Warning($"Service info is null for unregister: {sceneName}");
                return false;
            }

            int sceneType = serviceInfo.SceneType;

            // 从字典中移除
            self.Services.Remove(sceneName);
            self.ServicesByType.Remove(sceneType, sceneName);

            Log.Debug($"Service unregistered: {sceneType} {sceneName}");

            // 通知订阅者
            self.NotifyServiceChange(sceneType, 2, serviceInfo);

            // 销毁服务信息实体
            serviceInfo.Dispose();

            return true;
        }

        /// <summary>
        /// 更新服务心跳
        /// </summary>
        public static bool UpdateServiceHeartbeat(this ServiceDiscoveryComponent self, string sceneName)
        {
            if (!self.Services.TryGetValue(sceneName, out EntityRef<ServiceInfo> serviceRef))
            {
                Log.Warning($"Service not found for heartbeat: {sceneName}");
                return false;
            }

            ServiceInfo serviceInfo = serviceRef;
            if (serviceInfo == null)
            {
                Log.Warning($"Service info is null for heartbeat: {sceneName}");
                return false;
            }

            serviceInfo.UpdateHeartbeat();
            Log.Debug($"Service heartbeat updated: {sceneName}");
            return true;
        }

        /// <summary>
        /// 查询指定类型的服务列表
        /// </summary>
        public static List<ServiceInfo> QueryServices(this ServiceDiscoveryComponent self, int sceneType)
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
        public static void SubscribeServiceChange(this ServiceDiscoveryComponent self, int sceneType, string sceneName)
        {
            self.Subscribers.Add(sceneType, sceneName);
            Log.Debug($"Subscribe service change: {sceneType} by {sceneName}");
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static void UnsubscribeServiceChange(this ServiceDiscoveryComponent self, int sceneType, string sceneName)
        {
            self.Subscribers.Remove(sceneType, sceneName);
            Log.Debug($"Unsubscribe service change: {sceneType} by {sceneName}");
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
            notification.SceneName = serviceInfo.SceneName;
            notification.SceneType = sceneType;
            notification.ChangeType = changeType;
            notification.ServiceInfo = serviceInfo.ToProto();

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
            self.timeoutServices.Clear();

            foreach (var kvp in self.Services)
            {
                ServiceInfo serviceInfo = kvp.Value;
                if (serviceInfo != null && serviceInfo.IsHeartbeatTimeout(self.HeartbeatTimeout))
                {
                    self.timeoutServices.Add(kvp.Key);
                }
            }

            // 移除超时的服务
            foreach (string sceneName in self.timeoutServices)
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