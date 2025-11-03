namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateQuestHandler : MessageHandler<Scene, M2C_UpdateQuest>
    {
        protected override async ETTask Run(Scene root, M2C_UpdateQuest message)
        {
            QuestComponent questComponent = root.GetComponent<QuestComponent>();

            // 更新任务状态
            QuestStatus questStatus = (QuestStatus)message.State;
            questComponent.UpdateQuestData(message.QuestId, questStatus);

            Log.Info($"Quest {message.QuestId} status updated to {questStatus}");
            
            await ETTask.CompletedTask;
        }
    }
}