using System;
using System.IO;

namespace ET
{
    // 知道对方的instanceId，使用这个类发actor消息
    public readonly struct ProcessInnerMessageSenderStruct
    {
        public FiberInstanceId FiberInstanceId { get; }
        
        public Type RequestType { get; }
        
        private readonly ETTask<IResponse> tcs;

        public bool NeedException { get; }
        
        public ProcessInnerMessageSenderStruct(FiberInstanceId fiberInstanceId, Type requestType, bool needException)
        {
            this.FiberInstanceId = fiberInstanceId;
            
            this.RequestType = requestType;
            
            this.tcs = ETTask<IResponse>.Create(true);
            this.NeedException = needException;
        }
        
        public void SetResult(IResponse response)
        {
            this.tcs.SetResult(response);
        }
        
        public void SetException(Exception exception)
        {
            this.tcs.SetException(exception);
        }

        public async ETTask<IResponse> Wait()
        {
            return await this.tcs;
        }
    }
}