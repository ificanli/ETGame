using System.Collections.Generic;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
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

        public static HashSet<EntityRef<QuestObjective>> GetQuestObjective(this QuestComponent self, QuestObjectiveType questObjectiveType)
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

        /// <summary>
        /// 接取任务
        /// </summary>
        public static Quest AcceptQuest(this QuestComponent self, int questConfigId)
        {
            // 检查是否已完成或已接取
            if (self.FinishedQuests.Contains(questConfigId) || self.ActiveQuests.ContainsKey(questConfigId))
            {
                return null;
            }
            // 检查前置任务
            if (!self.IsPreQuestFinished(questConfigId))
            {
                return null;
            }
            // 创建任务实体
            Quest quest = self.AddChild<Quest, int>(questConfigId);
            self.ActiveQuests[questConfigId] = quest;
            // 初始化进度
            self.QuestProgressDict[questConfigId] = new Dictionary<int, int>();
            // 可扩展：初始化目标、事件订阅等
            return quest;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public static void AbandonQuest(this QuestComponent self, int questConfigId)
        {
            if (!self.ActiveQuests.TryGetValue(questConfigId, out var questRef))
            {
                return;
            }
            Quest quest = questRef;
            if (quest != null)
            {
                quest.Status = QuestStatus.Abandoned;
                self.RemoveChild(quest.Id);
            }
            self.ActiveQuests.Remove(questConfigId);
            self.QuestProgressDict.Remove(questConfigId);
        }

        /// <summary>
        /// 更新任务进度
        /// </summary>
        public static void UpdateQuestProgress(this QuestComponent self, int questConfigId, int objectiveId, int progress)
        {
            if (!self.QuestProgressDict.TryGetValue(questConfigId, out var progressDict))
            {
                return;
            }
            progressDict[objectiveId] = progress;
            // 可扩展：进度变更事件、完成检测
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public static bool CompleteQuest(this QuestComponent self, int questConfigId)
        {
            if (!self.ActiveQuests.TryGetValue(questConfigId, out var questRef))
            {
                return false;
            }
            Quest quest = questRef;
            if (quest == null)
            {
                return false;
            }
            // 检查所有目标是否完成
            bool allFinished = true;
            foreach (int objectiveId in quest.ObjectiveIds)
            {
                int needCount = 1;
                // 通过配置获取目标所需数量
                var objectiveConfig = QuestObjectiveConfigCategory.Instance.GetOrDefault(objectiveId);
                if (objectiveConfig != null)
                {
                    needCount = objectiveConfig.NeedCount;
                }
                if (!self.QuestProgressDict.TryGetValue(questConfigId, out var progressDict) ||
                    !progressDict.TryGetValue(objectiveId, out int value) ||
                    value < needCount)
                {
                    allFinished = false;
                    break;
                }
            }
            if (!allFinished)
            {
                return false;
            }
            quest.Status = QuestStatus.Finished;
            self.FinishedQuests.Add(questConfigId);
            self.ActiveQuests.Remove(questConfigId);
            self.QuestProgressDict.Remove(questConfigId);
            // 发放奖励
            self.GrantQuestReward(questConfigId);
            // 推进任务链
            self.AdvanceQuestChain(questConfigId);
            // 移除任务实体
            self.RemoveChild(quest.Id);
            return true;
        }

        /// <summary>
        /// 发放任务奖励
        /// </summary>
        public static void GrantQuestReward(this QuestComponent self, int questConfigId)
        {
            // TODO: 根据QuestConfig奖励字段发放奖励
            // 这里只做结构占位，具体实现需结合奖励系统
        }

        /// <summary>
        /// 推进任务链，解锁后续任务
        /// </summary>
        public static void AdvanceQuestChain(this QuestComponent self, int questConfigId)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get(questConfigId);
            foreach (int nextQuestId in questConfig.NextQuestId)
            {
                self.AvailableQuests.Add(nextQuestId);
            }
        }
    }
}