namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_SpellAddHandler: MessageHandler<Scene, M2C_SpellAdd>
    {
        protected override async ETTask Run(Scene root, M2C_SpellAdd message)
        {
            Unit unit = UnitHelper.GetMyUnitFromClientScene(root);
            Spell spell = unit.GetComponent<SpellComponent>().GetChild<Spell>(message.SpellId);
            spell.GetComponent<ObjectWait>().Notify(new Wait_M2C_SpellAdd() {Message = message});
            await ETTask.CompletedTask;
        }
    }
}