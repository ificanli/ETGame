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

            QuestComponent questComponent = root.AddComponent<QuestComponent>();

            // 同步服务器任务数据
            foreach (QuestInfo questInfo in message.QuestList)
            {
                Quest quest = questComponent.AddChildWithId<Quest>(questInfo.QuestId);
                quest.Status = (QuestStatus)questInfo.Status;
                quest.CreateTime = questInfo.AcceptTime;
                quest.CompleteTime = questInfo.CompleteTime;

                // 同步任务目标
                foreach (QuestObjectiveInfo objInfo in questInfo.Objectives)
                {
                    QuestObjective objective = quest.AddChildWithId<QuestObjective>(objInfo.QuestObjectiveId);
                    objective.Count = objInfo.Count;

                    quest.Objectives.Add(objective);
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