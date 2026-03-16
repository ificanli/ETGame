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
			LoadoutComponent loadout = player.GetComponent<LoadoutComponent>();
			EntityRef<LoadoutComponent> loadoutRef = loadout;
			// 在 Gate 上动态创建一个 Map Scene，把 Unit 从 DB 中加载进来，然后再传送到真正的 Map，
			// 这样登录和传送的逻辑就完全一致了
			GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
			EntityRef<GateMapComponent> gateMapComponentRef = gateMapComponent;
			await gateMapComponent.Create(player.Id);
			gateMapComponent = gateMapComponentRef;
			Scene scene = gateMapComponent.Fiber.Root;
			// 这里可以从 DB 中加载 Unit
			player = playerRef;
			loadout = loadoutRef;

			// 读取起装配置，使用英雄对应的 UnitConfigId
			int unitConfigId = HeroConfigHelper.GetDefaultUnitConfigId();
			if (loadout != null && loadout.HeroConfigId > 0)
			{
				HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(loadout.HeroConfigId);
				if (heroConfig != null)
				{
					unitConfigId = heroConfig.UnitConfigId;
				}
			}

			Unit unit = UnitFactory.Create(scene, player.Id, unitConfigId);
			unit.AddComponent<UnitGateInfoComponent>().ActorId = player.GetComponent<PlayerSessionComponent>().GetActorId();

			// Login -> Home 不强制要求已确认起装。
			// 只有已确认的起装快照才会被应用到当前 Unit。
			Log.Info($"C2G_EnterMap: loadout={loadout != null}, IsConfirmed={loadout?.IsConfirmed}, HeroConfigId={loadout?.HeroConfigId}");
			if (loadout != null && loadout.IsConfirmed)
			{
				LoadoutHelper.ApplyLoadout(unit, loadout);
			}

			session = sessionRef;

			response.MyId = player.Id;
			// 等到一帧的最后再传送，先让 G2C_EnterMap 返回，否则传送消息可能比 G2C_EnterMap 更早
			TransferAtFrameFinish(player, unit, "Home", player.Id).Coroutine();
		}

		private static async ETTask TransferAtFrameFinish(Player player, Unit unit, string mapName, long mapId)
		{
			EntityRef<Unit> unitRef = unit;
			EntityRef<Player> playerRef = player;
			await unit.Fiber().WaitFrameFinish();

			unit = unitRef;
			await TransferHelper.TransferLock(unit, mapName, mapId, true);
			// 传送完成后，移除 GateMap Fiber
			player = playerRef;
			GateMapComponent gateMapComponent = player.GetComponent<GateMapComponent>();
			await player.Fiber().RemoveFiber(gateMapComponent.Fiber.Id);
			player = playerRef;
			player.RemoveComponent<GateMapComponent>();
		}
	}
}
