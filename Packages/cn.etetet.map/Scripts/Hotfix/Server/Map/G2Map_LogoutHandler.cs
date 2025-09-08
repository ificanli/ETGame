

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class G2Map_LogoutHandler : MessageLocationHandler<Unit, G2Map_Logout, Map2G_Logout>
	{
		protected override async ETTask Run(Unit unit, G2Map_Logout request, Map2G_Logout response)
		{
			EntityRef<Unit> unitRef = unit;
			StartSceneConfig mapManagerConfig = StartSceneConfigCategory.Instance.GetOneBySceneType(unit.Zone(), SceneType.MapManager);
			Map2MapManager_LogoutRequest managerLogoutRequest = Map2MapManager_LogoutRequest.Create();
			managerLogoutRequest.MapName = unit.Scene().Name;
			managerLogoutRequest.UnitId = unit.Id;
			managerLogoutRequest.MapId = unit.Scene().Id;
			await unit.Root().GetComponent<MessageSender>().Call(mapManagerConfig.ActorId, managerLogoutRequest);
			unit = unitRef;
			UnitComponent unitComponent = unit.GetParent<UnitComponent>();
			unitComponent.Remove(unit.Id);
		}
	}
}