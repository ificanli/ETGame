using System;

namespace ET.Server
{
    [EntitySystemOf(typeof(Achievement))]
    public static partial class AchievementSystem
    {
        [EntitySystem]
        private static void Awake(this Achievement self, int configId)
        {
            self.ConfigId = configId;
            self.Status = AchievementStatus.InProgress;
            
            // TODO: 从配置表加载成就数据
            // AchievementConfig config = AchievementConfigCategory.Instance.Get(configId);
            // if (config != null)
            // {
            //     self.Type = (AchievementType)config.Type;
            //     self.MaxProgress = config.MaxProgress;
            //     self.Points = config.Points;
            //     self.CategoryId = config.CategoryId;
            //     self.IsHidden = config.IsHidden;
            //     self.PreAchievementIds.AddRange(config.PreAchievementIds);
            //     self.RewardIds.AddRange(config.RewardIds);
            //     self.ConditionParams = config.ConditionParams;
            // }
        }

        /// <summary>
        /// 检查成就是否完成
        /// </summary>
        public static bool IsCompleted(this Achievement self)
        {
            return self.Progress >= self.MaxProgress;
        }

        /// <summary>
        /// 检查成就是否已领取
        /// </summary>
        public static bool IsClaimed(this Achievement self)
        {
            return self.Status == AchievementStatus.Claimed;
        }

        /// <summary>
        /// 获取进度百分比
        /// </summary>
        public static float GetProgressPercent(this Achievement self)
        {
            return self.MaxProgress > 0 ? (float)self.Progress / self.MaxProgress : 0f;
        }

        /// <summary>
        /// 增加进度
        /// </summary>
        public static void AddProgress(this Achievement self, int value)
        {
            self.Progress = Math.Min(self.Progress + value, self.MaxProgress);
        }

        /// <summary>
        /// 设置进度
        /// </summary>
        public static void SetProgress(this Achievement self, int value)
        {
            self.Progress = Math.Min(value, self.MaxProgress);
        }

        /// <summary>
        /// 检查前置成就是否完成
        /// </summary>
        public static bool CheckPreAchievements(this Achievement self, AchievementComponent achievementComponent)
        {
            foreach (int preAchievementId in self.PreAchievementIds)
            {
                if (!achievementComponent.CompletedAchievements.Contains(preAchievementId))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 重置成就进度
        /// </summary>
        public static void Reset(this Achievement self)
        {
            self.Progress = 0;
            self.Status = AchievementStatus.InProgress;
            self.CompleteTime = 0;
            self.ClaimTime = 0;
        }
    }
}