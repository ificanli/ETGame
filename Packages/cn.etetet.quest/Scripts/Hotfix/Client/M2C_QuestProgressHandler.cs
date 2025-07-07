using System;

namespace ET.Client
{
    /// <summary>
    /// 任务进度更新通知处理器
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_QuestProgressHandler : MessageHandler<Scene, M2C_QuestProgress>
    {
        protected override async ETTask Run(Scene root, M2C_QuestProgress message)
        {
            try
            {
                Scene currentScene = root.CurrentScene();

                Log.Info($"任务进度更新: {message.ProgressText}");

                // 发布任务目标进度更新事件
                EventSystem.Instance.Publish(currentScene, new ET.QuestObjectiveProgressUpdatedEvent
                {
                    QuestId = message.QuestId,
                    ObjectiveId = 0, // 待服务器传递具体ObjectiveId
                    OldCount = 0, // 待完善
                    NewCount = 0, // 待完善
                    Progress = new System.Collections.Generic.Dictionary<int, int>()
                });
            }
            catch (Exception e)
            {
                Log.Error($"处理任务进度更新通知失败: {e}");
            }

            await ETTask.CompletedTask;
        }
    }
}