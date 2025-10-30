using System;

namespace ET.Server
{
    /// <summary>
    /// 任务事件系统 - 处理各种游戏事件对任务进度的影响
    /// </summary>
    public static class QuestEventHelper
    {
        /// <summary>
        /// 处理怪物击杀事件
        /// </summary>
        public static void OnMonsterKilled(Unit player, long monsterId, int count)
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
                QuestHelper.UpdateObjectiveCount(questObjective);
            }
        }

        /// <summary>
        /// 处理物品收集事件
        /// </summary>
        public static void OnItemCollected(Unit player, long itemId, int count)
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
                QuestHelper.UpdateObjectiveCount(questObjective);

            }
        }
        

    }
}