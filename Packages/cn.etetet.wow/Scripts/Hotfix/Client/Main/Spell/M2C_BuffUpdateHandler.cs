namespace ET.Client
{
    [Module(ModuleName.Spell)]
    [MessageHandler(SceneType.WOW)]
    public class M2C_BuffUpdateHandler: MessageHandler<Scene, M2C_BuffUpdate>
    {
        protected override async ETTask Run(Scene root, M2C_BuffUpdate message)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>().Scene;
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            Buff buff = unit.GetComponent<BuffComponent>().GetChild<Buff>(message.BuffId);
            BuffHelper.UpdateBuff(buff, message.ExpireTime, message.TickTime, message.Stack);
            await ETTask.CompletedTask;
        }
    }
}