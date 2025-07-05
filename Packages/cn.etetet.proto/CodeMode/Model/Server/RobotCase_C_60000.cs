using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(RobotCase.C2M_RobotCase_PrepareData_001_Request)]
    [ResponseType(nameof(C2M_RobotCase_PrepareData_001_Response))]
    public partial class C2M_RobotCase_PrepareData_001_Request : MessageObject, ILocationRequest
    {
        public static C2M_RobotCase_PrepareData_001_Request Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_RobotCase_PrepareData_001_Request>(isFromPool);
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
    [Message(RobotCase.C2M_RobotCase_PrepareData_001_Response)]
    public partial class C2M_RobotCase_PrepareData_001_Response : MessageObject, ILocationResponse
    {
        public static C2M_RobotCase_PrepareData_001_Response Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_RobotCase_PrepareData_001_Response>(isFromPool);
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

    public static class RobotCase
    {
        public const ushort C2M_RobotCase_PrepareData_001_Request = 60001;
        public const ushort C2M_RobotCase_PrepareData_001_Response = 60002;
    }
}