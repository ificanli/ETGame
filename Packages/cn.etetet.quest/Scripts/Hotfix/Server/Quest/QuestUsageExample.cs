namespace ET.Server
{
    /// <summary>
    /// 任务系统使用示例
    /// 展示如何在游戏中集成和使用魔兽世界风格的任务系统
    /// </summary>
    public static class QuestUsageExample
    {
        /// <summary>
        /// 示例：当怪物死亡时调用此方法
        /// 可以在怪物死亡的逻辑中调用
        /// </summary>
        public static void OnMonsterDeadExample(Unit monster, Unit killer)
        {
            if (killer?.GetComponent<QuestComponent>() == null)
            {
                return;
            }

            // 获取怪物配置ID
            int monsterConfigId = monster.ConfigId; // 假设Unit有ConfigId属性
            
            // 触发任务事件
            QuestEventSystem.OnMonsterKilled(killer, (int)monster.Id, monsterConfigId);
            
            Log.Debug($"Player {killer.Id} killed monster {monsterConfigId}, quest progress updated");
        }

        /// <summary>
        /// 示例：当玩家获得物品时调用此方法
        /// 可以在背包系统中调用
        /// </summary>
        public static void OnItemObtainedExample(Unit player, int itemId, int count)
        {
            if (player?.GetComponent<QuestComponent>() == null)
            {
                return;
            }

            // 触发任务事件
            QuestEventSystem.OnItemCollected(player, itemId, count);
            
            Log.Debug($"Player {player.Id} obtained {count} of item {itemId}, quest progress updated");
        }

        /// <summary>
        /// 示例：当玩家升级时调用此方法
        /// 可以在等级系统中调用
        /// </summary>
        public static void OnPlayerLevelUpExample(Unit player, int oldLevel, int newLevel)
        {
            if (player?.GetComponent<QuestComponent>() == null)
            {
                return;
            }

            // 触发任务事件
            QuestEventSystem.OnPlayerLevelUp(player, newLevel);
            
            Log.Debug($"Player {player.Id} leveled up from {oldLevel} to {newLevel}, quest progress updated");
        }

        /// <summary>
        /// 示例：当玩家进入新地图时调用此方法
        /// 可以在地图传送系统中调用
        /// </summary>
        public static void OnPlayerEnterMapExample(Unit player, int mapId)
        {
            if (player?.GetComponent<QuestComponent>() == null)
            {
                return;
            }

            // 触发任务事件
            QuestEventSystem.OnEnterMap(player, mapId);
            
            Log.Debug($"Player {player.Id} entered map {mapId}, quest progress updated");
        }

        /// <summary>
        /// 示例：玩家接取任务
        /// </summary>
        public static void AcceptQuestExample(Unit player, int questConfigId)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                Log.Error($"Player {player.Id} has no QuestComponent");
                return;
            }

            // 检查任务是否可接取
            if (!QuestHelper.CheckQuestAcceptable(player, questConfigId))
            {
                Log.Warning($"Quest {questConfigId} is not acceptable for player {player.Id}");
                return;
            }

            // 接取任务
            Quest quest = questComponent.AcceptQuest(questConfigId);
            if (quest != null)
            {
                Log.Debug($"Player {player.Id} accepted quest {questConfigId}");
                
                // 通知客户端
                QuestHelper.AddQuest(player, questConfigId);
            }
            else
            {
                Log.Error($"Failed to accept quest {questConfigId} for player {player.Id}");
            }
        }

        /// <summary>
        /// 示例：玩家提交任务
        /// </summary>
        public static void SubmitQuestExample(Unit player, int questConfigId)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                Log.Error($"Player {player.Id} has no QuestComponent");
                return;
            }

            // 检查任务是否可提交
            if (!QuestHelper.CheckQuestSubmittable(player, questConfigId))
            {
                Log.Warning($"Quest {questConfigId} is not submittable for player {player.Id}");
                return;
            }

            // 完成任务
            if (questComponent.CompleteQuest(questConfigId))
            {
                Log.Debug($"Player {player.Id} completed quest {questConfigId}");
                
                // 发放奖励
                QuestHelper.GrantQuestReward(player, questConfigId);
                
                // 通知客户端任务完成
                QuestHelper.FinishQuest(player, questConfigId);
            }
            else
            {
                Log.Error($"Failed to complete quest {questConfigId} for player {player.Id}");
            }
        }

        /// <summary>
        /// 示例：获取玩家的任务状态
        /// </summary>
        public static void GetPlayerQuestStatusExample(Unit player)
        {
            QuestComponent questComponent = player.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                Log.Debug($"Player {player.Id} has no quests");
                return;
            }

            // 获取可接取的任务
            var availableQuests = QuestHelper.GetAvailableQuests(player);
            Log.Debug($"Player {player.Id} has {availableQuests.Count} available quests");

            // 获取进行中的任务
            var activeQuests = QuestHelper.GetActiveQuests(player);
            Log.Debug($"Player {player.Id} has {activeQuests.Count} active quests");

            // 打印每个进行中任务的详细信息
            foreach (Quest quest in activeQuests)
            {
                QuestConfig config = quest.GetConfig();
                Log.Debug($"Active Quest: {config.Name} (ID: {quest.ConfigId}), Status: {quest.Status}");
                
                // 打印任务目标进度
                foreach (var child in quest.Children.Values)
                {
                    if (child is QuestObjective objective)
                    {
                        QuestObjectiveConfig objConfig = objective.GetConfig();
                        Log.Debug($"  Objective: {objConfig.Name}, Progress: {objective.Progress}/{objective.TargetCount}");
                    }
                }
            }
        }
    }
}