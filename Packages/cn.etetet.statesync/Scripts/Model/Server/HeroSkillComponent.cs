namespace ET.Server
{
    /// <summary>
    /// 英雄技能组件（桃子：范围持续治疗）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class HeroSkillComponent : Entity, IAwake<int>, IDestroy
    {
        /// <summary>英雄配置ID</summary>
        public int HeroConfigId;

        /// <summary>技能定时器ID</summary>
        public long SkillTimerId;

        /// <summary>治疗范围（米）</summary>
        public float HealRange;

        /// <summary>治疗间隔（毫秒）</summary>
        public int HealIntervalMs;

        /// <summary>每次治疗量</summary>
        public float HealAmount;
    }
}
