namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_HomeContractsChangedHandler : MessageHandler<Scene, M2C_HomeContractsChanged>
    {
        protected override async ETTask Run(Scene root, M2C_HomeContractsChanged message)
        {
            HomeClientHelper.ApplyContractsChanged(root, message);
            await ETTask.CompletedTask;
        }
    }
}
