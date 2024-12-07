namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SpellCastHandler: MessageLocationHandler<Unit, C2M_SpellCast>
    {
        protected override async ETTask Run(Unit unit, C2M_SpellCast message)
        {
            unit.GetComponent<TargetComponent>().Position = message.TargetPosition;
            SpellHelper.Cast(unit, message.SpellConfigId);
            await ETTask.CompletedTask;
        }
    }
}