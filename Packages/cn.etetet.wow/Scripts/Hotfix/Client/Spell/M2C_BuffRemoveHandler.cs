namespace ET.Client
{
    [Module(ModuleName.Spell)]
    [MessageHandler(SceneType.Client)]
    public class M2C_BuffRemoveHandler: MessageHandler<Scene, M2C_BuffRemove>
    {
        protected override async ETTask Run(Scene root, M2C_BuffRemove message)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>().Scene;
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);

            BuffHelper.RemoveBuff(unit, message.BuffId, (BuffFlags)message.RemoveType);
            await ETTask.CompletedTask;
        }
    }
}