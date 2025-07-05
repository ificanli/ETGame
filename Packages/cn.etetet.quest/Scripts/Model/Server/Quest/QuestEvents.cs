namespace ET.Server
{
    /// <summary>
    /// 任务接取事件
    /// </summary>
    public struct QuestAcceptedEvent
    {
        public EntityRef<Unit> Unit;
        public int QuestId;
    }

    /// <summary>
    /// 任务完成事件
    /// </summary>
    public struct QuestCompletedEvent
    {
        public EntityRef<Unit> Unit;
        public int QuestId;
        public QuestReward Reward;
    }

    /// <summary>
    /// 任务放弃事件
    /// </summary>
    public struct QuestAbandonedEvent
    {
        public EntityRef<Unit> Unit;
        public int QuestId;
    }

    /// <summary>
    /// 任务目标更新事件
    /// </summary>
    public struct QuestObjectiveUpdatedEvent
    {
        public EntityRef<Unit> Unit;
        public int QuestId;
        public int ObjectiveId;
        public int OldCount;
        public int NewCount;
    }

    /// <summary>
    /// 任务奖励结构
    /// </summary>
    public struct QuestReward
    {
        public int ExpReward;
        public int GoldReward;
        public int[] ItemRewards;
    }
}