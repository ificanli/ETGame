using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(AchievementComponent))]
    public static partial class AchievementComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AchievementComponent self)
        {
            // 初始化成就系统
        }

        [EntitySystem]
        private static void Destroy(this AchievementComponent self)
        {
            // 清理资源
        }

        /// <summary>
        /// 初始化成就系统
        /// </summary>
        public static void Initialize(this AchievementComponent self)
        {
            // TODO: 从配置加载所有成就
            // 激活符合条件的成就
            self.ActivateEligibleAchievements();
        }

        /// <summary>
        /// 激活符合条件的成就
        /// </summary>
        public static void ActivateEligibleAchievements(this AchievementComponent self)
        {
            // TODO: 从配置表获取所有成就，检查前置条件，激活符合条件的成就
        }

        /// <summary>
        /// 获取成就
        /// </summary>
        public static Achievement GetAchievement(this AchievementComponent self, int achievementId)
        {
            if (self.ActiveAchievements.TryGetValue(achievementId, out var achievementRef))
            {
                return achievementRef;
            }
            return null;
        }

        /// <summary>
        /// 添加成就
        /// </summary>
        public static Achievement AddAchievement(this AchievementComponent self, int configId)
        {
            Achievement achievement = self.AddChild<Achievement, int>(configId);
            self.ActiveAchievements[configId] = achievement;
            return achievement;
        }

        /// <summary>
        /// 更新成就进度
        /// </summary>
        public static void UpdateAchievementProgress(this AchievementComponent self, int achievementId, int progress)
        {
            Achievement achievement = self.GetAchievement(achievementId);
            if (achievement == null)
            {
                return;
            }

            int oldProgress = achievement.Progress;
            achievement.Progress = progress;
            self.AchievementProgress[achievementId] = progress;

            // 发布进度更新事件
            EventSystem.Instance.Publish(self.GetParent<Unit>().Scene(), new AchievementProgressEvent
            {
                UnitId = self.GetParent<Unit>().Id,
                AchievementId = achievementId,
                Progress = progress,
                MaxProgress = achievement.MaxProgress
            });

            // 检查是否完成
            if (achievement.Progress >= achievement.MaxProgress && achievement.Status == AchievementStatus.InProgress)
            {
                self.CompleteAchievement(achievementId);
            }
        }

        /// <summary>
        /// 完成成就
        /// </summary>
        public static void CompleteAchievement(this AchievementComponent self, int achievementId)
        {
            Achievement achievement = self.GetAchievement(achievementId);
            if (achievement == null || achievement.Status != AchievementStatus.InProgress)
            {
                return;
            }

            achievement.Status = AchievementStatus.Completed;
            achievement.CompleteTime = TimeInfo.Instance.ServerNow();
            self.CompletedAchievements.Add(achievementId);

            // 添加到最近完成列表
            self.RecentAchievements.Add(achievementId);
            if (self.RecentAchievements.Count > 10)
            {
                self.RecentAchievements.RemoveAt(0);
            }

            // 发布完成事件
            EventSystem.Instance.Publish(self.GetParent<Unit>().Scene(), new AchievementCompleteEvent
            {
                UnitId = self.GetParent<Unit>().Id,
                AchievementId = achievementId,
                CompleteTime = achievement.CompleteTime
            });

            Log.Debug($"Achievement completed: {achievementId}");
        }

        /// <summary>
        /// 领取成就奖励
        /// </summary>
        public static bool ClaimAchievementReward(this AchievementComponent self, int achievementId)
        {
            Achievement achievement = self.GetAchievement(achievementId);
            if (achievement == null || achievement.Status != AchievementStatus.Completed)
            {
                return false;
            }

            achievement.Status = AchievementStatus.Claimed;
            achievement.ClaimTime = TimeInfo.Instance.ServerNow();
            self.ClaimedAchievements.Add(achievementId);
            self.EarnedPoints += achievement.Points;

            // 发放奖励
            self.GrantAchievementReward(achievementId);

            // 发布领取事件
            EventSystem.Instance.Publish(self.GetParent<Unit>().Scene(), new AchievementClaimedEvent
            {
                UnitId = self.GetParent<Unit>().Id,
                AchievementId = achievementId,
                ClaimTime = achievement.ClaimTime
            });

            Log.Debug($"Achievement reward claimed: {achievementId}");
            return true;
        }

        /// <summary>
        /// 发放成就奖励
        /// </summary>
        public static void GrantAchievementReward(this AchievementComponent self, int achievementId)
        {
            // TODO: 根据成就配置发放奖励
            // 这里只做结构占位，具体实现需结合奖励系统
        }

        /// <summary>
        /// 获取成就列表
        /// </summary>
        public static List<AchievementInfo> GetAchievementList(this AchievementComponent self, int categoryId = 0)
        {
            List<AchievementInfo> result = new List<AchievementInfo>();

            foreach (var kvp in self.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null)
                {
                    continue;
                }

                if (categoryId > 0 && achievement.CategoryId != categoryId)
                {
                    continue;
                }

                AchievementInfo info = AchievementInfo.Create();
                info.AchievementId = achievement.ConfigId;
                info.Status = (int)achievement.Status;
                info.Progress = achievement.Progress;
                info.MaxProgress = achievement.MaxProgress;
                info.CompleteTime = achievement.CompleteTime;
                info.ClaimTime = achievement.ClaimTime;
                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 获取成就统计信息
        /// </summary>
        public static AchievementStatsInfo GetAchievementStats(this AchievementComponent self)
        {
            AchievementStatsInfo stats = AchievementStatsInfo.Create();
            stats.TotalAchievements = self.ActiveAchievements.Count;
            stats.CompletedAchievements = self.CompletedAchievements.Count;
            stats.TotalPoints = self.TotalPoints;
            stats.EarnedPoints = self.EarnedPoints;
            stats.RecentAchievements.AddRange(self.RecentAchievements);
            return stats;
        }

    }
}