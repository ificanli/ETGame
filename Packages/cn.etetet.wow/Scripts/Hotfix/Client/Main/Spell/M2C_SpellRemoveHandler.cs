namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_SpellRemoveHandler: MessageHandler<Scene, M2C_SpellRemove>
    {
        protected override async ETTask Run(Scene root, M2C_SpellRemove message)
        {
            Unit unit = UnitHelper.GetMyUnitFromClientScene(root);
            unit.GetComponent<ObjectWait>().Notify(new Wait_M2C_SpellRemove() {Message = message});

            await ETTask.CompletedTask;
        }
    }
}