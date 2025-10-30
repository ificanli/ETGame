using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(ClientQuestComponent))]
    public static partial class ClientQuestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientQuestComponent self)
        {
        }

        /// <summary>
        /// 更新任务数据
        /// </summary>
        public static void UpdateQuestData(this ClientQuestComponent self, long questId, QuestStatus status)
        {
            ClientQuest questData = self.GetQuest(questId);
            questData.Status = status;

            // 发布任务状态更新事件
            EventSystem.Instance.Publish(self.Scene(), new ClientQuestDataChanged { QuestId = questId, Status = (int)status });
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateQuestObjective(this ClientQuestComponent self, long questId, long questObjectiveId, int count)
        {
            ClientQuest quest = self.GetQuest(questId);
            ClientQuestObjective clientQuestObjective = quest.GetObjective(questObjectiveId);
            clientQuestObjective.Count = count;
            // 发布任务目标更新事件
            EventSystem.Instance.Publish(self.Scene(), new ClientQuestObjectiveChanged { QuestId = questId });
        }

        /// <summary>
        /// 获取任务数据
        /// </summary>
        public static ClientQuest GetQuest(this ClientQuestComponent self, long questId)
        {
            return self.GetChild<ClientQuest>(questId);
        }

        /// <summary>
        /// 获取所有进行中的任务
        /// </summary>
        public static List<ClientQuest> GetActiveQuests(this ClientQuestComponent self)
        {
            List<ClientQuest> activeQuests = new();
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is ClientQuest quest && quest.Status == QuestStatus.InProgress)
                {
                    activeQuests.Add(quest);
                }
            }
            return activeQuests;
        }

        /// <summary>
        /// 获取可提交的任务
        /// </summary>
        public static List<ClientQuest> GetSubmittableQuests(this ClientQuestComponent self)
        {
            List<ClientQuest> submittableQuests = new();
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is ClientQuest quest && quest.CanSubmit())
                {
                    submittableQuests.Add(quest);
                }
            }
            return submittableQuests;
        }


        /// <summary>
        /// 移除任务
        /// </summary>
        public static void RemoveQuest(this ClientQuestComponent self, long questId)
        {
            if (!self.RemoveChild(questId))
            {
                return;
            }
            EventSystem.Instance.Publish(self.Scene(), new ClientQuestRemoved { QuestId = questId });
        }
    }

}