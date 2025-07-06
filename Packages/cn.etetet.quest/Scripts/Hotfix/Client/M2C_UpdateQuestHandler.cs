namespace ET.Client
{
    [Module(ModuleName.Quest)]
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateQuestHandler : MessageHandler<Scene, M2C_UpdateQuest>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateQuest message)
        {
            ClientQuestComponent questComponent = scene.GetComponent<ClientQuestComponent>();
            if (questComponent == null)
            {
                Log.Error("ClientQuestComponent not found in scene");
                await ETTask.CompletedTask;
                return;
            }

            // 更新任务状态
            QuestStatus questStatus = (QuestStatus)message.State;
            questComponent.UpdateQuestData((int)message.QuestId, questStatus);

            Log.Info($"Quest {message.QuestId} status updated to {questStatus}");
            
            await ETTask.CompletedTask;
        }
    }
}