using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    [EntitySystemOf(typeof(ClientQuestComponent))]
    public static partial class ClientQuestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientQuestComponent self)
        {
            self.QuestDict.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ClientQuestComponent self)
        {
            self.QuestDict.Clear();
        }

        /// <summary>
        /// 更新任务数据
        /// </summary>
        public static void UpdateQuestData(this ClientQuestComponent self, int questId, QuestStatus status)
        {
            if (!self.QuestDict.ContainsKey(questId))
            {
                self.QuestDict[questId] = new ClientQuestData
                {
                    QuestId = questId,
                    Status = status
                };
            }
            else
            {
                self.QuestDict[questId].Status = status;
            }

            // 发布任务状态更新事件
            EventSystem.Instance.Publish(self.Scene(), new ET.ClientQuestDataChanged { QuestId = questId, Status = (int)status });
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateQuestObjective(this ClientQuestComponent self, int questId, List<ClientQuestObjectiveData> objectives)
        {
            if (!self.QuestDict.ContainsKey(questId))
            {
                self.QuestDict[questId] = new ClientQuestData
                {
                    QuestId = questId,
                    Status = QuestStatus.InProgress
                };
            }

            self.QuestDict[questId].Objectives = objectives;

            // 发布任务目标更新事件
            EventSystem.Instance.Publish(self.Scene(), new ET.ClientQuestObjectiveChanged { QuestId = questId });
        }

        /// <summary>
        /// 获取任务数据
        /// </summary>
        public static ClientQuestData GetQuestData(this ClientQuestComponent self, int questId)
        {
            self.QuestDict.TryGetValue(questId, out ClientQuestData questData);
            return questData;
        }

        /// <summary>
        /// 获取所有进行中的任务
        /// </summary>
        public static List<ClientQuestData> GetActiveQuests(this ClientQuestComponent self)
        {
            return self.QuestDict.Values
                .Where(quest => quest.Status == QuestStatus.InProgress)
                .ToList();
        }

        /// <summary>
        /// 获取可提交的任务
        /// </summary>
        public static List<ClientQuestData> GetSubmittableQuests(this ClientQuestComponent self)
        {
            return self.QuestDict.Values
                .Where(quest => quest.Status == QuestStatus.CanSubmit)
                .ToList();
        }

        /// <summary>
        /// 检查任务是否存在
        /// </summary>
        public static bool HasQuest(this ClientQuestComponent self, int questId)
        {
            return self.QuestDict.ContainsKey(questId);
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public static void RemoveQuest(this ClientQuestComponent self, int questId)
        {
            if (self.QuestDict.Remove(questId))
            {
                EventSystem.Instance.Publish(self.Scene(), new ET.ClientQuestRemoved { QuestId = questId });
            }
        }
    }

}