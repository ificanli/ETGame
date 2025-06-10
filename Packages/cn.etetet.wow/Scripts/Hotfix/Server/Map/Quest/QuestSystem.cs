using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(Quest))]
    public static partial class QuestSystem
    {
        [EntitySystem]
        private static void Awake(this Quest self, int configId)
        {
            self.ConfigId = configId;
            QuestConfig questConfig = self.GetConfig();
            foreach (int objectiveId in questConfig.ObjectiveIds)
            {
                self.AddChild<QuestObjective, int>(objectiveId);
            }
        }
        
        public static QuestConfig GetConfig(this Quest self)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get(self.ConfigId);
            return questConfig;
        }

        public static bool IsFinished(this Quest self)
        {
            foreach (KeyValuePair<long, Entity> pair in self.Children)
            {
                QuestObjective objective = (QuestObjective)pair.Value;
                if (!objective.IsFinished())
                {
                    return false;
                }
            }
            return true;
        }
        
        public static void Process(this Quest self, QuestObjectiveType questObjectiveType)
        {
            foreach (KeyValuePair<long, Entity> pair in self.Children)
            {
                QuestObjective objective = (QuestObjective)pair.Value;
                
            }
        }
    }
}