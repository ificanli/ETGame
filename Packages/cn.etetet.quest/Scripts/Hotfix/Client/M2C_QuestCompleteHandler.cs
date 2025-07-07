using System;

namespace ET.Client
{
    /// <summary>
    /// 任务完成通知处理器
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_QuestCompleteHandler : MessageHandler<Scene, M2C_QuestComplete>
    {
        protected override async ETTask Run(Scene root, M2C_QuestComplete message)
        {
            try
            {
                Scene currentScene = root.CurrentScene();

                Log.Info("任务完成");

                // 发布任务完成事件
                EventSystem.Instance.Publish(currentScene, new ET.QuestCompletedEvent
                {
                    QuestId = 0, // 待服务器传递QuestId
                    Reward = new ET.ClientQuestReward
                    {
                        Experience = 0,
                        Gold = 0,
                        ItemIds = new System.Collections.Generic.List<int>()
                    },
                    RewardItems = new System.Collections.Generic.List<int>()
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理任务完成通知失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}