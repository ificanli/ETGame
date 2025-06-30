using System;

namespace ET.Client
{
    /// <summary>
    /// 放弃任务响应处理器
    /// </summary>
    [MessageHandler(SceneType.WOW)]
    public class M2C_AbandonQuestHandler : MessageHandler<Scene, M2C_AbandonQuest>
    {
        protected override async ETTask Run(Scene root, M2C_AbandonQuest message)
        {
            try
            {
                Scene currentScene = root.CurrentScene();
                
                if (message.Error != 0)
                {
                    Log.Error($"放弃任务失败: {message.Message}");
                    // 发布任务操作失败事件
                    EventSystem.Instance.Publish(currentScene, new ET.QuestOperationFailedEvent
                    {
                        QuestId = 0, // 待服务器传递QuestId
                        Operation = "abandon",
                        ErrorMessage = message.Message
                    });
                    return;
                }

                Log.Info("成功放弃任务");

                // 发布任务放弃事件
                EventSystem.Instance.Publish(currentScene, new ET.QuestAbandonedEvent
                {
                    QuestId = 0 // 待服务器传递QuestId
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理放弃任务响应失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}