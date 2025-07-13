using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端成就数据
    /// </summary>
    public class ClientAchievementData: Entity, IAwake<int>
    {
        /// <summary>
        /// 成就Id
        /// </summary>
        public int AchievementId;

        /// <summary>
        /// 成就名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 成就描述
        /// </summary>
        public string Description;

        /// <summary>
        /// 成就图标
        /// </summary>
        public string Icon;

        /// <summary>
        /// 成就状态
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
        /// 分类Id
        /// </summary>
        public int CategoryId;

        /// <summary>
        /// 前置成就Id列表
        /// </summary>
        public List<int> PreAchievements = new();

        /// <summary>
        /// 奖励列表
        /// </summary>
        public List<EntityRef<ClientAchievementReward>> Rewards = new();

        /// <summary>
        /// 完成时间
        /// </summary>
        public long CompleteTime;

        /// <summary>
        /// 领取时间
        /// </summary>
        public long ClaimTime;

        /// <summary>
        /// 是否隐藏成就
        /// </summary>
        public bool IsHidden;

        /// <summary>
        /// 进度百分比
        /// </summary>
        public float ProgressPercent => MaxProgress > 0 ? (float)Progress / MaxProgress : 0f;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => Status >= AchievementStatus.Completed;

        /// <summary>
        /// 是否已领取
        /// </summary>
        public bool IsClaimed => Status == AchievementStatus.Claimed;
    }
}