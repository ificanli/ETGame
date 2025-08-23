using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 成就帮助类
    /// </summary>
    public static class AchievementHelper
    {
        /// <summary>
        /// 直接处理击杀怪物成就逻辑
        /// </summary>
        public static void ProcessKillMonsterAchievement(Unit unit, int monsterId, int count = 1)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有击杀类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Kill)
                {
                    continue;
                }

                // 检查是否是目标怪物（这里简化处理，实际应该从配置表获取）
                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + count;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Kill monster achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }
        }

        /// <summary>
        /// 直接处理等级提升成就逻辑
        /// </summary>
        public static void ProcessLevelUpAchievement(Unit unit, int level)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有等级类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Level)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    // 等级成就以达到目标等级为条件
                    if (level >= achievement.MaxProgress)
                    {
                        achievementComponent.UpdateAchievementProgress(achievement.ConfigId, achievement.MaxProgress);
                    }
                    else
                    {
                        achievementComponent.UpdateAchievementProgress(achievement.ConfigId, level);
                    }
                    
                    Log.Debug($"Level achievement progress updated: {achievement.ConfigId}, level: {level}");
                }
            }
        }

        /// <summary>
        /// 直接处理任务完成成就逻辑
        /// </summary>
        public static void ProcessQuestCompleteAchievement(Unit unit, int questId)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有任务类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Quest)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + 1;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Quest achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }
        }

        /// <summary>
        /// 直接处理道具收集成就逻辑
        /// </summary>
        public static void ProcessItemCollectAchievement(Unit unit, int itemId, int count)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有收集类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Collect)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + count;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Collect achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }
        }

        /// <summary>
        /// 直接处理地图探索成就逻辑
        /// </summary>
        public static void ProcessMapExploreAchievement(Unit unit, int mapId)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有探索类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Exploration)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + 1;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Exploration achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }
        }

        /// <summary>
        /// 直接处理进入地图成就逻辑
        /// </summary>
        public static void ProcessEnterMapAchievement(Unit unit, int mapId)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return;
            }

            // 遍历所有探索类型的成就
            foreach (var kvp in achievementComponent.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null || achievement.Type != AchievementType.Exploration)
                {
                    continue;
                }

                if (achievement.Status == AchievementStatus.InProgress)
                {
                    int newProgress = achievement.Progress + 1;
                    achievementComponent.UpdateAchievementProgress(achievement.ConfigId, newProgress);
                    
                    Log.Debug($"Enter map achievement progress updated: {achievement.ConfigId}, progress: {achievement.Progress}/{achievement.MaxProgress}");
                }
            }
        }

        /// <summary>
        /// 检查成就前置条件
        /// </summary>
        public static bool CheckAchievementPrerequisites(Unit unit, int achievementId)
        {
            AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
            if (achievementComponent == null)
            {
                return false;
            }

            Achievement achievement = achievementComponent.GetAchievement(achievementId);
            if (achievement == null)
            {
                return false;
            }

            return achievement.CheckPreAchievements(achievementComponent);
        }

        /// <summary>
        /// 获取成就奖励预览
        /// </summary>
        public static List<AchievementReward> GetAchievementRewards(int achievementId)
        {
            List<AchievementReward> rewards = new List<AchievementReward>();

            // TODO: 从配置表获取奖励信息
            // AchievementConfig config = AchievementConfigCategory.Instance.Get(achievementId);
            // if (config != null)
            // {
            //     foreach (var rewardConfig in config.Rewards)
            //     {
            //         rewards.Add(new AchievementReward
            //         {
            //             Type = rewardConfig.Type,
            //             ItemId = rewardConfig.ItemId,
            //             Count = rewardConfig.Count
            //         });
            //     }
            // }

            return rewards;
        }

        /// <summary>
        /// 格式化成就进度文本
        /// </summary>
        public static string FormatProgressText(int progress, int maxProgress)
        {
            return $"{progress}/{maxProgress}";
        }

        /// <summary>
        /// 计算成就完成率
        /// </summary>
        public static float CalculateCompletionRate(int completed, int total)
        {
            return total > 0 ? (float)completed / total : 0f;
        }

        /// <summary>
        /// 检查成就是否可见
        /// </summary>
        public static bool IsAchievementVisible(Unit unit, int achievementId)
        {
            // TODO: 从配置表获取成就信息，检查是否隐藏成就
            // AchievementConfig config = AchievementConfigCategory.Instance.Get(achievementId);
            // if (config != null && config.IsHidden)
            // {
            //     // 隐藏成就需要特定条件才能显示
            //     return CheckHiddenAchievementVisibility(unit, achievementId);
            // }
            return true;
        }

        /// <summary>
        /// 检查隐藏成就可见性
        /// </summary>
        private static bool CheckHiddenAchievementVisibility(Unit unit, int achievementId)
        {
            // TODO: 实现隐藏成就的可见性逻辑
            return false;
        }
    }
}