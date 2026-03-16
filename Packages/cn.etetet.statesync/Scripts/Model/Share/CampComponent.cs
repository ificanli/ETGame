namespace ET
{
    /// <summary>
    /// 阵营组件，挂载在 Unit 上，记录该单位所属阵营信息。
    /// 阵营系统用于判断单位之间的敌对/友方关系，支持自动射击索敌。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CampComponent : Entity, IAwake<int>, IDestroy
    {
        /// <summary>阵营ID（与配置表对应）</summary>
        public int CampId;

        /// <summary>阵营类型（友方/敌方/中立）</summary>
        public CampType CampType;
    }
}
