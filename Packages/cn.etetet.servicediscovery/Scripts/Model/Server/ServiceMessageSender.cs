using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务消息发送器组件，用于通过SceneName发送消息
    /// 自动获取目标服务的ActorId并通过MessageSender发送消息
    /// 支持同步调用，内部通过队列处理异步ActorId获取
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ServiceMessageSender : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 待发送的消息队列，按SceneName分组
        /// </summary>
        public Dictionary<string, Queue<IMessage>> PendingMessages = new();
        
        private EntityRef<ServiceDiscoveryProxyComponent> serviceDiscoveryProxy;

        public ServiceDiscoveryProxyComponent ServiceDiscoveryProxy
        {
            get
            {
                return this.serviceDiscoveryProxy;
            }
            set
            {
                this.serviceDiscoveryProxy = value;
            }
        }
    }
}