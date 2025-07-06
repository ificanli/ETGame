using System.Collections.Generic;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
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

        /// <summary>
        /// 查找指定目标Id的任务目标
        /// </summary>
        public static QuestObjective FindObjective(this Quest self, int objectiveId)
        {
            foreach (KeyValuePair<long, Entity> pair in self.Children)
            {
                QuestObjective objective = (QuestObjective)pair.Value;
                if (objective.ConfigId == objectiveId)
                {
                    return objective;
                }
            }
            return null;
        }

        /// <summary>
        /// 校验任务接取条件
        /// </summary>
        public static bool CheckAcceptCondition(this Quest self, Unit unit)
        {
            // TODO: 根据self.AcceptConditionIds校验，需结合条件系统
            return true;
        }

        /// <summary>
        /// 校验任务提交条件
        /// </summary>
        public static bool CheckSubmitCondition(this Quest self, Unit unit)
        {
            // TODO: 根据self.SubmitConditionIds校验，需结合条件系统
            return true;
        }

        /// <summary>
        /// 发放任务奖励
        /// </summary>
        public static void GrantReward(this Quest self, Unit unit)
        {
            // TODO: 根据self.RewardIds发放奖励，需结合奖励系统
        }

        /// <summary>
        /// 推进任务链，解锁后续任务
        /// </summary>
        public static void AdvanceChain(this Quest self, QuestComponent questComponent)
        {
            QuestConfig questConfig = self.GetConfig();
            foreach (int nextQuestId in questConfig.NextQuestId)
            {
                questComponent.AvailableQuests.Add(nextQuestId);
            }
        }
    }
}