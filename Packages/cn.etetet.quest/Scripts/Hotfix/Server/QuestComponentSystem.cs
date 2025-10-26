using System;
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

        public static HashSet<EntityRef<QuestObjective>> GetQuestObjectiveByType(this QuestComponent self, QuestObjectiveType questObjectiveType)
        {
            HashSet<EntityRef<QuestObjective>> questObjectives;
            self.QuestObjectives.TryGetValue(questObjectiveType, out questObjectives);
            return questObjectives;
        }
        
        // 获取任务
        public static Quest GetQuest(this QuestComponent self, long questId)
        {
            return self.GetChild<Quest>(questId);
        }

        // 判断前置任务是否完成
        public static bool IsPreQuestFinished(this QuestComponent self, long questId)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get((int)questId);
            foreach (int preQuestId in questConfig.PreQuestIds)
            {
                if (!self.FinishedQuests.Contains(preQuestId))
                {
                    return false;
                }
            }
            return true;
        }
        
        public static Quest AddQuest(this QuestComponent self, int questId)
        {
            Quest quest = self.AddChildWithId<Quest>(questId);

            foreach (KeyValuePair<long, Entity> kv in quest.Children)
            {
                QuestObjective questObjective = kv.Value as QuestObjective;
                self.QuestObjectives.Add(questObjective.GetConfig().Type, questObjective);
            }
            
            return quest;
        }

        // 尝试完成任务
        public static bool TryFinishQuest(this QuestComponent self, long questId)
        {
            Quest quest = self.GetQuest(questId);
            if (!quest.IsFinished())
            {
                return false;
            }

            self.FinishedQuests.Add((int)quest.Id);
            
            self.RemoveQuest(questId);
            return true;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public static void RemoveQuest(this QuestComponent self, long questId)
        {
            Quest quest = self.GetChild<Quest>(questId);
            
            // 检查所有目标是否完成
            // 检查所有目标是否完成
            foreach (var kv in quest.Children)
            {
                QuestObjective questObjective = kv.Value as QuestObjective;
                if (questObjective == null)
                {
                    continue;
                }

                QuestObjectiveConfig questObjectiveConfig = questObjective.GetConfig();
                self.QuestObjectives.Remove(questObjectiveConfig.Type, questObjective);
            }
            
            self.RemoveChild(quest.Id);
        }
    }
}