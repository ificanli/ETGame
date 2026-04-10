using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.ArchiveWarehouseItemProto)]
    public partial class ArchiveWarehouseItemProto : MessageObject
    {
        public static ArchiveWarehouseItemProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ArchiveWarehouseItemProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long ItemUid { get; set; }
        [MemoryPackOrder(1)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(2)]
        public int Count { get; set; }
        [MemoryPackOrder(3)]
        public int GridWidth { get; set; }
        [MemoryPackOrder(4)]
        public int GridHeight { get; set; }
        [MemoryPackOrder(5)]
        public int AnchorSlotIndex { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ItemUid = default;
            this.ConfigId = default;
            this.Count = default;
            this.GridWidth = default;
            this.GridHeight = default;
            this.AnchorSlotIndex = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ArchiveItemCountProto)]
    public partial class ArchiveItemCountProto : MessageObject
    {
        public static ArchiveItemCountProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ArchiveItemCountProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int ConfigId { get; set; }
        [MemoryPackOrder(1)]
        public int Count { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.ConfigId = default;
            this.Count = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Archive_GetOrCreatePlayerArchiveRequest)]
    [ResponseType(nameof(Archive2G_GetOrCreatePlayerArchiveResponse))]
    public partial class G2Archive_GetOrCreatePlayerArchiveRequest : MessageObject, IRequest
    {
        public static G2Archive_GetOrCreatePlayerArchiveRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Archive_GetOrCreatePlayerArchiveRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(3)]
        public long LastEvacuationWealth { get; set; }
        [MemoryPackOrder(4)]
        public int WarehouseColumnCount { get; set; }
        [MemoryPackOrder(5)]
        public List<ArchiveWarehouseItemProto> WarehouseItems { get; set; } = new();

        [MemoryPackOrder(6)]
        public List<ArchiveItemCountProto> LastEvacuationItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.TotalWealth = default;
            this.LastEvacuationWealth = default;
            this.WarehouseColumnCount = default;
            this.WarehouseItems.Clear();
            this.LastEvacuationItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Archive2G_GetOrCreatePlayerArchiveResponse)]
    public partial class Archive2G_GetOrCreatePlayerArchiveResponse : MessageObject, IResponse
    {
        public static Archive2G_GetOrCreatePlayerArchiveResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Archive2G_GetOrCreatePlayerArchiveResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(4)]
        public long LastEvacuationWealth { get; set; }
        [MemoryPackOrder(5)]
        public int WarehouseColumnCount { get; set; }
        [MemoryPackOrder(6)]
        public List<ArchiveWarehouseItemProto> WarehouseItems { get; set; } = new();

        [MemoryPackOrder(7)]
        public List<ArchiveItemCountProto> LastEvacuationItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.TotalWealth = default;
            this.LastEvacuationWealth = default;
            this.WarehouseColumnCount = default;
            this.WarehouseItems.Clear();
            this.LastEvacuationItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Archive_SavePlayerArchiveRequest)]
    [ResponseType(nameof(Archive2G_SavePlayerArchiveResponse))]
    public partial class G2Archive_SavePlayerArchiveRequest : MessageObject, IRequest
    {
        public static G2Archive_SavePlayerArchiveRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Archive_SavePlayerArchiveRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(3)]
        public long LastEvacuationWealth { get; set; }
        [MemoryPackOrder(4)]
        public int WarehouseColumnCount { get; set; }
        [MemoryPackOrder(5)]
        public List<ArchiveWarehouseItemProto> WarehouseItems { get; set; } = new();

        [MemoryPackOrder(6)]
        public List<ArchiveItemCountProto> LastEvacuationItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.TotalWealth = default;
            this.LastEvacuationWealth = default;
            this.WarehouseColumnCount = default;
            this.WarehouseItems.Clear();
            this.LastEvacuationItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Archive2G_SavePlayerArchiveResponse)]
    public partial class Archive2G_SavePlayerArchiveResponse : MessageObject, IResponse
    {
        public static Archive2G_SavePlayerArchiveResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Archive2G_SavePlayerArchiveResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(4)]
        public long LastEvacuationWealth { get; set; }
        [MemoryPackOrder(5)]
        public int WarehouseColumnCount { get; set; }
        [MemoryPackOrder(6)]
        public List<ArchiveWarehouseItemProto> WarehouseItems { get; set; } = new();

        [MemoryPackOrder(7)]
        public List<ArchiveItemCountProto> LastEvacuationItems { get; set; } = new();

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.TotalWealth = default;
            this.LastEvacuationWealth = default;
            this.WarehouseColumnCount = default;
            this.WarehouseItems.Clear();
            this.LastEvacuationItems.Clear();

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Archive_RecordMatchStartRequest)]
    [ResponseType(nameof(Archive2G_RecordMatchStartResponse))]
    public partial class G2Archive_RecordMatchStartRequest : MessageObject, IRequest
    {
        public static G2Archive_RecordMatchStartRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Archive_RecordMatchStartRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(3)]
        public int GameMode { get; set; }
        [MemoryPackOrder(4)]
        public string MapName { get; set; }
        [MemoryPackOrder(5)]
        public long MapId { get; set; }
        [MemoryPackOrder(6)]
        public long StartTime { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.PlayerId = default;
            this.GameMode = default;
            this.MapName = default;
            this.MapId = default;
            this.StartTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Archive2G_RecordMatchStartResponse)]
    public partial class Archive2G_RecordMatchStartResponse : MessageObject, IResponse
    {
        public static Archive2G_RecordMatchStartResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Archive2G_RecordMatchStartResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long RecordId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RecordId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Archive_RecordBattleResultRequest)]
    [ResponseType(nameof(Archive2G_RecordBattleResultResponse))]
    public partial class G2Archive_RecordBattleResultRequest : MessageObject, IRequest
    {
        public static G2Archive_RecordBattleResultRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Archive_RecordBattleResultRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(3)]
        public int ResultType { get; set; }
        [MemoryPackOrder(4)]
        public bool IsSuccess { get; set; }
        [MemoryPackOrder(5)]
        public long TotalWealth { get; set; }
        [MemoryPackOrder(6)]
        public int KillNum { get; set; }
        [MemoryPackOrder(7)]
        public long FinishTime { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.PlayerId = default;
            this.ResultType = default;
            this.IsSuccess = default;
            this.TotalWealth = default;
            this.KillNum = default;
            this.FinishTime = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Archive2G_RecordBattleResultResponse)]
    public partial class Archive2G_RecordBattleResultResponse : MessageObject, IResponse
    {
        public static Archive2G_RecordBattleResultResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Archive2G_RecordBattleResultResponse>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public int Error { get; set; }
        [MemoryPackOrder(2)]
        public string Message { get; set; }
        [MemoryPackOrder(3)]
        public long RecordId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.RecordId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.G2Archive_GetBattleRecordListRequest)]
    [ResponseType(nameof(Archive2G_GetBattleRecordListResponse))]
    public partial class G2Archive_GetBattleRecordListRequest : MessageObject, IRequest
    {
        public static G2Archive_GetBattleRecordListRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Archive_GetBattleRecordListRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public int Limit { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.Limit = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Archive2G_GetBattleRecordListResponse)]
    public partial class Archive2G_GetBattleRecordListResponse : MessageObject, IResponse
    {
        public static Archive2G_GetBattleRecordListResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Archive2G_GetBattleRecordListResponse>(isFromPool);
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
    [Message(Opcode.G2Archive_GetBattleRecordDetailRequest)]
    [ResponseType(nameof(Archive2G_GetBattleRecordDetailResponse))]
    public partial class G2Archive_GetBattleRecordDetailRequest : MessageObject, IRequest
    {
        public static G2Archive_GetBattleRecordDetailRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<G2Archive_GetBattleRecordDetailRequest>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public int RpcId { get; set; }
        [MemoryPackOrder(1)]
        public string Account { get; set; }
        [MemoryPackOrder(2)]
        public long RecordId { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RpcId = default;
            this.Account = default;
            this.RecordId = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.Archive2G_GetBattleRecordDetailResponse)]
    public partial class Archive2G_GetBattleRecordDetailResponse : MessageObject, IResponse
    {
        public static Archive2G_GetBattleRecordDetailResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<Archive2G_GetBattleRecordDetailResponse>(isFromPool);
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
        public const ushort ArchiveWarehouseItemProto = 22021;
        public const ushort ArchiveItemCountProto = 22022;
        public const ushort G2Archive_GetOrCreatePlayerArchiveRequest = 22023;
        public const ushort Archive2G_GetOrCreatePlayerArchiveResponse = 22024;
        public const ushort G2Archive_SavePlayerArchiveRequest = 22025;
        public const ushort Archive2G_SavePlayerArchiveResponse = 22026;
        public const ushort G2Archive_RecordMatchStartRequest = 22027;
        public const ushort Archive2G_RecordMatchStartResponse = 22028;
        public const ushort G2Archive_RecordBattleResultRequest = 22029;
        public const ushort Archive2G_RecordBattleResultResponse = 22030;
        public const ushort G2Archive_GetBattleRecordListRequest = 22031;
        public const ushort Archive2G_GetBattleRecordListResponse = 22032;
        public const ushort G2Archive_GetBattleRecordDetailRequest = 22033;
        public const ushort Archive2G_GetBattleRecordDetailResponse = 22034;
    }
}