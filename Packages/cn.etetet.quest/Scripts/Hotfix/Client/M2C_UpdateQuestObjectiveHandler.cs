namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateQuestObjectiveHandler : MessageHandler<Scene, M2C_UpdateQuestObjective>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateQuestObjective message)
        {
            QuestComponent questComponent = scene.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                Log.Error("QuestComponent not found in scene");
                await ETTask.CompletedTask;
                return;
            }

            // 更新任务目标进度 - 需要转换服务器数据到客户端数据格式
            // TODO: 实现服务器QuestObjective到ClientQuestObjectiveData的转换
            // questComponent.UpdateQuestObjective(message.QuestId, ConvertToClientObjectives(message.QuestObjective));

            Log.Info($"Quest {message.QuestId} objectives updated, count: {message.Count}");
            
            await ETTask.CompletedTask;
        }
    }
}