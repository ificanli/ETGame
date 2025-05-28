using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ET
{
    public struct MessageInfo
    {
        public ActorId ActorId;
        public MessageObject MessageObject;
    }
    
    public class MessageQueue: Singleton<MessageQueue>, ISingletonAwake
    {
        private readonly ConcurrentDictionary<int, ConcurrentQueue<MessageInfo>> messages = new();
        
        public void Awake()
        {
        }

        public bool Send(Fiber fiber, ActorId actorId, MessageObject messageObject)
        {
            LogMsg.Instance.Send(fiber, messageObject);
            if (!this.messages.TryGetValue(actorId.Address.Fiber, out var queue))
            {
                return false;
            }
            queue.Enqueue(new MessageInfo() {ActorId = new ActorId(fiber.Address, actorId.InstanceId), MessageObject = messageObject});
            return true;
        }
        
        public void Fetch(Fiber fiber, int count, List<MessageInfo> list)
        {
            if (!this.messages.TryGetValue(fiber.Id, out var queue))
            {
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                if (!queue.TryDequeue(out MessageInfo message))
                {
                    break;
                }
                LogMsg.Instance.Recv(fiber, message.MessageObject);
                list.Add(message);
            }
        }

        public void AddQueue(int fiberId)
        {
            var queue = new ConcurrentQueue<MessageInfo>();
            this.messages[fiberId] = queue;
        }
        
        public void RemoveQueue(int fiberId)
        {
            this.messages.TryRemove(fiberId, out _);
        }
    }
}