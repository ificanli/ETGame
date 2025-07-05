using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 客户端任务数据变更事件
    /// </summary>
    public struct ClientQuestDataChanged
    {
        public int QuestId;
        public int Status; // 简化为int状态值
    }

    /// <summary>
    /// 客户端任务目标变更事件
    /// </summary>
    public struct ClientQuestObjectiveChanged
    {
        public int QuestId;
    }

    /// <summary>
    /// 客户端任务移除事件
    /// </summary>
    public struct ClientQuestRemoved
    {
        public int QuestId;
    }

    /// <summary>
    /// 任务数据同步完成事件
    /// </summary>
    public struct QuestDataSyncedEvent
    {
        public Dictionary<int, int> QuestStatusMap; // 简化为任务ID和状态的映射
    }

    /// <summary>
    /// 任务目标进度更新事件
    /// </summary>
    public struct QuestObjectiveProgressUpdatedEvent
    {
        public int QuestId;
        public int ObjectiveId;
        public int OldCount;
        public int NewCount;
        public Dictionary<int, int> Progress;
    }

    /// <summary>
    /// 任务失败事件
    /// </summary>
    public struct QuestFailedEvent
    {
        public int QuestId;
        public string Reason;
    }

    /// <summary>
    /// 任务完成事件
    /// </summary>
    public struct QuestCompletedEvent
    {
        public int QuestId;
        public ClientQuestReward Reward;
        public List<int> RewardItems; // 简化为物品ID列表
    }

    /// <summary>
    /// 客户端任务奖励结构体
    /// </summary>
    public struct ClientQuestReward
    {
        public int Experience;
        public int Gold;
        public List<int> ItemIds;
    }

    /// <summary>
    /// 查询可接取任务失败事件
    /// </summary>
    public struct QueryAvailableQuestsFailedEvent
    {
        public int NPCId;
        public string ErrorMessage;
    }

    /// <summary>
    /// 可接取任务查询完成事件
    /// </summary>
    public struct AvailableQuestsQueriedEvent
    {
        public int NPCId;
        public List<AvailableQuestInfo> AvailableQuests;
    }

    /// <summary>
    /// 获取任务详情失败事件
    /// </summary>
    public struct GetQuestDetailFailedEvent
    {
        public int QuestId;
        public string ErrorMessage;
    }

    /// <summary>
    /// 任务详情接收事件
    /// </summary>
    public struct QuestDetailReceivedEvent
    {
        public int QuestId;
        public QuestDetailInfo DetailInfo;
        public QuestDetailInfo QuestDetail; // 兼容字段名
    }

    /// <summary>
    /// 任务操作失败事件
    /// </summary>
    public struct QuestOperationFailedEvent
    {
        public int QuestId;
        public string Operation;
        public string ErrorMessage;
    }

    /// <summary>
    /// 任务放弃事件
    /// </summary>
    public struct QuestAbandonedEvent
    {
        public int QuestId;
    }
}