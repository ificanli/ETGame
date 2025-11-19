using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.RobotCase_001_PrepareData_Request)]
    [ResponseType(nameof(RobotCase_001_PrepareData_Response))]
    public partial class RobotCase_001_PrepareData_Request : MessageObject, ILocationRequest, IRobotCaseMessage
    {
        public static RobotCase_001_PrepareData_Request Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RobotCase_001_PrepareData_Request>(isFromPool);
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
    [Message(Opcode.RobotCase_001_PrepareData_Response)]
    public partial class RobotCase_001_PrepareData_Response : MessageObject, ILocationResponse, IRobotCaseMessage
    {
        public static RobotCase_001_PrepareData_Response Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RobotCase_001_PrepareData_Response>(isFromPool);
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
        public const ushort RobotCase_001_PrepareData_Request = 10501;
        public const ushort RobotCase_001_PrepareData_Response = 10502;
    }
}