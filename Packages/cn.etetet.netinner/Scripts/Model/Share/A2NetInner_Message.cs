using MemoryPack;

namespace ET
{
    [Message(1)]
    public class A2NetInner_Message: MessageObject, IMessage, IMessageWrapper
    {
        public static A2NetInner_Message Create()
        {
            return ObjectPool.Fetch(typeof(A2NetInner_Message)) as A2NetInner_Message;
        }

        public override void Dispose()
        {
            this.FromFiber = 0;
            this.ActorId = default;
            
            ObjectPool.Recycle(this);
        }
        
        public int FromFiber;
        public ActorId ActorId;
        public IMessage MessageObject;
        
        public IMessage GetMessageObject()
        {
            return MessageObject;
        }
    }
    
    [Message(2)]
    [ResponseType(nameof(A2NetInner_Response))]
    public class A2NetInner_Request: MessageObject, IRequest, IMessageWrapper
    {
        public static A2NetInner_Request Create()
        {
            return ObjectPool.Fetch(typeof(A2NetInner_Request)) as A2NetInner_Request;
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.ActorId = default;
            this.MessageObject = default;
            
            ObjectPool.Recycle(this);
        }
        
        public int RpcId { get; set; }
        public ActorId ActorId;
        public IRequest MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
    
    [Message(3)]
    public class A2NetInner_Response: MessageObject, IResponse, IMessageWrapper
    {
        public static A2NetInner_Response Create()
        {
            return ObjectPool.Fetch(typeof(A2NetInner_Response)) as A2NetInner_Response;
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            
            ObjectPool.Recycle(this);
        }
        
        public int Error { get; set; }
        public string Message { get; set; }
        public int RpcId { get; set; }
        
        public IResponse MessageObject;
        
        public IMessage GetMessageObject()
        {
            return this.MessageObject;
        }
    }
}