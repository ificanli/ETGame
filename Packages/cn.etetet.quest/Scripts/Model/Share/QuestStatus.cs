namespace ET
{
    /// <summary>
    /// 任务状态枚举（前后端共享）
    /// </summary>
    public enum QuestStatus
    {
        None = 0,           // 未接取
        Available = 1,      // 可接取
        InProgress = 2,     // 进行中
        Submited = 4,      // 已完成
        Failed = 5,         // 失败
        Abandoned = 6,      // 已放弃
    }
}