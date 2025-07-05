
namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
    [Module(ModuleName.Spell)]
	public class C2M_SelectTargetHandler : MessageLocationHandler<Unit, C2M_SelectTarget>
	{
		protected override async ETTask Run(Unit unit, C2M_SelectTarget message)
		{
			Unit target = unit.GetParent<UnitComponent>().Get(message.TargetUnitId);
			if (target == null)
			{
				return;
			}

			unit.GetComponent<TargetComponent>().Unit = target;
			
			await ETTask.CompletedTask;
		}
	}
}