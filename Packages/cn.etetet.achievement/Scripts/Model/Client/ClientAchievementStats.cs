namespace ET.Client
{
    /// <summary>
    /// 客户端成就统计数据
    /// </summary>
    public class ClientAchievementStats: Entity, IAwake
    {
        /// <summary>
        /// 总成就数
        /// </summary>
        public int TotalAchievements;

        /// <summary>
        /// 已完成成就数
        /// </summary>
        public int CompletedAchievements;

        /// <summary>
        /// 总成就点数
        /// </summary>
        public int TotalPoints;

        /// <summary>
        /// 已获得成就点数
        /// </summary>
        public int EarnedPoints;

        /// <summary>
        /// 完成率
        /// </summary>
        public float CompletionRate => TotalAchievements > 0 ? (float)CompletedAchievements / TotalAchievements : 0f;

        /// <summary>
        /// 点数获得率
        /// </summary>
        public float PointsRate => TotalPoints > 0 ? (float)EarnedPoints / TotalPoints : 0f;
    }
}