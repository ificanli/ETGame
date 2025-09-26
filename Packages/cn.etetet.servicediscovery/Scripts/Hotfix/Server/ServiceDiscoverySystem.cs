using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 服务发现组件系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceDiscovery))]
    public static partial class ServiceDiscoverySystem
    {
        [EntitySystem]
        private static void Awake(this ServiceDiscovery self)
        {
            self.LastHeartbeatCheckTime = TimeInfo.Instance.ServerNow();

            Log.Debug($"ServiceDiscoveryComponent Awake");
        }

        [EntitySystem]
        private static void Destroy(this ServiceDiscovery self)
        {
        }

        [EntitySystem]
        private static void Update(this ServiceDiscovery self)
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
        public static void RegisterService(this ServiceDiscovery self, string sceneName, ActorId actorId, Dictionary<string, string> metadata)
        {
            // 已存在则注销之前的
            if (self.Services.ContainsKey(sceneName))
            {
                self.UnregisterService(sceneName);
            }

            // 创建服务信息
            ServiceInfo serviceInfo = self.AddChild<ServiceInfo, string, ActorId>(sceneName, actorId);
            
            // 设置元数据,根据Indexs创建索引
            foreach ((string key, string value) in metadata)
            {
                serviceInfo.Metadata[key] = value;
                
                if (!self.Indexs.Contains(key))
                {
                    continue;
                }

                if (!self.ServicesIndexs.TryGetValue(key, out MultiMapSet<string, string> index))
                {
                    index = new MultiMapSet<string, string>();
                    self.ServicesIndexs.Add(key, index);
                }
                index.Add(value, sceneName);
            }
            
            self.Services[sceneName] = serviceInfo;

            Log.Debug($"Service registered: {serviceInfo}");

            // 通知订阅者
            self.NotifyServiceChange(1, serviceInfo);
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        public static void UnregisterService(this ServiceDiscovery self, string sceneName)
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

            // 从字典中移除
            self.Services.Remove(sceneName);

            // 从索引中移除
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
            
            Log.Debug($"Service unregistered: {sceneName}");

            // 通知订阅者
            self.NotifyServiceChange(2, serviceInfo);

            // 销毁服务信息实体
            serviceInfo.Dispose();
        }

        /// <summary>
        /// 更新服务心跳
        /// </summary>
        public static void UpdateServiceHeartbeat(this ServiceDiscovery self, string sceneName)
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
        }
        
        /// <summary>
        /// 查询服务
        /// </summary>
        public static List<ServiceInfo> GetServiceInfoByFilter(this ServiceDiscovery self, Dictionary<string, string> filterMetadata)
        {
            return ServiceDiscoveryHelper.GetServiceInfoByFilter(self.Services, self.ServicesIndexs, filterMetadata);
        }
        

        /// <summary>
        /// 订阅服务变更
        /// </summary>
        public static void SubscribeServiceChange(this ServiceDiscovery self, string sceneName, string filterName, Dictionary<string, string> filterMetadata)
        {
            ServiceInfo serviceInfo = self.Services[sceneName];

            ServiceChangeNotification notification = ServiceChangeNotification.Create();
            notification.ChangeType = 1;

            // 存储过滤条件
            self.Subscribers.Add(sceneName, filterName, filterMetadata);

            // 使用优化的服务查询方法
            List<ServiceInfo> matchedServices = self.GetServiceInfoByFilter(filterMetadata);
            foreach (ServiceInfo sInfo in matchedServices)
            {
                notification.ServiceInfo.Add(sInfo.ToProto());
            }

            if (notification.ServiceInfo.Count > 0)
            {
                self.Root().GetComponent<MessageSender>().Send(serviceInfo.ActorId, notification);
            }

            string filterStr = filterMetadata.Count > 0? $" filter=[{string.Join(", ", filterMetadata.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}]" : "";
            Log.Debug($"Subscribe service change: {sceneName} {filterStr}");
        }

        /// <summary>
        /// 取消订阅服务变更
        /// </summary>
        public static void UnsubscribeServiceChange(this ServiceDiscovery self, string sceneName)
        {
            self.Subscribers.Remove(sceneName);
            
            Log.Debug($"Unsubscribe service change: {sceneName}");
        }

        /// <summary>
        /// 通知服务变更
        /// </summary>
        private static void NotifyServiceChange(this ServiceDiscovery self, int changeType, ServiceInfo serviceInfo)
        {
            int notifiedCount = 0;

            // 给订阅者广播, 遍历订阅者
            foreach (var kv in self.Subscribers)
            {
                foreach (var kv2 in kv.Value) // 一个订阅者有多个订阅
                {
                    string subSceneName = kv.Key;
                    Dictionary<string, string> filter = kv2.Value;
                    if (!self.Services.TryGetValue(subSceneName, out EntityRef<ServiceInfo> subServiceInfoRef))
                    {
                        // 订阅者不存在
                        continue;
                    }

                    // 应用过滤条件
                    if (!serviceInfo.MatchesFilter(filter))
                    {
                        continue;
                    }

                    // 创建通知消息
                    ServiceChangeNotification notification = ServiceChangeNotification.Create();
                    notification.ChangeType = changeType;
                    notification.ServiceInfo.Add(serviceInfo.ToProto());

                    ServiceInfo subServiceInfo = subServiceInfoRef;
                    self.Root().GetComponent<MessageSender>().Send(subServiceInfo.ActorId, notification);
                    notifiedCount++;
                    
                    // 只要满足一个就不用继续了
                    break;
                }
            }

            Log.Debug($"Notified service change: changeType={changeType} to {notifiedCount} subscribers (filtered from)");
        }

        /// <summary>
        /// 检查心跳超时
        /// </summary>
        private static void CheckHeartbeatTimeout(this ServiceDiscovery self)
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

                Log.Warning($"Service heartbeat timeout, removing: {sceneName}");
                self.UnregisterService(sceneName);
            }
        }
    }
}