namespace ET
{
    /// <summary>
    /// 阵营类型枚举
    /// </summary>
    public enum CampType
    {
        /// <summary>友方阵营</summary>
        Friendly = 1,
        /// <summary>敌方阵营</summary>
        Enemy = 2,
        /// <summary>中立阵营（不与任何人敌对）</summary>
        Neutral = 3,
    }

    /// <summary>
    /// 阵营关系枚举
    /// </summary>
    public enum CampRelation
    {
        /// <summary>友方</summary>
        Friendly,
        /// <summary>敌方</summary>
        Enemy,
        /// <summary>中立</summary>
        Neutral,
    }
}
