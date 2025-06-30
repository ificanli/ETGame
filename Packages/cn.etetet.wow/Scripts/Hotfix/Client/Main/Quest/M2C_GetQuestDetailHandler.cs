using System;

namespace ET.Client
{
    /// <summary>
    /// 获取任务详情响应处理器
    /// </summary>
    [MessageHandler(SceneType.WOW)]
    public class M2C_GetQuestDetailHandler : MessageHandler<Scene, M2C_GetQuestDetail>
    {
        protected override async ETTask Run(Scene root, M2C_GetQuestDetail message)
        {
            try
            {
                Scene currentScene = root.CurrentScene();
                
                if (message.Error != 0)
                {
                    Log.Error($"获取任务详情失败: {message.Message}");
                    // 发布获取任务详情失败事件
                    EventSystem.Instance.Publish(currentScene, new ET.GetQuestDetailFailedEvent
                    {
                        QuestId = message.QuestDetail?.QuestId ?? 0,
                        ErrorMessage = message.Message
                    });
                    return;
                }

                if (message.QuestDetail == null)
                {
                    Log.Error("任务详情为空");
                    return;
                }

                Log.Info($"获取任务详情成功: {message.QuestDetail.QuestName}");

                // 发布任务详情接收事件
                EventSystem.Instance.Publish(currentScene, new ET.QuestDetailReceivedEvent
                {
                    QuestId = message.QuestDetail.QuestId,
                    QuestDetail = message.QuestDetail,
                    DetailInfo = message.QuestDetail
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理获取任务详情响应失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}