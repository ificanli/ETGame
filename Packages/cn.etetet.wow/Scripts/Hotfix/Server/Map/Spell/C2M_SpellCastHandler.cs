namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SpellCastHandler: MessageLocationHandler<Unit, C2M_SpellCast>
    {
        protected override async ETTask Run(Unit unit, C2M_SpellCast message)
        {
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = unit.GetParent<UnitComponent>().Get(message.TargetUnitId);
            if (target != null)
            {
                targetComponent.Unit = target;
                targetComponent.Position = message.TargetPosition;
            }

            ETCancellationToken cancellationToken = new();
            SpellHelper.Cast(unit, message.SpellConfigId).WithContext(cancellationToken);
            await ETTask.CompletedTask;
        }
    }
}