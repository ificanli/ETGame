using System;

namespace ET.Client
{
    /// <summary>
    /// 任务失败通知处理器
    /// </summary>
    [MessageHandler(SceneType.WOW)]
    public class M2C_QuestFailedHandler : MessageHandler<Scene, M2C_QuestFailed>
    {
        protected override async ETTask Run(Scene root, M2C_QuestFailed message)
        {
            try
            {
                Scene currentScene = root.CurrentScene();

                ClientQuestComponent questComponent = currentScene.GetComponent<ClientQuestComponent>();
                if (questComponent != null)
                {
                    ClientQuestData quest = questComponent.GetQuestData(message.QuestId);
                    if (quest != null)
                    {
                        // 更新任务状态为失败
                        quest.Status = QuestStatus.Failed;
                    }
                }

                Log.Info($"任务失败: {message.QuestId}, 原因: {message.Reason}");

                // 发布任务失败事件
                EventSystem.Instance.Publish(currentScene, new ET.QuestFailedEvent
                {
                    QuestId = message.QuestId,
                    Reason = message.Reason
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理任务失败通知失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}