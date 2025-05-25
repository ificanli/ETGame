

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class G2M_SessionDisconnectHandler : MessageLocationHandler<Unit, G2M_SessionDisconnect>
	{
		protected override async ETTask Run(Unit unit, G2M_SessionDisconnect message)
		{
			UnitComponent unitComponent = unit.GetParent<UnitComponent>();
			unitComponent.Remove(unit.Id);
			await ETTask.CompletedTask;
		}
	}
}