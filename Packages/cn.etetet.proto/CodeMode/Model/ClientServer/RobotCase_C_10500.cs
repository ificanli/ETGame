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

    [MemoryPackable]
    [Message(Opcode.C2M_TestRobotCase)]
    [ResponseType(nameof(M2C_TestRobotCase))]
    public partial class C2M_TestRobotCase : MessageObject, ILocationRequest, IRobotCaseMessage
    {
        public static C2M_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_TestRobotCase>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.N = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2C_TestRobotCase)]
    public partial class M2C_TestRobotCase : MessageObject, ILocationResponse, IRobotCaseMessage
    {
        public static M2C_TestRobotCase Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_TestRobotCase>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public int N { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.N = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.RobotCase_005_PrepareData_Request)]
    [ResponseType(nameof(RobotCase_005_PrepareData_Response))]
    public partial class RobotCase_005_PrepareData_Request : MessageObject, ILocationRequest, IRobotCaseMessage
    {
        public static RobotCase_005_PrepareData_Request Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RobotCase_005_PrepareData_Request>(isFromPool);
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
    [Message(Opcode.RobotCase_005_PrepareData_Response)]
    public partial class RobotCase_005_PrepareData_Response : MessageObject, ILocationResponse, IRobotCaseMessage
    {
        public static RobotCase_005_PrepareData_Response Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<RobotCase_005_PrepareData_Response>(isFromPool);
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
        public const ushort C2M_TestRobotCase = 10503;
        public const ushort M2C_TestRobotCase = 10504;
        public const ushort RobotCase_005_PrepareData_Request = 10505;
        public const ushort RobotCase_005_PrepareData_Response = 10506;
    }
}