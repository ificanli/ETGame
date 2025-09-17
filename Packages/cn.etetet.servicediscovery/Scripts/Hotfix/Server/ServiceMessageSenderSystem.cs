using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务消息发送器组件系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceMessageSender))]
    public static partial class ServiceMessageSenderSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceMessageSender self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ServiceMessageSender self)
        {
        }

        /// <summary>
        /// 同步发送消息到指定的SceneName
        /// 如果ActorId未知，消息会被加入队列，等待ActorId获取后发送
        /// </summary>
        /// <param name="self">ServiceMessageSender实例</param>
        /// <param name="sceneName">目标服务的SceneName</param>
        /// <param name="message">要发送的消息</param>
        public static void Send(this ServiceMessageSender self, string sceneName, IMessage message)
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
        public static async ETTask<IResponse> Call(this ServiceMessageSender self, string sceneName, IRequest request, bool needException = true)
        {
            // ActorId未知，先获取ActorId
            Scene root = self.Root();
            EntityRef<Scene> rootRef = root;
            ServiceDiscoveryProxyComponent serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxyComponent>();

            ActorId actorId = await serviceDiscoveryProxy.GetServiceActorId(sceneName);
            if (actorId == default)
            {
                throw new System.Exception($"Failed to get ActorId for scene: {sceneName}");
            }

            root = rootRef;
            MessageSender messageSender = root.GetComponent<MessageSender>();

            return await messageSender.Call(actorId, request, needException);
        }

        /// <summary>
        /// 开始异步获取ActorId
        /// </summary>
        private static async ETTask StartFetchActorId(this ServiceMessageSender self, string sceneName)
        {
            Scene root = self.Root();
            ServiceDiscoveryProxyComponent serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxyComponent>();

            EntityRef<ServiceMessageSender> selfRef = self;

            ActorId actorId = await serviceDiscoveryProxy.GetServiceActorId(sceneName);

            self = selfRef;
            if (self == null)
            {
                return;
            }

            if (actorId == default)
            {
                throw new System.Exception($"Failed to get ActorId for scene: {sceneName}");
            }

            // 处理待发送的消息
            self.ProcessPendingMessages(sceneName, actorId);
        }

        /// <summary>
        /// 处理待发送的消息
        /// </summary>
        private static void ProcessPendingMessages(this ServiceMessageSender self, string sceneName, ActorId actorId)
        {
            if (!self.PendingMessages.TryGetValue(sceneName, out Queue<IMessage> queue))
            {
                return;
            }

            MessageSender messageSender = self.Root().GetComponent<MessageSender>();

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
    }
}