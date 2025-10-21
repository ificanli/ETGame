namespace ET.Server
{
    [EntitySystemOf(typeof(QuestObjective))]
    public static partial class QuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();

            self.Scene<QuestComponent>().QuestObjectives.Add(questObjectiveConfig.Type, self);
        }
        
        [EntitySystem]
        private static void Destroy(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            self.Scene<QuestComponent>()?.QuestObjectives.Remove(questObjectiveConfig.Type, self);
        }

        public static QuestObjectiveConfig GetConfig(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = QuestObjectiveConfigCategory.Instance.Get((int)self.Id);
            return questObjectiveConfig;
        }

        public static bool IsFinished(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            return self.Count >= questObjectiveConfig.NeedCount;
        }
    }
}