namespace ET.Server
{
    /// <summary>
    /// 成就进度实体
    /// </summary>
    [ChildOf(typeof(AchievementComponent))]
    public class AchievementProgress: Entity, IAwake<int, int>
    {
        /// <summary>
        /// 成就Id
        /// </summary>
        public int AchievementId;

        /// <summary>
        /// 当前进度
        /// </summary>
        public int Progress;

        /// <summary>
        /// 最大进度
        /// </summary>
        public int MaxProgress;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public long LastUpdateTime;

        /// <summary>
        /// 进度是否已完成
        /// </summary>
        public bool IsCompleted => Progress >= MaxProgress;

        /// <summary>
        /// 进度百分比
        /// </summary>
        public float ProgressPercent => MaxProgress > 0 ? (float)Progress / MaxProgress : 0f;
    }
}