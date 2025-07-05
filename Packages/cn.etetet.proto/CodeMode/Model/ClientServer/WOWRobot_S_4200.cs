using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.Console2Robot_LogoutRequest)]
    [ResponseType(nameof(Console2Robot_LogoutResponse))]
    public partial class Console2Robot_LogoutRequest : MessageObject, IRequest
    {
        public static Console2Robot_LogoutRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Console2Robot_LogoutRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Console2Robot_LogoutResponse)]
    public partial class Console2Robot_LogoutResponse : MessageObject, IResponse
    {
        public static Console2Robot_LogoutResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Console2Robot_LogoutResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort Console2Robot_LogoutRequest = 4201;
        public const ushort Console2Robot_LogoutResponse = 4202;
    }
}