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
			Unit unit = UnitFactory.Create(scene, player.Id, 1001);

			session = sessionRef;
			
			response.MyId = player.Id;
			// 等到一帧的最后面再传送，先让G2C_EnterMap返回，否则传送消息可能比G2C_EnterMap还早
			TransferAtFrameFinish(player, unit, "Map2", 0).NoContext();
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