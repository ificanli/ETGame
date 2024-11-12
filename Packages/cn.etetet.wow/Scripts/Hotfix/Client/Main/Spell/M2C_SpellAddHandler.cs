namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_SpellAddHandler: MessageHandler<Scene, M2C_SpellAdd>
    {
        protected override async ETTask Run(Scene root, M2C_SpellAdd message)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>().Scene;
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);

            await SpellHelper.Start(unit, message.SpellId, message.SpellConfigId);
        }
    }
}