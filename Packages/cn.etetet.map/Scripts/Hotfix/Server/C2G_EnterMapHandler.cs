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
			// 鍦℅ate涓婂姩鎬佸垱寤轰竴涓狹ap Scene锛屾妸Unit浠嶥B涓姞杞芥斁杩涙潵锛岀劧鍚庝紶閫佸埌鐪熸鐨凪ap涓紝杩欐牱鐧婚檰璺熶紶閫佺殑閫昏緫灏卞畬鍏ㄤ竴鏍蜂簡
			GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
			EntityRef<GateMapComponent> gateMapComponentRef = gateMapComponent;
			await gateMapComponent.Create(player.Id);
			gateMapComponent = gateMapComponentRef;
			Scene scene = gateMapComponent.Fiber.Root;
			// 杩欓噷鍙互浠嶥B涓姞杞経nit
			player = playerRef;

			// 璇诲彇璧疯閰嶇疆锛屼娇鐢ㄨ嫳闆勫搴旂殑 UnitConfigId锛堟病鏈夎捣瑁呭垯榛樿1001锛?
			int unitConfigId = HeroConfigHelper.GetDefaultUnitConfigId();
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

			// 搴旂敤璧疯锛堝皢璧疯瑁呭缁?Unit锛夆€?鍙澶囧埌 EquipmentComponent锛屼笉鍦?GateMap 鍒涘缓 Timer
			// 姝﹀櫒缁勪欢鍜岃嫳闆勬妧鑳藉湪 M2M_UnitTransferRequestHandler锛圡ap 鍦烘櫙锛変腑鍒濆鍖栵紝閬垮厤 Timer 娉ㄥ唽鍦?GateMap 涓婇殢鍏堕攢姣?
			Log.Info($"C2G_EnterMap: loadout={loadout != null}, IsConfirmed={loadout?.IsConfirmed}, HeroConfigId={loadout?.HeroConfigId}");
			if (loadout != null && loadout.IsConfirmed)
			{
				LoadoutHelper.ApplyLoadout(unit, loadout);
			}

			session = sessionRef;

			response.MyId = player.Id;
			// 绛夊埌涓€甯х殑鏈€鍚庨潰鍐嶄紶閫侊紝鍏堣G2C_EnterMap杩斿洖锛屽惁鍒欎紶閫佹秷鎭彲鑳芥瘮G2C_EnterMap杩樻棭
			TransferAtFrameFinish(player, unit, "Home", player.Id).Coroutine();
		}

		private static async ETTask TransferAtFrameFinish(Player player, Unit unit, string mapName, long mapId)
		{
			EntityRef<Unit> unitRef = unit;
			EntityRef<Player> playerRef = player;
			await unit.Fiber().WaitFrameFinish();

			unit = unitRef;
			await TransferHelper.TransferLock(unit, mapName, mapId, true);
			// 浼犻€佸畬鎴愶紝绉婚櫎GateMap Fiber
			player = playerRef;
			GateMapComponent gateMapComponent = player.GetComponent<GateMapComponent>();
			await player.Fiber().RemoveFiber(gateMapComponent.Fiber.Id);
			player = playerRef;
			player.RemoveComponent<GateMapComponent>();
		}
	}
}
