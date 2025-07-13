using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端成就管理组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ClientAchievementComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 成就数据字典（成就Id->成就信息）
        /// </summary>
        public Dictionary<int, EntityRef<ClientAchievementData>> Achievements = new();

        /// <summary>
        /// 成就分类数据字典（分类Id->分类信息）
        /// </summary>
        public Dictionary<int, EntityRef<ClientAchievementCategory>> Categories = new();

        /// <summary>
        /// 已完成的成就Id集合
        /// </summary>
        public HashSet<int> CompletedAchievements = new();

        /// <summary>
        /// 已领取奖励的成就Id集合
        /// </summary>
        public HashSet<int> ClaimedAchievements = new();

        /// <summary>
        /// 成就统计信息
        /// </summary>
        public EntityRef<ClientAchievementStats> Stats;

        /// <summary>
        /// 最近完成的成就列表
        /// </summary>
        public List<int> RecentAchievements = new();

        /// <summary>
        /// 当前选中的分类Id
        /// </summary>
        public int SelectedCategoryId;
    }
}