using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class ContainerComponent : Entity, IAwake<string>
    {
        public string PointId { get; set; }
        public int State { get; set; }
        public int OutputMode { get; set; }
        public bool LootGenerated { get; set; }
        public bool HasOpenedOnce { get; set; }
        public long CreatorPlayerId { get; set; }
        public long CreateTime { get; set; }

        // 容器内物品，Key=槽位索引。
        public Dictionary<int, ContainerItemEntry> ItemEntries { get; set; } = new();

        // 玩家搜索会话信息，Key=PlayerId。
        public Dictionary<long, string> SearchTimerIds { get; set; } = new();
        public Dictionary<long, long> SearchStartTimes { get; set; } = new();
        public Dictionary<long, long> SearchDurations { get; set; } = new();

        // 最近一次掉落请求记录（调试/兼容旧流程用）。
        public string LastLootTable { get; set; }
        public int LastDropCount { get; set; }
        public float LastDropRadius { get; set; }
        public long LastRequestTime { get; set; }
        public long LastRequestPlayerId { get; set; }
    }
}
