

using System.Collections.Generic;

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class G2Map_LogoutHandler : MessageLocationHandler<Unit, G2Map_Logout, Map2G_Logout>
	{
		protected override async ETTask Run(Unit unit, G2Map_Logout request, Map2G_Logout response)
		{
			EntityRef<Unit> unitRef = unit;
			ServiceDiscoveryProxy serviceDiscoveryProxy = unit.Root().GetComponent<ServiceDiscoveryProxy>();
			int zone = unit.Zone();

			List<ServiceInfo> serviceInfos = serviceDiscoveryProxy.GetBySceneTypeAndZone(SceneType.MapManager, zone);
			string mapManagerName = serviceInfos[0].SceneName;
			
			Map2MapManager_LogoutRequest managerLogoutRequest = Map2MapManager_LogoutRequest.Create();
			managerLogoutRequest.MapName = unit.Scene().Name.GetSceneConfigName();
			managerLogoutRequest.UnitId = unit.Id;
			managerLogoutRequest.MapId = unit.Scene().Id;
			await serviceDiscoveryProxy.Call(mapManagerName, managerLogoutRequest);
			unit = unitRef;
			UnitComponent unitComponent = unit.GetParent<UnitComponent>();
			unitComponent.Remove(unit.Id);
		}
	}
}
