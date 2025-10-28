using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 客户端任务数据变更事件
    /// </summary>
    public struct ClientQuestDataChanged
    {
        public long QuestId;
        public int Status; // 简化为int状态值
    }

    /// <summary>
    /// 客户端任务目标变更事件
    /// </summary>
    public struct ClientQuestObjectiveChanged
    {
        public long QuestId;
    }

    /// <summary>
    /// 客户端任务移除事件
    /// </summary>
    public struct ClientQuestRemoved
    {
        public long QuestId;
    }

    /// <summary>
    /// 任务数据同步完成事件
    /// </summary>
    public struct QuestDataSyncedEvent
    {
        public Dictionary<long, int> QuestStatusMap; // 简化为任务ID和状态的映射
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
    /// 任务放弃事件
    /// </summary>
    public struct QuestAbandonedEvent
    {
        public int QuestId;
    }
}