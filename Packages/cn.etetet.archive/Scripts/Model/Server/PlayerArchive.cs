using System.Collections.Generic;

namespace ET.Server
{
    [ChildOf(typeof(ArchiveManagerComponent))]
    public class PlayerArchive : Entity, IAwake<string>, IDestroy
    {
        public string Account { get; set; }

        public int WarehouseColumnCount { get; set; }

        public List<ArchiveWarehouseItemInfo> WarehouseItems { get; set; } = new();

        public List<ArchiveItemCountInfo> LastEvacuationItems { get; set; } = new();

        public long LastEvacuationWealth { get; set; }

        public long TotalWealth { get; set; }

        public long CurrentBattleRecordId { get; set; }
    }
}
