namespace ET.Client
{
    /// <summary>
    /// 客户端成就奖励数据
    /// </summary>
    public class ClientAchievementReward: Entity, IAwake<int, int, int>
    {
        /// <summary>
        /// 奖励类型：1=经验，2=金币，3=道具
        /// </summary>
        public int Type;

        /// <summary>
        /// 道具Id（奖励类型为道具时使用）
        /// </summary>
        public int ItemId;

        /// <summary>
        /// 数量
        /// </summary>
        public int Count;

        /// <summary>
        /// 奖励图标
        /// </summary>
        public string Icon;

        /// <summary>
        /// 奖励名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 奖励描述
        /// </summary>
        public string Description;
    }
}