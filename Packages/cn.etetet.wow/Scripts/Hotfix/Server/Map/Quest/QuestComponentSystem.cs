using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(QuestComponent))]
    public static partial class QuestComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this QuestComponent self)
        {

        }
        [EntitySystem]
        private static void Awake(this QuestComponent self)
        {
            
        }

        // 任务目标前进,订阅各种事件，然后调用这个。比如杀一只怪，调用一下，采集调用一下
        public static void Process(this QuestComponent self, QuestObjectiveType questObjectiveType)
        {
            var questObjectives = self.GetQuestObjective(questObjectiveType);
            foreach (EntityRef<QuestObjective> questObjectiveRef in questObjectives)
            {
                QuestObjective questObjective = questObjectiveRef;
                questObjective.Process();
            }
        }

        private static HashSet<EntityRef<QuestObjective>> GetQuestObjective(this QuestComponent self, QuestObjectiveType questObjectiveType)
        {
            HashSet<EntityRef<QuestObjective>> questObjectives;
            self.QuestObjectives.TryGetValue(questObjectiveType, out questObjectives);
            return questObjectives;
        }
        
        // 获取任务
        public static Quest GetQuest(this QuestComponent self, int questId)
        {
            return self.GetChild<Quest>(questId);
        }

        // 判断前置任务是否完成
        public static bool IsPreQuestFinished(this QuestComponent self, int questId)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get(questId);
            foreach (int preQuestId in questConfig.PreQuestIds)
            {
                if (!self.FinishedQuests.Contains(preQuestId))
                {
                    return false;
                }
            }
            return true;
        }
        
        public static Quest AddQuest(this QuestComponent self, int configId)
        {
            Quest quest = self.AddChild<Quest, int>(configId);
            return quest;
        }

        // 尝试完成任务
        public static bool TryFinishQuest(this QuestComponent self, int configId)
        {
            Quest quest = self.GetChild<Quest>(configId);
            if (!quest.IsFinished())
            {
                return false;
            }

            self.FinishedQuests.Add((int)quest.Id);
            self.RemoveChild(quest.Id);
            return true;
        }
    }
}