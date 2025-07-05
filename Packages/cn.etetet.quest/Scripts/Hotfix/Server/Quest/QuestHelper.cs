using System.Collections.Generic;
using System.Threading.Tasks;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
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
        public static Quest FindQuest(Unit self, int questId)
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
            if (questConfig == null) return;
            
            // TODO: 根据questConfig中的奖励配置发放奖励
            // 这里需要配置表支持奖励字段，如：
            // - ExpReward: 经验奖励
            // - GoldReward: 金币奖励
            // - ItemRewards: 物品奖励列表
            // - ReputationRewards: 声望奖励
            
            // 示例奖励发放逻辑：
            // self.GetComponent<LevelComponent>()?.AddExp(questConfig.ExpReward);
            // self.GetComponent<BagComponent>()?.AddGold(questConfig.GoldReward);
            
            Log.Debug($"Quest {questId} rewards granted to player {self.Id}");
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateQuestObjectiveProgress(Unit self, int questId, int objectiveId, int progress)
        {
            Quest quest = self.GetComponent<QuestComponent>().GetQuest(questId);
            if (quest == null) return;
            QuestObjective objective = quest.FindObjective(objectiveId);
            if (objective == null) return;
            
            int oldProgress = objective.Progress;
            objective.Progress = progress;
            
            // 检查目标是否完成
            if (objective.Progress >= objective.TargetCount && !objective.IsCompleted)
            {
                objective.IsCompleted = true;
                NotifyQuestObjectiveCompleted(self, questId, objectiveId);
            }
            
            // 检查整个任务是否完成
            if (quest.IsFinished() && quest.Status != QuestStatus.CanSubmit)
            {
                quest.Status = QuestStatus.CanSubmit;
                NotifyQuestCanSubmit(self, questId);
            }
            
            // 发送进度更新通知
            if (oldProgress != progress)
            {
                NotifyQuestProgressUpdate(self, questId, objectiveId, progress);
            }
        }

        /// <summary>
        /// 处理怪物击杀事件
        /// </summary>
        public static void OnMonsterKilled(Unit self, int monsterId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            questComponent.Process(QuestObjectiveType.KillMonster);
            
            // 同时通知所有相关的任务目标
            foreach (var kvp in questComponent.ActiveQuests)
            {
                Quest quest = kvp.Value;
                foreach (var child in quest.Children.Values)
                {
                    if (child is QuestObjective objective)
                    {
                        objective.OnMonsterKilled(monsterId);
                    }
                }
            }
        }

        /// <summary>
        /// 处理物品收集事件
        /// </summary>
        public static void OnItemCollected(Unit self, int itemId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            questComponent.Process(QuestObjectiveType.Collectltem);
            
            // 通知所有相关的任务目标
            foreach (var kvp in questComponent.ActiveQuests)
            {
                Quest quest = kvp.Value;
                foreach (var child in quest.Children.Values)
                {
                    if (child is QuestObjective objective)
                    {
                        objective.OnItemCollected(itemId);
                    }
                }
            }
        }

        /// <summary>
        /// 处理进入地图事件
        /// </summary>
        public static void OnEnterMap(Unit self, int mapId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            questComponent.Process(QuestObjectiveType.EnterMap);
            
            // 通知所有相关的任务目标
            foreach (var kvp in questComponent.ActiveQuests)
            {
                Quest quest = kvp.Value;
                foreach (var child in quest.Children.Values)
                {
                    if (child is QuestObjective objective)
                    {
                        objective.OnEnterMap(mapId);
                    }
                }
            }
        }

        /// <summary>
        /// 获取可接取的任务列表
        /// </summary>
        public static List<int> GetAvailableQuests(Unit self)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            List<int> availableQuests = new List<int>();
            
            foreach (int questId in questComponent.AvailableQuests)
            {
                if (CheckQuestAcceptable(self, questId))
                {
                    availableQuests.Add(questId);
                }
            }
            
            return availableQuests;
        }

        /// <summary>
        /// 获取进行中的任务列表
        /// </summary>
        public static List<Quest> GetActiveQuests(Unit self)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            List<Quest> activeQuests = new List<Quest>();
            
            foreach (var kvp in questComponent.ActiveQuests)
            {
                Quest quest = kvp.Value;
                if (quest != null)
                {
                    activeQuests.Add(quest);
                }
            }
            
            return activeQuests;
        }

        #region 通知方法
        
        /// <summary>
        /// 通知任务目标完成
        /// </summary>
        private static void NotifyQuestObjectiveCompleted(Unit self, int questId, int objectiveId)
        {
            Quest quest = self.GetComponent<QuestComponent>().GetQuest(questId);
            if (quest == null) return;
            
            M2C_UpdateQuestObjective message = M2C_UpdateQuestObjective.Create();
            message.QuestId = questId;
            
            // 添加所有任务目标信息
            foreach (var child in quest.Children.Values)
            {
                if (child is QuestObjective objective)
                {
                    QuestObjectiveInfo info = QuestObjectiveInfo.Create();
                    info.QuestObjectiveId = objective.ConfigId;
                    info.Count = objective.Progress;
                    info.NeedCount = objective.TargetCount;
                    message.QuestObjective.Add(info);
                }
            }
            
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

        /// <summary>
        /// 通知任务进度更新
        /// </summary>
        private static void NotifyQuestProgressUpdate(Unit self, int questId, int objectiveId, int progress)
        {
            Quest quest = self.GetComponent<QuestComponent>().GetQuest(questId);
            if (quest == null) return;
            
            M2C_UpdateQuestObjective message = M2C_UpdateQuestObjective.Create();
            message.QuestId = questId;
            
            // 添加所有任务目标信息
            foreach (var child in quest.Children.Values)
            {
                if (child is QuestObjective objective)
                {
                    QuestObjectiveInfo info = QuestObjectiveInfo.Create();
                    info.QuestObjectiveId = objective.ConfigId;
                    info.Count = objective.Progress;
                    info.NeedCount = objective.TargetCount;
                    message.QuestObjective.Add(info);
                }
            }
            
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

        /// <summary>
        /// 发放任务奖励
        /// </summary>
        public static async ETTask GiveQuestReward(Unit unit, int questId)
        {
            var questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return;
            }

            // TODO: 实现任务奖励发放
            // 当前QuestConfig只有基本字段，没有奖励字段
            // 需要配置表设计师添加奖励相关字段，如：
            // - RewardExp: 经验奖励
            // - RewardGold: 金币奖励  
            // - RewardItems: 物品奖励列表
            
            Log.Debug($"给予玩家 {unit.Id} 任务 {questId} 奖励 (暂时未实现具体奖励)");

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 移除任务相关道具
        /// </summary>
        public static async ETTask RemoveQuestItems(Unit unit, int questId)
        {
            var questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return;
            }

            // TODO: 移除任务物品
            // 当前QuestConfig没有QuestItems字段
            // 需要配置表设计师添加任务物品字段
            Log.Debug($"移除玩家 {unit.Id} 任务 {questId} 相关物品 (暂时未实现)");

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 检查背包空间是否足够
        /// </summary>
        public static bool CheckBagSpace(Unit unit, int questId)
        {
            var questConfig = QuestConfigCategory.Instance.Get(questId);
            if (questConfig == null)
            {
                return true;
            }

            // TODO: 实现背包空间检查
            // var bagComponent = unit.GetComponent<BagComponent>();
            // if (bagComponent == null)
            // {
            //     return false;
            // }

            // TODO: 检查奖励物品所需空间
            // 当前QuestConfig没有RewardItems字段
            // 需要配置表设计师添加奖励物品字段
            Log.Debug($"检查玩家 {unit.Id} 背包空间 (暂时未实现具体检查)");

            return true; // 暂时返回true
        }

        #endregion

        #region 机器人测试所需方法

        /// <summary>
        /// 获取可接取任务列表（按NPC ID过滤）
        /// </summary>
        public static List<AvailableQuestInfo> GetAvailableQuests(Unit unit, long npcId)
        {
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            List<AvailableQuestInfo> availableQuests = new List<AvailableQuestInfo>();
            
            foreach (int questId in questComponent.AvailableQuests)
            {
                if (CheckQuestAcceptable(unit, questId))
                {
                    // 如果指定了NPC ID，只返回该NPC的任务
                    if (npcId != 0 && !IsCorrectAcceptNPC(questId, (int)npcId))
                    {
                        continue;
                    }
                    
                    var questConfig = QuestConfigCategory.Instance.Get(questId);
                    if (questConfig != null)
                    {
                        AvailableQuestInfo questInfo = AvailableQuestInfo.Create();
                        questInfo.QuestId = questId;
                        questInfo.QuestName = questConfig.Name ?? $"Quest {questId}";
                        questInfo.QuestDesc = questConfig.Desc ?? "No description";
                        questInfo.QuestType = 1; // 默认任务类型
                        // TODO: 添加奖励信息
                        questInfo.RewardExp = 0;
                        questInfo.RewardGold = 0;
                        availableQuests.Add(questInfo);
                    }
                }
            }
            
            return availableQuests;
        }

        /// <summary>
        /// 获取玩家任务列表
        /// </summary>
        public static List<QuestInfo> GetPlayerQuestList(Unit unit)
        {
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            List<QuestInfo> questList = new List<QuestInfo>();
            
            foreach (var kvp in questComponent.ActiveQuests)
            {
                Quest quest = kvp.Value;
                if (quest != null)
                {
                    QuestInfo questInfo = QuestInfo.Create();
                    questInfo.QuestId = quest.Id;
                    questInfo.Status = (int)quest.Status;
                    questInfo.AcceptTime = 0; // TODO: 添加AcceptTime字段到Quest
                    questInfo.CompleteTime = 0; // TODO: 添加CompleteTime字段到Quest
                    
                    // 添加任务目标信息
                    foreach (var child in quest.Children.Values)
                    {
                        if (child is QuestObjective objective)
                        {
                            QuestObjectiveInfo objInfo = QuestObjectiveInfo.Create();
                            objInfo.QuestObjectiveId = objective.ConfigId;
                            objInfo.Count = objective.Progress;
                            objInfo.NeedCount = objective.TargetCount;
                            questInfo.Objectives.Add(objInfo);
                        }
                    }
                    
                    questList.Add(questInfo);
                }
            }
            
            return questList;
        }

        #endregion
    }
}