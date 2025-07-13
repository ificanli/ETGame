using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.C2M_RobotCase_PrepareData_001_Request)]
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
    [Message(Opcode.C2M_RobotCase_PrepareData_001_Response)]
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

    [MemoryPackable]
    [Message(Opcode.C2M_TestRobotCase)]
    [ResponseType(nameof(M2C_TestRobotCase))]
    public partial class C2M_TestRobotCase : MessageObject, ILocationRequest
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
    public partial class M2C_TestRobotCase : MessageObject, ILocationResponse
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
    [Message(Opcode.C2M_RobotCase_PrepareData_005_Request)]
    [ResponseType(nameof(M2C_RobotCase_PrepareData_005_Response))]
    public partial class C2M_RobotCase_PrepareData_005_Request : MessageObject, ILocationRequest
    {
        public static C2M_RobotCase_PrepareData_005_Request Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2M_RobotCase_PrepareData_005_Request>(isFromPool);
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
    [Message(Opcode.M2C_RobotCase_PrepareData_005_Response)]
    public partial class M2C_RobotCase_PrepareData_005_Response : MessageObject, ILocationResponse
    {
        public static M2C_RobotCase_PrepareData_005_Response Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2C_RobotCase_PrepareData_005_Response>(isFromPool);
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
        public const ushort C2M_RobotCase_PrepareData_001_Request = 10501;
        public const ushort C2M_RobotCase_PrepareData_001_Response = 10502;
        public const ushort C2M_TestRobotCase = 10503;
        public const ushort M2C_TestRobotCase = 10504;
        public const ushort C2M_RobotCase_PrepareData_005_Request = 10505;
        public const ushort M2C_RobotCase_PrepareData_005_Response = 10506;
    }
}