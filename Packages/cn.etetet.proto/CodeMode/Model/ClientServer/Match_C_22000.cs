using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.C2G_MatchRequest)]
    [ResponseType(nameof(G2C_MatchRequest))]
    public partial class C2G_MatchRequest : MessageObject, ISessionRequest
    {
        public static C2G_MatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_MatchRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int GameMode { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.GameMode = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_MatchRequest)]
    public partial class G2C_MatchRequest : MessageObject, ISessionResponse
    {
        public static G2C_MatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_MatchRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long RequestId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RequestId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_MatchCancel)]
    [ResponseType(nameof(G2C_MatchCancel))]
    public partial class C2G_MatchCancel : MessageObject, ISessionRequest
    {
        public static C2G_MatchCancel Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_MatchCancel>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long RequestId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.RequestId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_MatchCancel)]
    public partial class G2C_MatchCancel : MessageObject, ISessionResponse
    {
        public static G2C_MatchCancel Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_MatchCancel>(isFromPool);
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
    [Message(Opcode.G2C_MatchSuccess)]
    public partial class G2C_MatchSuccess : MessageObject, IMessage
    {
        public static G2C_MatchSuccess Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_MatchSuccess>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int GameMode { get; set; }
        [MemoryPackOrder(1)]
        public string MapName { get; set; }
        [MemoryPackOrder(2)]
        public List<long> PlayerIds { get; set; } = new();

        [MemoryPackOrder(3)]
        public long MapId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.GameMode = default;
            this.MapName = default;
            this.PlayerIds.Clear();
            this.MapId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_MatchTimeout)]
    public partial class G2C_MatchTimeout : MessageObject, IMessage
    {
        public static G2C_MatchTimeout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_MatchTimeout>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int GameMode { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.GameMode = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort C2G_MatchRequest = 22001;
        public const ushort G2C_MatchRequest = 22002;
        public const ushort C2G_MatchCancel = 22003;
        public const ushort G2C_MatchCancel = 22004;
        public const ushort G2C_MatchSuccess = 22005;
        public const ushort G2C_MatchTimeout = 22006;
    }
}