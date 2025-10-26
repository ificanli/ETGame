using System;

namespace ET.Server
{
    /// <summary>
    /// 任务事件系统 - 处理各种游戏事件对任务进度的影响
    /// </summary>
    public static class QuestEventSystem
    {
        /// <summary>
        /// 处理怪物击杀事件
        /// </summary>
        public static void OnMonsterKilled(Unit player, int monsterId, int count)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();

            // 触发击杀类型任务的进度检查
            var questObjectives = questComponent.GetQuestObjectiveByType(QuestObjectiveType.KillMonster);
            if (questObjectives == null)
            {
                return;
            }

            foreach (QuestObjective questObjective in questObjectives)
            {
                if (questObjective == null)
                {
                    continue;
                }
                QuestObjectiveConfig questObjectiveConfig = questObjective.GetConfig();
                    
                if (questObjective.Count >= questObjectiveConfig.NeedCount)
                {
                    return;
                }
                    
                QuestObjectiveParams_KillMonster questObjectiveParams = (QuestObjectiveParams_KillMonster)questObjectiveConfig.Params;
                // 检查是否是目标怪物
                if (questObjectiveParams.MonsterId != monsterId)
                {
                    continue;
                }

                questObjective.Count += count;
                UpdateObjectiveProgress(questObjective);
            }
        }

        /// <summary>
        /// 处理物品收集事件
        /// </summary>
        public static void OnItemCollected(Unit player, int itemId, int count)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            
            var questObjectives = questComponent.GetQuestObjectiveByType(QuestObjectiveType.Collectltem);
            if (questObjectives == null)
            {
                return;
            }
            
            foreach (QuestObjective questObjective in questObjectives)
            {
                if (questObjective == null)
                {
                    continue;
                }

                QuestObjectiveConfig questObjectiveConfig = questObjective.GetConfig();
                if (questObjective.Count >= questObjectiveConfig.NeedCount)
                {
                    continue;
                }
                
                QuestObjectiveParams_CollectItem questObjectiveParamsCollectItem = questObjectiveConfig.Params as QuestObjectiveParams_CollectItem;
                if (questObjectiveParamsCollectItem.ItemId != itemId)
                {
                    continue;
                }

                questObjective.Count += count;
                UpdateObjectiveProgress(questObjective);

            }
        }
        
        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        private static void UpdateObjectiveProgress(QuestObjective objective)
        {
            QuestObjectiveConfig questObjectiveConfig = objective.GetConfig();
            if (objective.Count >= questObjectiveConfig.NeedCount)
            {
                return;
            }

            // 获取玩家Unit并通知进度更新
            Quest quest = objective.GetParent<Quest>();
            Unit player = quest.GetParent<QuestComponent>().GetParent<Unit>();
            NotifyQuestProgress(player, objective);
            
            // 检查整个任务是否完成
            if (quest.IsFinished())
            {
                quest.Status = QuestStatus.CanSubmit;
                QuestHelper.NotifyQuestCanSubmit(player, (int)quest.Id);
            }
        }

        /// <summary>
        /// 通知任务进度更新
        /// </summary>
        private static void NotifyQuestProgress(Unit player, QuestObjective objective)
        {
            Quest quest = objective.GetParent<Quest>();
            if (quest == null)
            {
                return;
            }

            M2C_UpdateQuestObjective message = M2C_UpdateQuestObjective.Create();
            message.QuestId = quest.Id;

            // 添加所有任务目标信息
            QuestObjectiveInfo info = QuestObjectiveInfo.Create();
            info.QuestObjectiveId = (int)objective.Id;
            info.Count = objective.Count;
            info.NeedCount = objective.GetConfig().NeedCount;
            message.QuestObjective.Add(info);

            MapMessageHelper.NoticeClient(player, message, NoticeType.Self);
        }
    }
}