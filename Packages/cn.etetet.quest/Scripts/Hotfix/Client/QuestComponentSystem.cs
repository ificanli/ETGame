using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(QuestComponent))]
    public static partial class QuestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this QuestComponent self)
        {
        }

        /// <summary>
        /// 更新任务数据
        /// </summary>
        public static void UpdateQuestData(this QuestComponent self, long questId, QuestStatus status)
        {
            Quest questData = self.GetQuest(questId);
            questData.Status = status;

            // 发布任务状态更新事件
            EventSystem.Instance.Publish(self.Scene(), new ClientQuestDataChanged { QuestId = questId, Status = (int)status });
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateQuestObjective(this QuestComponent self, long questId, long questObjectiveId, int count)
        {
            Quest quest = self.GetQuest(questId);
            QuestObjective clientQuestObjective = quest.GetObjective(questObjectiveId);
            clientQuestObjective.Count = count;
            // 发布任务目标更新事件
            EventSystem.Instance.Publish(self.Scene(), new ClientQuestObjectiveChanged { QuestId = questId });
        }

        /// <summary>
        /// 获取任务数据
        /// </summary>
        public static Quest GetQuest(this QuestComponent self, long questId)
        {
            return self.GetChild<Quest>(questId);
        }

        /// <summary>
        /// 获取所有进行中的任务
        /// </summary>
        public static List<Quest> GetActiveQuests(this QuestComponent self)
        {
            List<Quest> activeQuests = new();
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is Quest quest && quest.Status == QuestStatus.InProgress)
                {
                    activeQuests.Add(quest);
                }
            }
            return activeQuests;
        }

        /// <summary>
        /// 获取可提交的任务
        /// </summary>
        public static List<Quest> GetSubmittableQuests(this QuestComponent self)
        {
            List<Quest> submittableQuests = new();
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is Quest quest && quest.CanSubmit())
                {
                    submittableQuests.Add(quest);
                }
            }
            return submittableQuests;
        }


        /// <summary>
        /// 移除任务
        /// </summary>
        public static void RemoveQuest(this QuestComponent self, long questId)
        {
            if (!self.RemoveChild(questId))
            {
                return;
            }
            EventSystem.Instance.Publish(self.Scene(), new ClientQuestRemoved { QuestId = questId });
        }
    }

}