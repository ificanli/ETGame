namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SubmitQuestHandler : MessageLocationHandler<Unit, C2M_SubmitQuest, M2C_SubmitQuest>
    {
        protected override async ETTask Run(Unit unit, C2M_SubmitQuest request, M2C_SubmitQuest response)
        {
            await ETTask.CompletedTask;
        }
    }
}