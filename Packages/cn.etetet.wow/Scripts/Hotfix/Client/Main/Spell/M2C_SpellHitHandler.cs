namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_SpellHitHandler: MessageHandler<Scene, M2C_SpellHit>
    {
        protected override async ETTask Run(Scene root, M2C_SpellHit message)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>().Scene;
            Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.UnitId);
            Spell spell = unit.GetComponent<SpellComponent>().GetChild<Spell>(message.SpellId);
            spell.GetComponent<ObjectWait>().Notify(new Wait_M2C_SpellHit() {Message = message});
            await ETTask.CompletedTask;
        }
    }
}