using System;

namespace ET.Server
{
    public static class QuestHelper
    {
        public static void AddQuest(Unit self, long questId)
        {
            Quest quest = self.GetComponent<QuestComponent>().AddQuest(questId);
            
            // 通知任务
            M2C_CreateQuest createQuest = M2C_CreateQuest.Create();
            foreach (var kv in quest.Children)
            {
                QuestObjective objective = (QuestObjective)kv.Value;
                QuestObjectiveConfig objectiveConfig = objective.GetConfig();
                QuestObjectiveInfo questObjectiveInfo = QuestObjectiveInfo.Create();
                questObjectiveInfo.QuestObjectiveId = objectiveConfig.Id;
                createQuest.QuestObjective.Add(questObjectiveInfo);
            }
            
            MapMessageHelper.NoticeClient(self, createQuest, NoticeType.Self);
        }
        
        public static void SubmitQuest(Unit self, long questId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            if (!questComponent.TrySubmitQuest(questId))
            {
                return;
            }
            
            // 通知任务完成
            M2C_UpdateQuest updateQuest = M2C_UpdateQuest.Create();
            updateQuest.QuestId = questId;
            updateQuest.State = (int)QuestStatus.Submited;
            
            MapMessageHelper.NoticeClient(self, updateQuest, NoticeType.Self);
            
            // 触发新可接任务
        }

        /// <summary>
        /// 查找指定任务Id的任务
        /// </summary>
        public static Quest GetQuest(Unit self, long questId)
        {
            return self.GetComponent<QuestComponent>().GetQuest(questId);
        }

        
        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateObjectiveCount(QuestObjective objective)
        {
            QuestObjectiveConfig questObjectiveConfig = objective.GetConfig();
            if (objective.Count >= questObjectiveConfig.NeedCount)
            {
                return;
            }

            // 获取玩家Unit并通知进度更新
            Quest quest = objective.GetParent<Quest>();
            Unit player = quest.GetParent<QuestComponent>().GetParent<Unit>();
            
            M2C_UpdateQuestObjective message = M2C_UpdateQuestObjective.Create();
            message.QuestId = quest.Id;
            message.QuestObjectiveId = objective.Id;
            message.Count = objective.Count;

            MapMessageHelper.NoticeClient(player, message, NoticeType.Self);
        }
        

        /// <summary>
        /// 检查任务是否可以接取
        /// </summary>
        public static bool CanAcceptQuest(Unit unit, long questId)
        {
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();

            // 检查是否已经有此任务
            if (questComponent.GetQuest(questId) != null)
            {
                return false;
            }

            // 检查是否已经完成过此任务
            if (questComponent.FinishedQuests.Contains((int)questId))
            {
                return false;
            }

            // 获取任务配置
            var questConfig = QuestConfigCategory.Instance.Get((int)questId);

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

            return true;
        }
    }
}