namespace ET.Server
{
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_EnterMapHandler : MessageSessionHandler<C2G_EnterMap, G2C_EnterMap>
	{
		protected override async ETTask Run(Session session, C2G_EnterMap request, G2C_EnterMap response)
		{
			EntityRef<Session> sessionRef = session;
			Player player = session.GetComponent<SessionPlayerComponent>().Player;
			EntityRef<Player> playerRef = player;
			// 在Gate上动态创建一个Map Scene，把Unit从DB中加载放进来，然后传送到真正的Map中，这样登陆跟传送的逻辑就完全一样了
			GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
			EntityRef<GateMapComponent> gateMapComponentRef = gateMapComponent;
			await gateMapComponent.Create(player.Id);
			gateMapComponent = gateMapComponentRef;
			Scene scene = gateMapComponent.Fiber.Root;
			// 这里可以从DB中加载Unit
			player = playerRef;

			// 读取起装配置，使用英雄对应的 UnitConfigId（没有起装则默认1001）
			int unitConfigId = 1001;
			LoadoutComponent loadout = player.GetComponent<LoadoutComponent>();
			if (loadout != null && loadout.IsConfirmed && loadout.HeroConfigId > 0)
			{
				HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(loadout.HeroConfigId);
				if (heroConfig != null)
				{
					unitConfigId = heroConfig.UnitConfigId;
				}
			}

			Unit unit = UnitFactory.Create(scene, player.Id, unitConfigId);
			unit.AddComponent<UnitGateInfoComponent>().ActorId = player.GetComponent<PlayerSessionComponent>().GetActorId();

			// 应用起装（将起装装备给 Unit）
			if (loadout != null && loadout.IsConfirmed)
			{
				LoadoutHelper.ApplyLoadout(unit, loadout);
			}

			session = sessionRef;

			response.MyId = player.Id;
			// 等到一帧的最后面再传送，先让G2C_EnterMap返回，否则传送消息可能比G2C_EnterMap还早
			TransferAtFrameFinish(player, unit, "Home", 0).Coroutine();
		}

		private static async ETTask TransferAtFrameFinish(Player player, Unit unit, string mapName, int mapId)
		{
			EntityRef<Unit> unitRef = unit;
			EntityRef<Player> playerRef = player;
			await unit.Fiber().WaitFrameFinish();
			
			unit = unitRef;
			await TransferHelper.TransferLock(unit, mapName, mapId, true);
			// 传送完成，移除GateMap Fiber
			player = playerRef;
			GateMapComponent gateMapComponent = player.GetComponent<GateMapComponent>();
			await player.Fiber().RemoveFiber(gateMapComponent.Fiber.Id);
			player = playerRef;
			player.RemoveComponent<GateMapComponent>();
		}
	}
}