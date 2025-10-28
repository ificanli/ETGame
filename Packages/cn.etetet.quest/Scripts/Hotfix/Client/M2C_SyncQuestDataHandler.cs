using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 同步任务数据响应处理器
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_SyncQuestDataHandler : MessageHandler<Scene, M2C_SyncQuestData>
    {
        protected override async ETTask Run(Scene root, M2C_SyncQuestData message)
        {
            if (message.Error != 0)
            {
                Log.Error($"同步任务数据失败: {message.Message}");
                return;
            }

            ClientQuestComponent questComponent = root.AddComponent<ClientQuestComponent>();

            // 同步服务器任务数据
            foreach (QuestInfo questInfo in message.QuestList)
            {
                ClientQuest clientQuest = questComponent.AddChildWithId<ClientQuest>(questInfo.QuestId);
                clientQuest.Status = (QuestStatus)questInfo.Status;
                clientQuest.CreateTime = questInfo.AcceptTime;
                clientQuest.CompleteTime = questInfo.CompleteTime;

                // 同步任务目标
                foreach (QuestObjectiveInfo objInfo in questInfo.Objectives)
                {
                    ClientQuestObjective objective = clientQuest.AddChildWithId<ClientQuestObjective>(objInfo.QuestObjectiveId);
                    objective.Count = objInfo.Count;
                    objective.NeedCount = objInfo.NeedCount;

                    clientQuest.Objectives.Add(objective);
                }
            }

            Log.Debug($"成功同步 {message.QuestList.Count} 个任务数据");

            // 发布任务数据同步完成事件
            var questStatusMap = new Dictionary<long, int>();
            foreach (QuestInfo questInfo in message.QuestList)
            {
                questStatusMap[questInfo.QuestId] = questInfo.Status;
            }

            EventSystem.Instance.Publish(root, new QuestDataSyncedEvent
            {
                QuestStatusMap = questStatusMap
            });
            await ETTask.CompletedTask;
        }
    }
}