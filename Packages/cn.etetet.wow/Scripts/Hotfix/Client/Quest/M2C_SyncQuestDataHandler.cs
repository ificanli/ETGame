using System;

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
            try
            {
                Scene currentScene = root.CurrentScene();
                
                if (message.Error != 0)
                {
                    Log.Error($"同步任务数据失败: {message.Message}");
                    return;
                }

                ClientQuestComponent questComponent = currentScene.GetComponent<ClientQuestComponent>();
                if (questComponent == null)
                {
                    questComponent = currentScene.AddComponent<ClientQuestComponent>();
                }

                // 清空现有任务数据
                questComponent.QuestDict.Clear();

                // 同步服务器任务数据
                foreach (var questInfo in message.QuestList)
                {
                    ClientQuestData clientQuest = new ClientQuestData
                    {
                        QuestId = (int)questInfo.QuestId, // 显式转换long到int
                        Status = (QuestStatus)questInfo.Status,
                        CreateTime = questInfo.AcceptTime,
                        CompleteTime = questInfo.CompleteTime
                    };

                    // 同步任务目标
                    foreach (var objInfo in questInfo.Objectives)
                    {
                        ClientQuestObjectiveData objective = new ClientQuestObjectiveData
                        {
                            ObjectiveId = objInfo.QuestObjectiveId,
                            CurrentCount = objInfo.Count,
                            RequiredCount = objInfo.NeedCount,
                            IsCompleted = objInfo.Count >= objInfo.NeedCount
                        };
                        clientQuest.Objectives.Add(objective);
                    }

                    questComponent.QuestDict[(int)questInfo.QuestId] = clientQuest; // 显式转换long到int
                }

                Log.Info($"成功同步 {message.QuestList.Count} 个任务数据");

                // 发布任务数据同步完成事件
                var questStatusMap = new System.Collections.Generic.Dictionary<int, int>();
                foreach (var questInfo in message.QuestList)
                {
                    questStatusMap[(int)questInfo.QuestId] = questInfo.Status;
                }
                EventSystem.Instance.Publish(currentScene, new ET.QuestDataSyncedEvent
                {
                    QuestStatusMap = questStatusMap
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理任务数据同步失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}