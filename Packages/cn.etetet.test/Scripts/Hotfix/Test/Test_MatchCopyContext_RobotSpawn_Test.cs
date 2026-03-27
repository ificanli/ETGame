using ET.Server;

namespace ET.Test
{
    public class Test_MatchCopyContext_RobotSpawn_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(
                context.Fiber, nameof(Test_MatchCopyContext_RobotSpawn_Test));

            Fiber testFiber = scope.TestFiber;
            Fiber mapManagerFiber = testFiber.GetFiber("MapManager");
            if (mapManagerFiber == null)
            {
                Log.Console("map manager fiber is null");
                return 1;
            }

            Scene mapManagerRoot = mapManagerFiber.Root;
            MapManagerComponent mapManagerComponent = mapManagerRoot.GetComponent<MapManagerComponent>();
            if (mapManagerComponent == null)
            {
                Log.Console("map manager component is null");
                return 2;
            }

            long mapId = IdGenerater.Instance.GenerateId();
            EntityRef<MapManagerComponent> mapManagerComponentRef = mapManagerComponent;
            MapCopy mapCopy = await mapManagerComponent.GetMapAsync("Home", mapId);
            mapManagerComponent = mapManagerComponentRef;
            if (mapCopy == null)
            {
                Log.Console("map copy is null");
                return 3;
            }

            Fiber mapFiber = mapManagerFiber.GetFiber(mapCopy.FiberId);
            if (mapFiber == null)
            {
                Log.Console("map fiber is null");
                return 4;
            }

            Scene mapScene = mapFiber.Root;
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Console("unit component is null");
                return 5;
            }

            MatchCopyContextComponent contextComponent = mapScene.AddComponent<MatchCopyContextComponent>();

            const long player1 = 21001;
            const long player2 = 21002;
            const long robot1 = -91001;

            Match2Map_InitMatchCopyRequest request = Match2Map_InitMatchCopyRequest.Create();
            request.GameMode = GameModeType.OneVsOne;
            request.MapName = "Home";
            request.MapId = mapId;
            request.HumanPlayerIds.Add(player1);
            request.HumanPlayerIds.Add(player2);
            request.RobotPlayerIds.Add(robot1);
            request.PlayerIds.Add(player1);
            request.PlayerIds.Add(player2);
            request.PlayerIds.Add(robot1);
            request.TeamIds.Add(1);
            request.TeamIds.Add(2);
            request.TeamIds.Add(3);

            MatchCopyContextHelper.Initialize(mapScene, contextComponent, request);

            if (contextComponent.RobotsSpawned)
            {
                Log.Console("robots should not spawn before humans enter");
                return 6;
            }

            if (unitComponent.Get(robot1) != null)
            {
                Log.Console("robot unit should not exist before humans enter");
                return 7;
            }

            int playerUnitConfigId = HeroConfigHelper.GetDefaultUnitConfigId();
            if (playerUnitConfigId <= 0)
            {
                Log.Console("default player unit config id is invalid");
                return 8;
            }

            Unit human1 = UnitFactory.Create(mapScene, player1, playerUnitConfigId);
            MatchCopyContextHelper.OnHumanPlayerEntered(mapScene, human1);

            if (contextComponent.RobotsSpawned)
            {
                Log.Console("robots should not spawn after only one human enters");
                return 9;
            }

            if (unitComponent.Get(robot1) != null)
            {
                Log.Console("robot unit should not exist after only one human enters");
                return 10;
            }

            Unit human2 = UnitFactory.Create(mapScene, player2, playerUnitConfigId);
            MatchCopyContextHelper.OnHumanPlayerEntered(mapScene, human2);

            if (!contextComponent.RobotsSpawned)
            {
                Log.Console("robots should spawn after all humans enter");
                return 11;
            }

            Unit robotUnit = unitComponent.Get(robot1);
            if (robotUnit == null)
            {
                Log.Console("robot unit should exist after all humans enter");
                return 12;
            }

            if (robotUnit.UnitType != UnitType.Player)
            {
                Log.Console($"robot unit type mismatch: {robotUnit.UnitType}");
                return 13;
            }

            MatchRobotComponent matchRobot = robotUnit.GetComponent<MatchRobotComponent>();
            if (matchRobot == null)
            {
                Log.Console("match robot marker should remain after setup");
                return 14;
            }

            if (matchRobot.HeroConfigId <= 0 || matchRobot.MainWeaponConfigId <= 0 || matchRobot.AIBuffConfigId <= 0)
            {
                Log.Console($"match robot profile invalid: hero={matchRobot.HeroConfigId}, main={matchRobot.MainWeaponConfigId}, ai={matchRobot.AIBuffConfigId}");
                return 15;
            }

            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(matchRobot.HeroConfigId);
            if (heroConfig == null || robotUnit.ConfigId != heroConfig.UnitConfigId)
            {
                Log.Console($"robot hero config mismatch: hero={matchRobot.HeroConfigId}, unitConfig={robotUnit.ConfigId}, expected={heroConfig?.UnitConfigId ?? 0}");
                return 16;
            }

            EquipmentComponent equipmentComponent = robotUnit.GetComponent<EquipmentComponent>();
            if (equipmentComponent == null || equipmentComponent.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId != matchRobot.MainWeaponConfigId)
            {
                Log.Console($"robot main weapon mismatch: equipped={equipmentComponent?.GetEquippedItem(EquipmentSlotType.MainHand)?.ConfigId ?? 0}, expected={matchRobot.MainWeaponConfigId}");
                return 17;
            }

            WeaponComponent weaponComponent = robotUnit.GetComponent<WeaponComponent>();
            if (weaponComponent == null || weaponComponent.Slot1WeaponId != matchRobot.MainWeaponConfigId)
            {
                Log.Console($"robot weapon runtime mismatch: slot1={weaponComponent?.Slot1WeaponId ?? 0}, expected={matchRobot.MainWeaponConfigId}");
                return 18;
            }

            if (robotUnit.NumericComponent?.GetAsInt(NumericType.AI) != matchRobot.AIBuffConfigId)
            {
                Log.Console($"robot ai numeric mismatch: ai={robotUnit.NumericComponent?.GetAsInt(NumericType.AI) ?? 0}, expected={matchRobot.AIBuffConfigId}");
                return 19;
            }

            SpawnPointManagerComponent spawnPointManager = mapScene.GetComponent<SpawnPointManagerComponent>();
            if (spawnPointManager != null &&
                (!spawnPointManager.PlayerTeamAssignments.TryGetValue(robot1, out int robotTeamId) || robotTeamId != 3))
            {
                Log.Console($"robot team assignment mismatch: {robotTeamId}");
                return 20;
            }

            if (contextComponent.EnteredHumanPlayerIds.Count != 2)
            {
                Log.Console($"entered human count mismatch: {contextComponent.EnteredHumanPlayerIds.Count}");
                return 21;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
