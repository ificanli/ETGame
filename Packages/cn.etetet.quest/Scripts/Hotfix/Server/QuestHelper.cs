using System.Collections.Generic;
using System.Threading.Tasks;

namespace ET.Server
{
    public static class QuestHelper
    {
        public static void AddQuest(Unit self, int configId)
        {
            Quest quest = self.GetComponent<QuestComponent>().AddQuest(configId);
            
            // 通知任务
            M2C_CreateQuest createQuest = M2C_CreateQuest.Create();
            foreach (var kv in quest.Children)
            {
                QuestObjective objective = (QuestObjective)kv.Value;
                QuestObjectiveConfig objectiveConfig = objective.GetConfig();
                QuestObjectiveInfo questObjectiveInfo = QuestObjectiveInfo.Create();
                questObjectiveInfo.QuestObjectiveId = objectiveConfig.Id;
                questObjectiveInfo.NeedCount = objectiveConfig.NeedCount;
                createQuest.QuestObjective.Add(questObjectiveInfo);
            }
            
            MapMessageHelper.NoticeClient(self, createQuest, NoticeType.Self);
        }
        
        public static void FinishQuest(Unit self, int questId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            if (!questComponent.TryFinishQuest(questId))
            {
                return;
            }
            
            // 通知任务完成
            M2C_UpdateQuest updateQuest = M2C_UpdateQuest.Create();
            updateQuest.QuestId = questId;
            updateQuest.State = (int)QuestStatus.Finished;
            
            MapMessageHelper.NoticeClient(self, updateQuest, NoticeType.Self);
        }

        /// <summary>
        /// 查找指定任务Id的任务
        /// </summary>
        public static Quest GetQuest(Unit self, int questId)
        {
            return self.GetComponent<QuestComponent>().GetQuest(questId);
        }

        /// <summary>
        /// 校验任务是否可接取
        /// </summary>
        public static bool CheckQuestAcceptable(Unit self, int questId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            return !questComponent.FinishedQuests.Contains(questId) && questComponent.IsPreQuestFinished(questId);
        }

        /// <summary>
        /// 校验任务是否可提交
        /// </summary>
        public static bool CheckQuestSubmittable(Unit self, int questId)
        {
            Quest quest = self.GetComponent<QuestComponent>().GetQuest(questId);
            return quest != null && quest.IsFinished();
        }

        /// <summary>
        /// 发放任务奖励
        /// </summary>
        public static void GrantQuestReward(Unit self, int questId)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return;
            }
            
            Log.Debug($"Quest {questId} rewards granted to player {self.Id}");
        }

        /// <summary>
        /// 处理怪物击杀事件
        /// </summary>
        public static void OnMonsterKilled(Unit self, int monsterId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            questComponent.Process(QuestObjectiveType.KillMonster, monsterId);
        }

        /// <summary>
        /// 处理物品收集事件
        /// </summary>
        public static void OnItemCollected(Unit self, int itemId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            questComponent.Process(QuestObjectiveType.Collectltem, itemId);
        }


        #region 通知方法
        
        /// <summary>
        /// 通知任务目标完成
        /// </summary>
        private static void NotifyQuestObjectiveUpdate(Unit self, int questId, int objectiveId)
        {
            Quest quest = self.GetComponent<QuestComponent>().GetQuest(questId);
            if (quest == null) return;
            
            M2C_UpdateQuestObjective message = M2C_UpdateQuestObjective.Create();
            message.QuestId = questId;

            QuestObjective objective = quest.GetQuestObjective(objectiveId);
            
            QuestObjectiveInfo info = QuestObjectiveInfo.Create();
            info.QuestObjectiveId = (int)objective.Id;
            info.Count = objective.Count;
            info.NeedCount = objective.GetConfig().NeedCount;
            message.QuestObjective.Add(info);
            
            MapMessageHelper.NoticeClient(self, message, NoticeType.Self);
        }

        /// <summary>
        /// 通知任务可提交
        /// </summary>
        public static void NotifyQuestCanSubmit(Unit self, int questId)
        {
            M2C_UpdateQuest message = M2C_UpdateQuest.Create();
            message.QuestId = questId;
            message.State = (int)QuestStatus.CanSubmit;
            
            MapMessageHelper.NoticeClient(self, message, NoticeType.Self);
        }
        
        #endregion

        #region 消息处理器所需方法

        /// <summary>
        /// 检查任务是否可以接取
        /// </summary>
        public static async ETTask<bool> CanAcceptQuest(Unit unit, int questId)
        {
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                return false;
            }

            // 检查是否已经有此任务
            if (questComponent.GetQuest(questId) != null)
            {
                return false;
            }

            // 检查是否已经完成过此任务
            if (questComponent.FinishedQuests.Contains(questId))
            {
                return false;
            }

            // 获取任务配置
            var questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return false;
            }

            // 检查等级要求
            // TODO: 获取玩家等级进行检查
            // if (unit.GetComponent<LevelComponent>().Level < questConfig.MinLevel)
            // {
            //     return false;
            // }

            // 检查前置任务
            if (questConfig.PreQuestIds != null)
            {
                foreach (int preQuestId in questConfig.PreQuestIds)
                {
                    if (!questComponent.FinishedQuests.Contains(preQuestId))
                    {
                        return false;
                    }
                }
            }

            await ETTask.CompletedTask;
            return true;
        }

        /// <summary>
        /// 检查NPC是否正确
        /// </summary>
        public static bool IsCorrectAcceptNPC(int questId, int npcId)
        {
            var questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return false;
            }

            return questConfig.AcceptNPC == npcId;
        }

        /// <summary>
        /// 检查是否正确的提交NPC
        /// </summary>
        public static bool IsCorrectSubmitNPC(int questId, int npcId)
        {
            var questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return false;
            }

            return questConfig.SubmitNPC == npcId;
        }


        #endregion
    }
}