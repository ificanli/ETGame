using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 成就帮助类
    /// </summary>
    public static class AchievementHelper
    {
        /// <summary>
        /// 触发击杀怪物事件
        /// </summary>
        public static void TriggerKillMonster(Unit unit, int monsterId, int count = 1)
        {
            EventSystem.Instance.Publish(unit.Scene(), new KillMonsterEvent
            {
                UnitId = unit.Id,
                MonsterId = monsterId,
                Count = count
            });
        }

        /// <summary>
        /// 触发等级提升事件
        /// </summary>
        public static void TriggerLevelUp(Unit unit, int level)
        {
            EventSystem.Instance.Publish(unit.Scene(), new LevelUpEvent
            {
                UnitId = unit.Id,
                Level = level
            });
        }

        /// <summary>
        /// 触发任务完成事件
        /// </summary>
        public static void TriggerQuestComplete(Unit unit, int questId)
        {
            EventSystem.Instance.Publish(unit.Scene(), new QuestCompleteEvent
            {
                UnitId = unit.Id,
                QuestId = questId
            });
        }

        /// <summary>
        /// 触发道具收集事件
        /// </summary>
        public static void TriggerItemCollect(Unit unit, int itemId, int count)
        {
            EventSystem.Instance.Publish(unit.Scene(), new ItemCollectEvent
            {
                UnitId = unit.Id,
                ItemId = itemId,
                Count = count
            });
        }

        /// <summary>
        /// 触发地图探索事件
        /// </summary>
        public static void TriggerMapExplore(Unit unit, int mapId)
        {
            EventSystem.Instance.Publish(unit.Scene(), new MapExploreEvent
            {
                UnitId = unit.Id,
                MapId = mapId
            });
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