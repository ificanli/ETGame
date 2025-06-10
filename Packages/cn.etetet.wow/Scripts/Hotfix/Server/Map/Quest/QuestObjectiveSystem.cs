namespace ET.Server
{
    [EntitySystemOf(typeof(QuestObjective))]
    public static partial class QuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this QuestObjective self, int configId)
        {
            self.ConfigId = configId;

            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();

            // 根据不同的类型添加不同的组件来记录任务数据
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveConfig.Type);
            questObjectiveHandler.Init(self);

            self.Scene<QuestComponent>().QuestObjectives.Add(questObjectiveConfig.Type, self);
        }
        
        [EntitySystem]
        private static void Destroy(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            self.Scene<QuestComponent>()?.QuestObjectives.Remove(questObjectiveConfig.Type, self);
        }

        public static void Process(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveConfig.Type);
            questObjectiveHandler.Process();
        }

        public static QuestObjectiveConfig GetConfig(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = QuestObjectiveConfigCategory.Instance.Get(self.ConfigId);
            return questObjectiveConfig;
        }

        public static bool IsFinished(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveConfig.Type);
            return questObjectiveHandler.IsFinished();
        }
    }
}