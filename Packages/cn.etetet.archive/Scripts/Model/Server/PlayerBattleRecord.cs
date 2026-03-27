using System.Collections.Generic;

namespace ET.Server
{
    [ChildOf(typeof(PlayerArchive))]
    public class PlayerBattleRecord : Entity, IAwake, IDestroy
    {
        public long PlayerId { get; set; }

        public int GameMode { get; set; }

        public string MapName { get; set; }

        public long MapId { get; set; }

        public long StartedAt { get; set; }

        public long FinishedAt { get; set; }

        public int ResultType { get; set; }

        public bool IsSuccess { get; set; }

        public int KillNum { get; set; }

        public long TotalWealth { get; set; }

        public List<ArchiveBattleEventInfo> Events { get; set; } = new();
    }
}
