using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.M2A_Reload)]
    [ResponseType(nameof(A2M_Reload))]
    public partial class M2A_Reload : MessageObject, IRequest
    {
        public static M2A_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2A_Reload>(isFromPool);
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
    [Message(Opcode.A2M_Reload)]
    public partial class A2M_Reload : MessageObject, IResponse
    {
        public static A2M_Reload Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2M_Reload>(isFromPool);
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
    [Message(Opcode.M2M_UnitTransferRequest)]
    [ResponseType(nameof(M2M_UnitTransferResponse))]
    public partial class M2M_UnitTransferRequest : MessageObject, IRequest
    {
        public static M2M_UnitTransferRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2M_UnitTransferRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public ActorId OldActorId { get; set; }

        [MemoryPackOrder(2)]
        public byte[] Unit { get; set; }

        [MemoryPackOrder(3)]
        public List<byte[]> Entitys { get; set; } = new();

        [MemoryPackOrder(4)]
        public bool ChangeScene { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.OldActorId = default;
            this.Unit = default;
            this.Entitys.Clear();
            this.ChangeScene = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.M2M_UnitTransferResponse)]
    public partial class M2M_UnitTransferResponse : MessageObject, IResponse
    {
        public static M2M_UnitTransferResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<M2M_UnitTransferResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public ActorId NewActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.NewActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.A2MapManager_GetMapRequest)]
    [ResponseType(nameof(A2MapManager_GetMapResponse))]
    public partial class A2MapManager_GetMapRequest : MessageObject, IRequest
    {
        public static A2MapManager_GetMapRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2MapManager_GetMapRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string MapName { get; set; }

        [MemoryPackOrder(2)]
        public int Line { get; set; }

        [MemoryPackOrder(3)]
        public long UnitId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.MapName = default;
            this.Line = default;
            this.UnitId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.A2MapManager_GetMapResponse)]
    public partial class A2MapManager_GetMapResponse : MessageObject, IResponse
    {
        public static A2MapManager_GetMapResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2MapManager_GetMapResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public int Error { get; set; }

        [MemoryPackOrder(2)]
        public string Message { get; set; }

        [MemoryPackOrder(3)]
        public string MapName { get; set; }

        [MemoryPackOrder(4)]
        public int Line { get; set; }

        [MemoryPackOrder(5)]
        public ActorId MapActorId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MapName = default;
            this.Line = default;
            this.MapActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.A2MapManager_NotifyPlayerAlreadyEnterMapRequest)]
    [ResponseType(nameof(A2MapManager_NotifyPlayerAlreadyEnterMapResponse))]
    public partial class A2MapManager_NotifyPlayerAlreadyEnterMapRequest : MessageObject, IRequest
    {
        public static A2MapManager_NotifyPlayerAlreadyEnterMapRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2MapManager_NotifyPlayerAlreadyEnterMapRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string MapName { get; set; }

        [MemoryPackOrder(2)]
        public int Line { get; set; }

        [MemoryPackOrder(3)]
        public long UnitId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.MapName = default;
            this.Line = default;
            this.UnitId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.A2MapManager_NotifyPlayerAlreadyEnterMapResponse)]
    public partial class A2MapManager_NotifyPlayerAlreadyEnterMapResponse : MessageObject, IResponse
    {
        public static A2MapManager_NotifyPlayerAlreadyEnterMapResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<A2MapManager_NotifyPlayerAlreadyEnterMapResponse>(isFromPool);
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
    [Message(Opcode.MapManager2Map_NotifyPlayerTransferRequest)]
    [ResponseType(nameof(MapManager2Map_NotifyPlayerTransferResponse))]
    public partial class MapManager2Map_NotifyPlayerTransferRequest : MessageObject, ILocationRequest
    {
        public static MapManager2Map_NotifyPlayerTransferRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<MapManager2Map_NotifyPlayerTransferRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string MapName { get; set; }

        [MemoryPackOrder(2)]
        public int Line { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.MapName = default;
            this.Line = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.MapManager2Map_NotifyPlayerTransferResponse)]
    public partial class MapManager2Map_NotifyPlayerTransferResponse : MessageObject, ILocationResponse
    {
        public static MapManager2Map_NotifyPlayerTransferResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<MapManager2Map_NotifyPlayerTransferResponse>(isFromPool);
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
    [Message(Opcode.Map2MapManager_LogoutRequest)]
    [ResponseType(nameof(Map2MapManager_LogoutResponse))]
    public partial class Map2MapManager_LogoutRequest : MessageObject, IRequest
    {
        public static Map2MapManager_LogoutRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2MapManager_LogoutRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }

        [MemoryPackOrder(1)]
        public string MapName { get; set; }

        [MemoryPackOrder(2)]
        public long UnitId { get; set; }

        [MemoryPackOrder(3)]
        public long MapId { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.MapName = default;
            this.UnitId = default;
            this.MapId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Map2MapManager_LogoutResponse)]
    public partial class Map2MapManager_LogoutResponse : MessageObject, IResponse
    {
        public static Map2MapManager_LogoutResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2MapManager_LogoutResponse>(isFromPool);
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
    [Message(Opcode.G2Map_Logout)]
    [ResponseType(nameof(Map2G_Logout))]
    public partial class G2Map_Logout : MessageObject, ILocationRequest
    {
        public static G2Map_Logout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Map_Logout>(isFromPool);
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
    [Message(Opcode.Map2G_Logout)]
    public partial class Map2G_Logout : MessageObject, ILocationResponse
    {
        public static Map2G_Logout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2G_Logout>(isFromPool);
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
        public const ushort M2A_Reload = 4001;
        public const ushort A2M_Reload = 4002;
        public const ushort M2M_UnitTransferRequest = 4003;
        public const ushort M2M_UnitTransferResponse = 4004;
        public const ushort A2MapManager_GetMapRequest = 4005;
        public const ushort A2MapManager_GetMapResponse = 4006;
        public const ushort A2MapManager_NotifyPlayerAlreadyEnterMapRequest = 4007;
        public const ushort A2MapManager_NotifyPlayerAlreadyEnterMapResponse = 4008;
        public const ushort MapManager2Map_NotifyPlayerTransferRequest = 4009;
        public const ushort MapManager2Map_NotifyPlayerTransferResponse = 4010;
        public const ushort Map2MapManager_LogoutRequest = 4011;
        public const ushort Map2MapManager_LogoutResponse = 4012;
        public const ushort G2Map_Logout = 4013;
        public const ushort Map2G_Logout = 4014;
    }
}