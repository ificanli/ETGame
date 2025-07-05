namespace ET
{
    /// <summary>
    /// 任务状态枚举（前后端共享）
    /// </summary>
    [Module(ModuleName.Quest)]
    public enum QuestStatus
    {
        None = 0,           // 未接取
        Available = 1,      // 可接取
        InProgress = 2,     // 进行中
        CanSubmit = 3,      // 可提交
        Completed = 4,      // 已完成
        Finished = 4,       // 已完成（别名，保持向后兼容）
        Failed = 5,         // 失败
        Abandoned = 6,      // 已放弃
    }
}