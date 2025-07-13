using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 成就实体
    /// </summary>
    [ChildOf(typeof(AchievementComponent))]
    public class Achievement: Entity, IAwake<int>
    {
        /// <summary>
        /// 配置表Id（唯一标识）
        /// </summary>
        public int ConfigId;
        
        /// <summary>
        /// 成就当前状态
        /// </summary>
        public AchievementStatus Status;

        /// <summary>
        /// 成就类型
        /// </summary>
        public AchievementType Type;

        /// <summary>
        /// 当前进度
        /// </summary>
        public int Progress;

        /// <summary>
        /// 最大进度
        /// </summary>
        public int MaxProgress;

        /// <summary>
        /// 成就点数
        /// </summary>
        public int Points;

        /// <summary>
        /// 前置成就Id列表
        /// </summary>
        public List<int> PreAchievementIds = new();

        /// <summary>
        /// 奖励Id列表
        /// </summary>
        public List<int> RewardIds = new();

        /// <summary>
        /// 成就分类Id
        /// </summary>
        public int CategoryId;

        /// <summary>
        /// 是否隐藏成就
        /// </summary>
        public bool IsHidden;

        /// <summary>
        /// 完成时间
        /// </summary>
        public long CompleteTime;

        /// <summary>
        /// 领取奖励时间
        /// </summary>
        public long ClaimTime;

        /// <summary>
        /// 成就条件参数（用于存储特定的成就条件数据）
        /// </summary>
        public Dictionary<string, object> ConditionParams = new();
    }
}