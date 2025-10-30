using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(Quest))]
    public static partial class QuestSystem
    {
        [EntitySystem]
        private static void Awake(this Quest self)
        {
            QuestConfig questConfig = self.GetConfig();
            foreach (int objectiveId in questConfig.ObjectiveIds)
            {
                self.AddChildWithId<QuestObjective>(objectiveId);
            }
        }
        
        public static QuestConfig GetConfig(this Quest self)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get((int)self.Id);
            return questConfig;
        }

        public static bool CanSubmit(this Quest self)
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

        /// <summary>
        /// 查找指定目标Id的任务目标
        /// </summary>
        public static QuestObjective GetQuestObjective(this Quest self, int objectiveId)
        {
            return self.GetChild<QuestObjective>(objectiveId);
        }
        
        public static List<QuestObjective> GetQuestObjectiveByType(this Quest self, QuestObjectiveType type)
        {
            List<QuestObjective> list = new();
            foreach (KeyValuePair<long, Entity> pair in self.Children)
            {
                QuestObjective objective = (QuestObjective)pair.Value;
                QuestObjectiveConfig config = objective.GetConfig();
                if (config.Type == type)
                {
                    list.Add(objective);
                }
            }
            return list;
        }
    }
}