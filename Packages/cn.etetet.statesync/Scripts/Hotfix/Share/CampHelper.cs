namespace ET
{
    /// <summary>
    /// 阵营辅助工具类，提供阵营关系判断方法。
    /// 业务逻辑不放在System中，单独提取为Helper，便于复用。
    /// </summary>
    public static class CampHelper
    {
        /// <summary>
        /// 判断两个单位是否互为敌对关系
        /// </summary>
        /// <param name="unit1">单位1</param>
        /// <param name="unit2">单位2</param>
        /// <returns>true表示敌对</returns>
        public static bool IsEnemy(Unit unit1, Unit unit2)
        {
            if (unit1 == null || unit2 == null)
            {
                return false;
            }

            CampComponent camp1 = unit1.GetComponent<CampComponent>();
            CampComponent camp2 = unit2.GetComponent<CampComponent>();

            if (camp1 == null || camp2 == null)
            {
                return false;
            }

            // 中立阵营不与任何人敌对
            if (camp1.CampType == CampType.Neutral || camp2.CampType == CampType.Neutral)
            {
                return false;
            }

            // 不同阵营ID且都不是中立，则敌对
            return camp1.CampId != camp2.CampId;
        }

        /// <summary>
        /// 判断两个单位是否互为友方关系
        /// </summary>
        /// <param name="unit1">单位1</param>
        /// <param name="unit2">单位2</param>
        /// <returns>true表示友方</returns>
        public static bool IsFriendly(Unit unit1, Unit unit2)
        {
            if (unit1 == null || unit2 == null)
            {
                return false;
            }

            CampComponent camp1 = unit1.GetComponent<CampComponent>();
            CampComponent camp2 = unit2.GetComponent<CampComponent>();

            if (camp1 == null || camp2 == null)
            {
                return false;
            }

            return camp1.CampId == camp2.CampId;
        }

        /// <summary>
        /// 判断单位是否为中立
        /// </summary>
        /// <param name="unit">单位</param>
        /// <returns>true表示中立</returns>
        public static bool IsNeutral(Unit unit)
        {
            if (unit == null)
            {
                return false;
            }

            CampComponent camp = unit.GetComponent<CampComponent>();
            if (camp == null)
            {
                return false;
            }

            return camp.CampType == CampType.Neutral;
        }

        /// <summary>
        /// 获取两个单位之间的阵营关系
        /// </summary>
        /// <param name="unit1">单位1</param>
        /// <param name="unit2">单位2</param>
        /// <returns>阵营关系枚举</returns>
        public static CampRelation GetRelation(Unit unit1, Unit unit2)
        {
            if (IsFriendly(unit1, unit2))
            {
                return CampRelation.Friendly;
            }

            if (IsEnemy(unit1, unit2))
            {
                return CampRelation.Enemy;
            }

            return CampRelation.Neutral;
        }
    }
}
