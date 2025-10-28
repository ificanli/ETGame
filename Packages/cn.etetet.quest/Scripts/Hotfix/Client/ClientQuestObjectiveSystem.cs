namespace ET.Client
{
    [EntitySystemOf(typeof(ClientQuestObjective))]
    public static partial class ClientQuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this ClientQuestObjective self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this ClientQuestObjective self)
        {
            
        }
        
        public static bool IsFinished(this ClientQuestObjective self)
        {
            return self.Count >= self.NeedCount;
        }

        /// <summary>
        /// 获取进度百分比
        /// </summary>
        public static float GetProgressPercent(this ClientQuestObjective self)
        {
            if (self.NeedCount <= 0)
            {
                return 0f;
            }
            return (float)self.Count / self.NeedCount;
        }

        /// <summary>
        /// 获取进度文本
        /// </summary>
        public static string GetProgressText(this ClientQuestObjective self)
        {
            return $"{self.Count}/{self.NeedCount}";
        }
    }
}