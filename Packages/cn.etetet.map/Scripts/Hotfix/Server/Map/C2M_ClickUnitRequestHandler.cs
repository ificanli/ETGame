
namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class C2M_ClickUnitRequestHandler : MessageLocationHandler<Unit, C2M_ClickUnitRequest, M2C_ClickUnitResponse>
	{
		protected override async ETTask Run(Unit unit, C2M_ClickUnitRequest request, M2C_ClickUnitResponse response)
		{
			await ETTask.CompletedTask;
			
			Unit target = unit.GetParent<UnitComponent>().Get(request.UnitId);
			if (target == null)
			{
				return;
			}

			unit.GetComponent<TargetComponent>().Unit = target;
			
			// 返回点击展示的内容给客户端,根据unit跟npc不同的状态，显示不同的内容
		}
	}
}