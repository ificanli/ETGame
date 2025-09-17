using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务发现代理组件，挂载在Fiber上提供服务注册和心跳功能
    /// 通过此组件可以将Fiber注册到服务发现服务器上
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceDiscoveryProxyComponent : Entity, IAwake, IUpdate
    {
        /// <summary>
        /// 服务发现服务器的ActorId
        /// </summary>
        public ActorId ServiceDiscoveryActorId;

        /// <summary>
        /// 是否已注册
        /// </summary>
        public bool IsRegistered;

        /// <summary>
        /// 心跳发送间隔（毫秒）
        /// </summary>
        public long HeartbeatInterval = 10 * 1000;

        /// <summary>
        /// 上次心跳发送时间
        /// </summary>
        public long LastHeartbeatTime;

        /// <summary>
        /// 订阅的SceneType列表
        /// </summary>
        public HashSet<int> SubscribedSceneTypes = new();

        /// <summary>
        /// 缓存的服务列表，Key为SceneType，Value为该类型的服务SceneName列表
        /// </summary>
        public MultiMap<int, string> CachedServices = new();
    }
}