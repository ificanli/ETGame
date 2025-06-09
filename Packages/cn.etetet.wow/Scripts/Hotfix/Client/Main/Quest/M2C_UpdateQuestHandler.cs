namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_UpdateQuestHandler : MessageHandler<Scene, M2C_UpdateQuest>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateQuest message)
        {
            await ETTask.CompletedTask;
        }
    }
}