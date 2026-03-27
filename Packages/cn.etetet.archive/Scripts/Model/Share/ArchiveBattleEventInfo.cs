namespace ET
{
    /// <summary>
    /// 战绩事件时间轴节点。
    /// </summary>
    public struct ArchiveBattleEventInfo
    {
        public long Timestamp;
        public int EventType;
        public long PlayerId;
        public long Value;
        public string Text;
    }
}
