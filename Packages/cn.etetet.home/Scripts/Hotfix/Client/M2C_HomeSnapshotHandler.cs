namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_HomeSnapshotHandler : MessageHandler<Scene, M2C_HomeSnapshot>
    {
        protected override async ETTask Run(Scene root, M2C_HomeSnapshot message)
        {
            HomeClientHelper.ApplySnapshot(root, message);
            await ETTask.CompletedTask;
        }
    }
}
