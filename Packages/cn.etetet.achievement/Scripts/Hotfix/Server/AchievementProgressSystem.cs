using System;

namespace ET.Server
{
    [EntitySystemOf(typeof(AchievementProgress))]
    public static partial class AchievementProgressSystem
    {
        [EntitySystem]
        private static void Awake(this AchievementProgress self, int achievementId, int maxProgress)
        {
            self.AchievementId = achievementId;
            self.MaxProgress = maxProgress;
            self.Progress = 0;
            self.LastUpdateTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public static void UpdateProgress(this AchievementProgress self, int progress)
        {
            int oldProgress = self.Progress;
            self.Progress = Math.Min(progress, self.MaxProgress);
            self.LastUpdateTime = TimeInfo.Instance.ServerNow();

            // 如果进度有变化，记录日志
            if (oldProgress != self.Progress)
            {
                Log.Debug($"Achievement progress updated: {self.AchievementId} from {oldProgress} to {self.Progress}");
            }
        }

        /// <summary>
        /// 增加进度
        /// </summary>
        public static void AddProgress(this AchievementProgress self, int value)
        {
            self.UpdateProgress(self.Progress + value);
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public static void Reset(this AchievementProgress self)
        {
            self.Progress = 0;
            self.LastUpdateTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 检查是否完成
        /// </summary>
        public static bool CheckComplete(this AchievementProgress self)
        {
            return self.IsCompleted;
        }

        /// <summary>
        /// 获取进度信息
        /// </summary>
        public static string GetProgressText(this AchievementProgress self)
        {
            return $"{self.Progress}/{self.MaxProgress}";
        }
    }
}