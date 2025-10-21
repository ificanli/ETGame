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
        public static void Process(this QuestComponent self, QuestObjectiveType questObjectiveType, int p)
        {
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveType);
            
            var questObjectives = self.GetQuestObjectiveByType(questObjectiveType);
            foreach (EntityRef<QuestObjective> questObjectiveRef in questObjectives)
            {
                QuestObjective questObjective = questObjectiveRef;
                questObjectiveHandler.Process(questObjective, p);
            }
        }

        public static HashSet<EntityRef<QuestObjective>> GetQuestObjectiveByType(this QuestComponent self, QuestObjectiveType questObjectiveType)
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
            Quest quest = self.AddChildWithId<Quest>(configId);

            foreach (KeyValuePair<long, Entity> kv in quest.Children)
            {
                QuestObjective questObjective = kv.Value as QuestObjective;
                self.QuestObjectives.Add(questObjective.GetConfig().Type, questObjective);
            }
            
            return quest;
        }

        // 尝试完成任务
        public static bool TryFinishQuest(this QuestComponent self, int questId)
        {
            Quest quest = self.GetQuest(questId);
            if (!quest.IsFinished())
            {
                return false;
            }

            self.FinishedQuests.Add((int)quest.Id);
            self.RemoveChild(quest.Id);
            return true;
        }

        /// <summary>
        /// 接取任务
        /// </summary>
        public static Quest AcceptQuest(this QuestComponent self, int questId)
        {
            // 检查是否已完成或已接取
            if (self.FinishedQuests.Contains(questId) || self.GetQuest(questId) != null)
            {
                return null;
            }
            // 检查前置任务
            if (!self.IsPreQuestFinished(questId))
            {
                return null;
            }
            // 创建任务实体
            Quest quest = self.AddChildWithId<Quest>(questId);
            // 可扩展：初始化目标、事件订阅等
            return quest;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public static void RemoveQuest(this QuestComponent self, int questId)
        {
            Quest quest = self.GetChild<Quest>(questId);
            if (quest != null)
            {
                self.RemoveChild(quest.Id);
            }
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public static bool TryCompleteQuest(this QuestComponent self, int questId)
        {
            if (self.FinishedQuests.Contains(questId))
            {
                return true;
            }
            
            Quest quest = self.GetQuest(questId);
            if (quest == null)
            {
                return false;
            }
            
            // 检查所有目标是否完成
            foreach (int objectiveId in quest.GetConfig().ObjectiveIds)
            {
                QuestObjective questObjective = quest.GetChild<QuestObjective>(objectiveId);
                if (questObjective == null)
                {
                    return false;
                }

                if (!questObjective.IsCompleted)
                {
                    return false;
                }
            }
            
            quest.Status = QuestStatus.Finished;
            self.FinishedQuests.Add(questId);
            // 移除任务实体
            self.RemoveChild(quest.Id);
            return true;
        }
    }
}