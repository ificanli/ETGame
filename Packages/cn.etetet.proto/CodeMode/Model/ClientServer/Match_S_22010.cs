using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.G2Match_MatchRequest)]
    [ResponseType(nameof(Match2G_MatchRequest))]
    public partial class G2Match_MatchRequest : MessageObject, IRequest
    {
        public static G2Match_MatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Match_MatchRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(2)]
        public int GameMode { get; set; }
        [MemoryPackOrder(3)]
        public long GateActorId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.PlayerId = default;
            this.GameMode = default;
            this.GateActorId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Match2G_MatchRequest)]
    public partial class Match2G_MatchRequest : MessageObject, IResponse
    {
        public static Match2G_MatchRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2G_MatchRequest>(isFromPool);
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
    [Message(Opcode.G2Match_MatchCancel)]
    [ResponseType(nameof(Match2G_MatchCancel))]
    public partial class G2Match_MatchCancel : MessageObject, IRequest
    {
        public static G2Match_MatchCancel Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Match_MatchCancel>(isFromPool);
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
    [Message(Opcode.Match2G_MatchCancel)]
    public partial class Match2G_MatchCancel : MessageObject, IResponse
    {
        public static Match2G_MatchCancel Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2G_MatchCancel>(isFromPool);
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
    [Message(Opcode.Match2G_MatchSuccess)]
    [ResponseType(nameof(G2Match_MatchSuccess))]
    public partial class Match2G_MatchSuccess : MessageObject, IRequest
    {
        public static Match2G_MatchSuccess Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2G_MatchSuccess>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int GameMode { get; set; }
        [MemoryPackOrder(2)]
        public string MapName { get; set; }
        [MemoryPackOrder(3)]
        public List<long> PlayerIds { get; set; } = new();

        [MemoryPackOrder(4)]
        public long MapId { get; set; }
        [MemoryPackOrder(5)]
        public List<int> TeamIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.GameMode = default;
            this.MapName = default;
            this.PlayerIds.Clear();
            this.MapId = default;
            this.TeamIds.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Match_MatchSuccess)]
    public partial class G2Match_MatchSuccess : MessageObject, IResponse
    {
        public static G2Match_MatchSuccess Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Match_MatchSuccess>(isFromPool);
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
    [Message(Opcode.Match2Map_InitMatchCopyRequest)]
    [ResponseType(nameof(Map2Match_InitMatchCopyResponse))]
    public partial class Match2Map_InitMatchCopyRequest : MessageObject, IRequest
    {
        public static Match2Map_InitMatchCopyRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2Map_InitMatchCopyRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int GameMode { get; set; }
        [MemoryPackOrder(2)]
        public string MapName { get; set; }
        [MemoryPackOrder(3)]
        public long MapId { get; set; }
        [MemoryPackOrder(4)]
        public List<long> HumanPlayerIds { get; set; } = new();

        [MemoryPackOrder(5)]
        public List<long> RobotPlayerIds { get; set; } = new();

        [MemoryPackOrder(6)]
        public List<long> PlayerIds { get; set; } = new();

        [MemoryPackOrder(7)]
        public List<int> TeamIds { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.GameMode = default;
            this.MapName = default;
            this.MapId = default;
            this.HumanPlayerIds.Clear();
            this.RobotPlayerIds.Clear();
            this.PlayerIds.Clear();
            this.TeamIds.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Map2Match_InitMatchCopyResponse)]
    public partial class Map2Match_InitMatchCopyResponse : MessageObject, IResponse
    {
        public static Map2Match_InitMatchCopyResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Map2Match_InitMatchCopyResponse>(isFromPool);
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
    [Message(Opcode.Match2G_MatchTimeout)]
    public partial class Match2G_MatchTimeout : MessageObject, IMessage
    {
        public static Match2G_MatchTimeout Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Match2G_MatchTimeout>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(1)]
        public int GameMode { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.PlayerId = default;
            this.GameMode = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort G2Match_MatchRequest = 22011;
        public const ushort Match2G_MatchRequest = 22012;
        public const ushort G2Match_MatchCancel = 22013;
        public const ushort Match2G_MatchCancel = 22014;
        public const ushort Match2G_MatchSuccess = 22015;
        public const ushort G2Match_MatchSuccess = 22016;
        public const ushort Match2Map_InitMatchCopyRequest = 22017;
        public const ushort Map2Match_InitMatchCopyResponse = 22018;
        public const ushort Match2G_MatchTimeout = 22019;
    }
}