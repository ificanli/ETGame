using MemoryPack;
using System.Collections.Generic;

namespace ET
{
    [MemoryPackable]
    [Message(Opcode.ArchiveBattleEventProto)]
    public partial class ArchiveBattleEventProto : MessageObject
    {
        public static ArchiveBattleEventProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ArchiveBattleEventProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long Timestamp { get; set; }
        [MemoryPackOrder(1)]
        public int EventType { get; set; }
        [MemoryPackOrder(2)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(3)]
        public long Value { get; set; }
        [MemoryPackOrder(4)]
        public string Text { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.Timestamp = default;
            this.EventType = default;
            this.PlayerId = default;
            this.Value = default;
            this.Text = default;

            ObjectPool.Recycle(this);
        }
    }

    [MemoryPackable]
    [Message(Opcode.ArchiveBattleRecordSummaryProto)]
    public partial class ArchiveBattleRecordSummaryProto : MessageObject
    {
        public static ArchiveBattleRecordSummaryProto Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ArchiveBattleRecordSummaryProto>(isFromPool);
        }

        [MemoryPackOrder(0)]
        public long RecordId { get; set; }
        [MemoryPackOrder(1)]
        public long PlayerId { get; set; }
        [MemoryPackOrder(2)]
        public int GameMode { get; set; }
        [MemoryPackOrder(3)]
        public string MapName { get; set; }
        [MemoryPackOrder(4)]
        public long MapId { get; set; }
        [MemoryPackOrder(5)]
        public long StartedAt { get; set; }
        [MemoryPackOrder(6)]
        public long FinishedAt { get; set; }
        [MemoryPackOrder(7)]
        public int ResultType { get; set; }
        [MemoryPackOrder(8)]
        public bool IsSuccess { get; set; }
        [MemoryPackOrder(9)]
        public int KillNum { get; set; }
        [MemoryPackOrder(10)]
        public long TotalWealth { get; set; }
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.RecordId = default;
            this.PlayerId = default;
            this.GameMode = default;
            this.MapName = default;
            this.MapId = default;
            this.StartedAt = default;
            this.FinishedAt = default;
            this.ResultType = default;
            this.IsSuccess = default;
            this.KillNum = default;
            this.TotalWealth = default;

            ObjectPool.Recycle(this);
        }
    }

    public static partial class Opcode
    {
        public const ushort ArchiveBattleEventProto = 12025;
        public const ushort ArchiveBattleRecordSummaryProto = 12026;
    }
}