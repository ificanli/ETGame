using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现组件，挂载到Fiber上成为服务发现Fiber
    /// 管理所有注册的服务信息和订阅者
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceDiscoveryComponent : Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 所有注册的服务信息，Key为服务的唯一标识SceneName
        /// </summary>
        public Dictionary<string, EntityRef<ServiceInfo>> Services = new();

        /// <summary>
        /// 按SceneType分组的服务列表，Key为SceneType，Value为SceneName
        /// </summary>
        public MultiMapSet<int, string> ServicesByType = new();

        /// <summary>
        /// 订阅者信息，Key为SceneType，Value为订阅该类型的SceneName集合
        /// </summary>
        public MultiMapSet<int, string> Subscribers = new();

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