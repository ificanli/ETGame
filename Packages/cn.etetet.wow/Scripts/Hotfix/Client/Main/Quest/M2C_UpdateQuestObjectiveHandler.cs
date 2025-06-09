namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_UpdateQuestObjectiveHandler : MessageHandler<Scene, M2C_UpdateQuestObjective>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateQuestObjective message)
        {
            await ETTask.CompletedTask;
        }
    }
}