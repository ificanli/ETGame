namespace ET.Client
{
    /// <summary>
    /// 客户端成就分类数据
    /// </summary>
    public class ClientAchievementCategory: Entity, IAwake<int>
    {
        /// <summary>
        /// 分类Id
        /// </summary>
        public int CategoryId;

        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 分类图标
        /// </summary>
        public string Icon;

        /// <summary>
        /// 排序序号
        /// </summary>
        public int Order;

        /// <summary>
        /// 该分类下总成就数
        /// </summary>
        public int TotalCount;

        /// <summary>
        /// 已完成数量
        /// </summary>
        public int CompletedCount;

        /// <summary>
        /// 完成率
        /// </summary>
        public float CompletionRate => TotalCount > 0 ? (float)CompletedCount / TotalCount : 0f;
    }
}