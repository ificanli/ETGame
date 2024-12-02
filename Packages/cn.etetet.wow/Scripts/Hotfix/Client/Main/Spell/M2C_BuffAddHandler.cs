namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_BuffAddHandler: MessageHandler<Scene, M2C_BuffAdd>
    {
        protected override async ETTask Run(Scene root, M2C_BuffAdd message)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>().Scene;
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);

            BuffHelper.CreateBuff(unit, message);
            await ETTask.CompletedTask;
        }
    }
}