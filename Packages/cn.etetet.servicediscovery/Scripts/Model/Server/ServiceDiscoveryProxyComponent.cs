using System.Collections.Generic;

namespace ET.Server
{
    [ChildOf(typeof(ServiceDiscoveryProxyComponent))]
    public partial class ServiceCacheInfo: Entity, IAwake
    {
        public string SceneName;
        public int SceneType;
        public ActorId ActorId;
        public Dictionary<string, string> Metadata;
    }
    
    /// <summary>
    /// 服务发现代理组件，挂载在Fiber上提供服务注册和心跳功能
    /// 通过此组件可以将Fiber注册到服务发现服务器上
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceDiscoveryProxyComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 服务发现服务器的ActorId
        /// </summary>
        public ActorId ServiceDiscoveryActorId;

        /// <summary>
        /// 心跳发送间隔（毫秒）
        /// </summary>
        public long HeartbeatInterval = 2 * 1000;

        /// <summary>
        /// 缓存的服务列表，Key为SceneType，Value为该类型的服务SceneName列表
        /// </summary>
        public Dictionary<string, EntityRef<ServiceCacheInfo>> SceneNameServices = new();
        public MultiMap<int, string> SceneTypeServices = new();
        public Dictionary<long, MultiMap<int, string>> ZoneSceneTypeServices = new();

        public long HeartbeatTimer;
        
        private EntityRef<MessageSender> messageSender;

        public MessageSender MessageSender
        {
            get
            {
                return this.messageSender;
            }
            set
            {
                this.messageSender = value;
            }
        }
        
        
        /// <summary>
        /// 待发送的消息队列，按SceneName分组
        /// </summary>
        public Dictionary<string, Queue<IMessage>> PendingMessages = new();
    }
}