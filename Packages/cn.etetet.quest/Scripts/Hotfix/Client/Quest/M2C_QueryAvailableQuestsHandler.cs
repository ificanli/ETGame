using System;

namespace ET.Client
{
    /// <summary>
    /// 查询可接取任务响应处理器
    /// </summary>
    [Module(ModuleName.Quest)]
    [MessageHandler(SceneType.Client)]
    public class M2C_QueryAvailableQuestsHandler : MessageHandler<Scene, M2C_QueryAvailableQuests>
    {
        protected override async ETTask Run(Scene root, M2C_QueryAvailableQuests message)
        {
            try
            {
                Scene currentScene = root.CurrentScene();
                
                if (message.Error != 0)
                {
                    Log.Error($"查询可接取任务失败: {message.Message}");
                    // 发布查询失败事件
                    EventSystem.Instance.Publish(currentScene, new ET.QueryAvailableQuestsFailedEvent
                    {
                        NPCId = 0, // 待服务器传递NPCId
                        ErrorMessage = message.Message
                    });
                    return;
                }

                Log.Info("查询可接取任务成功");

                // 发布可接取任务查询完成事件
                EventSystem.Instance.Publish(currentScene, new ET.AvailableQuestsQueriedEvent
                {
                    NPCId = 0, // 待服务器传递NPCId
                    AvailableQuests = new System.Collections.Generic.List<ET.AvailableQuestInfo>() // 待完善
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理查询可接取任务响应失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}