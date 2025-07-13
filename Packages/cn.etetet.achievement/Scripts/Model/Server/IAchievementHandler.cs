namespace ET.Server
{
    /// <summary>
    /// 成就处理器接口
    /// </summary>
    public interface IAchievementHandler
    {
        /// <summary>
        /// 成就类型
        /// </summary>
        AchievementType AchievementType { get; }

        /// <summary>
        /// 检查成就是否可以激活
        /// </summary>
        /// <param name="unit">玩家单位</param>
        /// <param name="achievementId">成就Id</param>
        /// <returns>是否可以激活</returns>
        bool CanActivate(Unit unit, int achievementId);

        /// <summary>
        /// 更新成就进度
        /// </summary>
        /// <param name="unit">玩家单位</param>
        /// <param name="achievementId">成就Id</param>
        /// <param name="eventData">事件数据</param>
        /// <returns>更新后的进度</returns>
        int UpdateProgress(Unit unit, int achievementId, object eventData);

        /// <summary>
        /// 检查成就是否完成
        /// </summary>
        /// <param name="unit">玩家单位</param>
        /// <param name="achievementId">成就Id</param>
        /// <returns>是否完成</returns>
        bool IsComplete(Unit unit, int achievementId);
    }
}