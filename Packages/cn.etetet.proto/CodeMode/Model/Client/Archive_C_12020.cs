using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.C2G_GetBattleRecordList)]
    [ResponseType(nameof(G2C_GetBattleRecordList))]
    public partial class C2G_GetBattleRecordList : MessageObject, ISessionRequest
    {
        public static C2G_GetBattleRecordList Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_GetBattleRecordList>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Limit { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Limit = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_GetBattleRecordList)]
    public partial class G2C_GetBattleRecordList : MessageObject, ISessionResponse
    {
        public static G2C_GetBattleRecordList Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_GetBattleRecordList>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public List<ArchiveBattleRecordSummaryProto> Records { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Records.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.C2G_GetBattleRecordDetail)]
    [ResponseType(nameof(G2C_GetBattleRecordDetail))]
    public partial class C2G_GetBattleRecordDetail : MessageObject, ISessionRequest
    {
        public static C2G_GetBattleRecordDetail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<C2G_GetBattleRecordDetail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public long RecordId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.RecordId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2C_GetBattleRecordDetail)]
    public partial class G2C_GetBattleRecordDetail : MessageObject, ISessionResponse
    {
        public static G2C_GetBattleRecordDetail Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2C_GetBattleRecordDetail>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public ArchiveBattleRecordSummaryProto Record { get; set; }
        [MemoryPackOrder(4)]
        public List<ArchiveBattleEventProto> Events { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Record = default;
            this.Events.Clear();

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort C2G_GetBattleRecordList = 12021;
        public const ushort G2C_GetBattleRecordList = 12022;
        public const ushort C2G_GetBattleRecordDetail = 12023;
        public const ushort G2C_GetBattleRecordDetail = 12024;
    }
}