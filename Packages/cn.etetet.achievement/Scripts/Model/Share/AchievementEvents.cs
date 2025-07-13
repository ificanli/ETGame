namespace ET
{
    /// <summary>
    /// 成就进度更新事件
    /// </summary>
    public struct AchievementProgressEvent
    {
        public long UnitId { get; set; }
        public int AchievementId { get; set; }
        public int Progress { get; set; }
        public int MaxProgress { get; set; }
    }

    /// <summary>
    /// 成就完成事件
    /// </summary>
    public struct AchievementCompleteEvent
    {
        public long UnitId { get; set; }
        public int AchievementId { get; set; }
        public long CompleteTime { get; set; }
    }

    /// <summary>
    /// 成就奖励领取事件
    /// </summary>
    public struct AchievementClaimedEvent
    {
        public long UnitId { get; set; }
        public int AchievementId { get; set; }
        public long ClaimTime { get; set; }
    }

    /// <summary>
    /// 击杀怪物事件
    /// </summary>
    public struct KillMonsterEvent
    {
        public long UnitId { get; set; }
        public int MonsterId { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 等级提升事件
    /// </summary>
    public struct LevelUpEvent
    {
        public long UnitId { get; set; }
        public int Level { get; set; }
    }

    /// <summary>
    /// 任务完成事件
    /// </summary>
    public struct QuestCompleteEvent
    {
        public long UnitId { get; set; }
        public int QuestId { get; set; }
    }

    /// <summary>
    /// 道具收集事件
    /// </summary>
    public struct ItemCollectEvent
    {
        public long UnitId { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 地图探索事件
    /// </summary>
    public struct MapExploreEvent
    {
        public long UnitId { get; set; }
        public int MapId { get; set; }
    }
}