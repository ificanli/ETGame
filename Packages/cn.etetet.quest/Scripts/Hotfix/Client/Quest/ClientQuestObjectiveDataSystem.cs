namespace ET.Client
{
    [Module(ModuleName.Quest)]
    [EntitySystemOf(typeof(ClientQuestObjectiveData))]
    public static partial class ClientQuestObjectiveDataSystem
    {
        [EntitySystem]
        private static void Awake(this ClientQuestObjectiveData self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this ClientQuestObjectiveData self)
        {
            
        }

        /// <summary>
        /// 获取进度百分比
        /// </summary>
        public static float GetProgressPercent(this ClientQuestObjectiveData self)
        {
            if (self.RequiredCount <= 0)
            {
                return 0f;
            }
            return (float)self.CurrentCount / self.RequiredCount;
        }

        /// <summary>
        /// 获取进度文本
        /// </summary>
        public static string GetProgressText(this ClientQuestObjectiveData self)
        {
            return $"{self.CurrentCount}/{self.RequiredCount}";
        }

        /// <summary>
        /// 更新进度
        /// </summary>
        public static void UpdateProgress(this ClientQuestObjectiveData self, int currentCount)
        {
            self.CurrentCount = currentCount;
            self.IsCompleted = currentCount >= self.RequiredCount;
        }
    }
}