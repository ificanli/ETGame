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
        public static void OnMonsterKilled(Unit player, int monsterId, int monsterConfigId)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                return;
            }

            // 触发击杀类型任务的进度检查
            var killObjectives = questComponent.GetQuestObjectiveByType(QuestObjectiveType.KillMonster);
            if (killObjectives != null)
            {
                foreach (QuestObjective objective in killObjectives)
                {
                    if (objective == null)
                    {
                        continue;
                    }
                    QuestObjectiveConfig questObjectiveConfig = objective.GetConfig();
                    
                    if (objective.Count >= questObjectiveConfig.NeedCount)
                    {
                        return;
                    }
                    
                    QuestObjectiveParams_KillMonster questObjectiveParams = (QuestObjectiveParams_KillMonster)questObjectiveConfig.Params;
                    // 检查是否是目标怪物
                    if (questObjectiveParams.MonsterId == monsterConfigId)
                    {
                        UpdateObjectiveProgress(objective, 1);
                    }
                }
            }
        }

        /// <summary>
        /// 处理物品收集事件
        /// </summary>
        public static void OnItemCollected(Unit player, int itemId, int count)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();

            // 触发收集类型任务的进度检查
            var collectObjectives = questComponent.GetQuestObjectiveByType(QuestObjectiveType.Collectltem);
            if (collectObjectives != null)
            {
                foreach (EntityRef<QuestObjective> objectiveRef in collectObjectives)
                {
                    QuestObjective objective = objectiveRef;
                    if (objective == null)
                    {
                        continue;
                    }

                    QuestObjectiveConfig questObjectiveConfig = objective.GetConfig();

                    if (objective.Count >= questObjectiveConfig.NeedCount)
                    {
                        return;
                    }
                    
                    QuestObjectiveParams_Collectltem questObjectiveParams = (QuestObjectiveParams_Collectltem)questObjectiveConfig.Params;
                    // 检查是否是目标物品
                    if (questObjectiveParams.ItemId == itemId)
                    {
                        UpdateObjectiveProgress(objective, count);
                    }
                }
            }
        }
        
        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        private static void UpdateObjectiveProgress(QuestObjective objective, int increment)
        {
            if (objective.IsCompleted)
            {
                return;
            }

            ++objective.Count;
            QuestObjectiveConfig questObjectiveConfig = objective.GetConfig();
            if (objective.Count >= questObjectiveConfig.NeedCount)
            {
                objective.IsCompleted = true;
            }

            // 获取玩家Unit并通知进度更新
            Unit player = objective.GetParent<Quest>().GetParent<QuestComponent>().GetParent<Unit>();
            NotifyQuestProgress(player, objective);
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
            foreach (var child in quest.Children.Values)
            {
                if (child is QuestObjective obj)
                {
                    QuestObjectiveInfo info = QuestObjectiveInfo.Create();
                    info.QuestObjectiveId = (int)obj.Id;
                    info.Count = obj.Count;
                    info.NeedCount = obj.GetConfig().NeedCount;
                    message.QuestObjective.Add(info);
                }
            }

            MapMessageHelper.NoticeClient(player, message, NoticeType.Self);

            // 检查整个任务是否完成
            if (quest.IsFinished())
            {
                quest.Status = QuestStatus.CanSubmit;
                QuestHelper.NotifyQuestCanSubmit(player, (int)quest.Id);
            }
        }
    }
}