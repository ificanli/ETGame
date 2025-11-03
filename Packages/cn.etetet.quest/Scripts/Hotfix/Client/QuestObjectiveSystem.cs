namespace ET.Client
{
    [EntitySystemOf(typeof(QuestObjective))]
    public static partial class QuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this QuestObjective self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this QuestObjective self)
        {

        }

        public static bool IsFinished(this QuestObjective self)
        {
            return self.Count >= self.NeedCount;
        }

        /// <summary>
        /// 获取进度百分比
        /// </summary>
        public static float GetProgressPercent(this QuestObjective self)
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
        public static string GetProgressText(this QuestObjective self)
        {
            return $"{self.Count}/{self.NeedCount}";
        }
    }
}