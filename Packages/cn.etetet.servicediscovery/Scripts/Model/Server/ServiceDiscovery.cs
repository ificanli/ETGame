using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现组件，挂载到Fiber上成为服务发现Fiber
    /// 管理所有注册的服务信息和订阅者
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceDiscovery : Entity, IAwake, IDestroy, IUpdate
    {
        // 这里可以指定索引
        public readonly string[] Indexs = { ServiceMetaKey.SceneType, ServiceMetaKey.Zone };

        /// <summary>
        /// 所有注册的服务信息，Key为服务的唯一标识SceneName
        /// </summary>
        public Dictionary<string, EntityRef<ServiceInfo>> Services = new();

        //  索引,key1例如"SceneType",  key2 是值，例如SceneType的值，  HashSet中是ServiceInfo的SceneName
        public Dictionary<string, MultiMapSet<string, string>> ServicesIndexs = new();
        

        /// <summary>
        /// 订阅者信息，Key为SceneName，value是订阅的过滤条件,可以多个过滤条件
        /// </summary>
        public MultiDictionary<string, string, Dictionary<string, string>> Subscribers = new();

        /// <summary>
        /// 心跳超时时间（毫秒）
        /// </summary>
        public long HeartbeatTimeout = 30 * 1000;

        /// <summary>
        /// 心跳检查间隔（毫秒）
        /// </summary>
        public long HeartbeatCheckInterval = 5 * 1000;

        /// <summary>
        /// 上次心跳检查时间
        /// </summary>
        public long LastHeartbeatCheckTime;
    }
}