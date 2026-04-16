namespace ET.Server
{
    public static class GateMapTransferHelper
    {
        public static async ETTask TransferPlayerToMap(Player player, string mapName, long mapId, int teamId = 0, bool waitFrameFinish = false)
        {
            if (player == null)
            {
                return;
            }

            EntityRef<Player> playerRef = player;
            LoadoutComponent loadout = player.GetComponent<LoadoutComponent>();
            EntityRef<LoadoutComponent> loadoutRef = loadout;
            Fiber playerFiber = player.Fiber();

            GateMapComponent existingGateMap = player.GetComponent<GateMapComponent>();
            if (existingGateMap != null)
            {
                await CleanupGateMap(player, existingGateMap, playerFiber);
                player = playerRef;
                if (player == null)
                {
                    return;
                }
            }

            GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
            EntityRef<GateMapComponent> gateMapComponentRef = gateMapComponent;
            await gateMapComponent.Create(player.Id);
            gateMapComponent = gateMapComponentRef;
            if (gateMapComponent == null)
            {
                return;
            }

            int gateMapFiberId = gateMapComponent.Fiber.Id;

            try
            {
                gateMapComponent = gateMapComponentRef;
                player = playerRef;
                loadout = loadoutRef;
                if (gateMapComponent == null || player == null)
                {
                    return;
                }

                Scene scene = gateMapComponent.Fiber.Root;
                int unitConfigId = ResolveUnitConfigId(loadout);
                Unit unit = UnitFactory.Create(scene, player.Id, unitConfigId);

                PlayerSessionComponent playerSessionComponent = player.GetComponent<PlayerSessionComponent>();
                if (playerSessionComponent == null)
                {
                    return;
                }

                UnitGateInfoComponent gateInfo = unit.AddComponent<UnitGateInfoComponent>();
                gateInfo.ActorId = playerSessionComponent.GetActorId();
                gateInfo.PlayerActorId = player.GetActorId();

                Log.Info(
                    $"[GateMapTransfer] create temp unit: player={player.Id}, map={mapName}, mapId={mapId}, teamId={teamId}, waitFrameFinish={waitFrameFinish}, hasLoadout={loadout != null}, isConfirmed={loadout?.IsConfirmed}");

                if (loadout != null && loadout.IsConfirmed)
                {
                    LoadoutHelper.ApplyLoadout(unit, loadout);
                }

                if (waitFrameFinish)
                {
                    EntityRef<Unit> unitRef = unit;
                    await unit.Fiber().WaitFrameFinish();
                    unit = unitRef;
                    if (unit == null)
                    {
                        return;
                    }
                }

                await TransferHelper.TransferLock(unit, mapName, mapId, teamId, true);
            }
            finally
            {
                await CleanupGateMap(playerRef, gateMapComponentRef, playerFiber, gateMapFiberId);
            }
        }

        private static int ResolveUnitConfigId(LoadoutComponent loadout)
        {
            int unitConfigId = HeroConfigHelper.GetDefaultUnitConfigId();
            if (loadout == null || loadout.HeroConfigId <= 0)
            {
                return unitConfigId;
            }

            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(loadout.HeroConfigId);
            if (heroConfig != null)
            {
                unitConfigId = heroConfig.UnitConfigId;
            }

            return unitConfigId;
        }

        private static async ETTask CleanupGateMap(Player player, GateMapComponent gateMapComponent, Fiber playerFiber)
        {
            if (player == null || gateMapComponent == null)
            {
                return;
            }

            await CleanupGateMap((EntityRef<Player>)player, (EntityRef<GateMapComponent>)gateMapComponent, playerFiber, gateMapComponent.Fiber?.Id ?? 0);
        }

        private static async ETTask CleanupGateMap(EntityRef<Player> playerRef, EntityRef<GateMapComponent> gateMapComponentRef, Fiber playerFiber, int gateMapFiberId)
        {
            if (playerFiber != null && gateMapFiberId != 0)
            {
                await playerFiber.RemoveFiber(gateMapFiberId);
            }

            Player player = playerRef;
            GateMapComponent gateMapComponent = gateMapComponentRef;
            if (player == null || gateMapComponent == null)
            {
                return;
            }

            GateMapComponent currentGateMap = player.GetComponent<GateMapComponent>();
            if (currentGateMap == gateMapComponent)
            {
                player.RemoveComponent<GateMapComponent>();
            }
        }
    }
}
