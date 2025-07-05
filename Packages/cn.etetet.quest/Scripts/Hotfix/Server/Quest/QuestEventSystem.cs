using System;

namespace ET.Server
{
    /// <summary>
    /// 任务事件系统 - 处理各种游戏事件对任务进度的影响
    /// </summary>
    [Module(ModuleName.Quest)]
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
            var killObjectives = questComponent.GetQuestObjective(QuestObjectiveType.KillMonster);
            if (killObjectives != null)
            {
                foreach (EntityRef<QuestObjective> objectiveRef in killObjectives)
                {
                    QuestObjective objective = objectiveRef;
                    if (objective == null)
                    {
                        continue;
                    }

                    // 检查是否是目标怪物
                    if (objective.Params.Count > 0 && objective.Params[0] == monsterConfigId)
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
            if (questComponent == null)
            {
                return;
            }

            // 触发收集类型任务的进度检查
            var collectObjectives = questComponent.GetQuestObjective(QuestObjectiveType.Collectltem);
            if (collectObjectives != null)
            {
                foreach (EntityRef<QuestObjective> objectiveRef in collectObjectives)
                {
                    QuestObjective objective = objectiveRef;
                    if (objective == null)
                    {
                        continue;
                    }

                    // 检查是否是目标物品
                    if (objective.Params.Count > 0 && objective.Params[0] == itemId)
                    {
                        UpdateObjectiveProgress(objective, count);
                    }
                }
            }
        }

        /// <summary>
        /// 处理玩家升级事件
        /// </summary>
        public static void OnPlayerLevelUp(Unit player, int newLevel)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                return;
            }

            // 触发等级类型任务的进度检查
            var levelObjectives = questComponent.GetQuestObjective(QuestObjectiveType.Level);
            if (levelObjectives != null)
            {
                foreach (EntityRef<QuestObjective> objectiveRef in levelObjectives)
                {
                    QuestObjective objective = objectiveRef;
                    if (objective == null)
                    {
                        continue;
                    }

                    // 检查是否达到目标等级
                    int targetLevel = objective.TargetCount;
                    if (newLevel >= targetLevel)
                    {
                        objective.Progress = targetLevel;
                        objective.IsCompleted = true;
                        NotifyQuestProgress(player, objective);
                    }
                }
            }
        }

        /// <summary>
        /// 处理进入地图事件
        /// </summary>
        public static void OnEnterMap(Unit player, int mapId)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                return;
            }

            // 触发进入地图类型任务的进度检查
            var enterMapObjectives = questComponent.GetQuestObjective(QuestObjectiveType.EnterMap);
            if (enterMapObjectives != null)
            {
                foreach (EntityRef<QuestObjective> objectiveRef in enterMapObjectives)
                {
                    QuestObjective objective = objectiveRef;
                    if (objective == null)
                    {
                        continue;
                    }

                    // 检查是否是目标地图
                    if (objective.Params.Count > 0 && objective.Params[0] == mapId)
                    {
                        objective.Progress = 1;
                        objective.IsCompleted = true;
                        NotifyQuestProgress(player, objective);
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

            objective.Progress = Math.Min(objective.Progress + increment, objective.TargetCount);
            
            if (objective.Progress >= objective.TargetCount)
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
                    info.QuestObjectiveId = obj.ConfigId;
                    info.Count = obj.Progress;
                    info.NeedCount = obj.TargetCount;
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