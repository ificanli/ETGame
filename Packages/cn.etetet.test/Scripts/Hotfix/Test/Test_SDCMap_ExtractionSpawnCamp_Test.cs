using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_SDCMap_ExtractionSpawnCamp_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_SDCMap_ExtractionSpawnCamp_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber mapManagerFiber = testFiber.GetFiber("MapManager");
            if (mapManagerFiber == null)
            {
                Log.Console("map manager fiber is null");
                return 1;
            }

            MapManagerComponent mapManagerComponent = mapManagerFiber.Root.GetComponent<MapManagerComponent>();
            if (mapManagerComponent == null)
            {
                Log.Console("map manager component is null");
                return 2;
            }

            long mapId = IdGenerater.Instance.GenerateId();
            EntityRef<MapManagerComponent> mapManagerComponentRef = mapManagerComponent;
            MapCopy mapCopy = await mapManagerComponent.GetMapAsync("SDCMap", mapId);
            mapManagerComponent = mapManagerComponentRef;
            if (mapCopy == null)
            {
                Log.Console("failed to create SDCMap map copy");
                return 3;
            }

            Fiber mapFiber = mapManagerFiber.GetFiber(mapCopy.FiberId);
            if (mapFiber == null)
            {
                Log.Console("SDCMap fiber is null");
                return 4;
            }

            Scene mapScene = mapFiber.Root;
            SpawnPointManagerComponent spawnPointManager = mapScene.GetComponent<SpawnPointManagerComponent>();
            if (spawnPointManager == null)
            {
                Log.Console("spawn point manager is null on SDCMap");
                return 5;
            }

            int playerUnitConfigId = HeroConfigHelper.GetDefaultUnitConfigId();
            if (playerUnitConfigId <= 0)
            {
                Log.Console("default player unit config id is invalid");
                return 6;
            }

            List<int> orderedTeamIds = new List<int>(spawnPointManager.TeamSpawnPoints.Keys);
            orderedTeamIds.Sort();
            if (orderedTeamIds.Count < 4)
            {
                Log.Console($"team spawn groups are insufficient: count={orderedTeamIds.Count}");
                return 7;
            }

            SceneNavmeshComponent sceneNavmesh = mapScene.GetComponent<SceneNavmeshComponent>();
            Log.Console(
                $"SDCMap extraction spawn probe: navPolyCount={sceneNavmesh?.NavPolyCount ?? -1}, teams=[{string.Join(",", orderedTeamIds)}]");

            HashSet<int> uniqueCampIds = new HashSet<int>();
            for (int seatIndex = 0; seatIndex < 4; ++seatIndex)
            {
                long playerId = 41001 + seatIndex;
                int teamId = MatchHelper.ResolveTeamId(GameModeType.Extraction, seatIndex, 4);
                int expectedCampId = seatIndex + 1;

                Unit unit = UnitFactory.Create(mapScene, playerId, playerUnitConfigId);
                if (unit == null)
                {
                    Log.Console($"failed to create player unit: seat={seatIndex}");
                    return 10 + seatIndex;
                }

                // Mirror the transfer enter flow on map change.
                MapUnitEnterHelper.EnsureMapRuntimeComponents(mapScene, unit);
                MapUnitEnterHelper.ApplyAssignedTeamIfNeeded(mapScene, unit, teamId);
                MapUnitEnterHelper.ApplySpawnPointIfNeeded(mapScene, unit, teamId);

                if (!spawnPointManager.PlayerTeamAssignments.TryGetValue(playerId, out int assignedTeamId))
                {
                    Log.Console($"team assignment missing: seat={seatIndex}, playerId={playerId}, inputTeamId={teamId}");
                    return 20 + seatIndex;
                }

                if (assignedTeamId != teamId)
                {
                    Log.Console($"team assignment mismatch: seat={seatIndex}, playerId={playerId}, inputTeamId={teamId}, assignedTeamId={assignedTeamId}");
                    return 30 + seatIndex;
                }

                CampComponent camp = unit.GetComponent<CampComponent>();
                if (camp == null)
                {
                    Log.Console($"camp component missing: seat={seatIndex}, playerId={playerId}");
                    return 40 + seatIndex;
                }

                if (camp.CampId != expectedCampId)
                {
                    Log.Console(
                        $"camp mismatch: seat={seatIndex}, playerId={playerId}, teamId={teamId}, expectedCampId={expectedCampId}, actualCampId={camp.CampId}");
                    return 50 + seatIndex;
                }

                uniqueCampIds.Add(camp.CampId);
                Log.Console(
                    $"player probe: seat={seatIndex}, playerId={playerId}, teamId={teamId}, campId={camp.CampId}, pos={unit.Position}");
            }

            if (uniqueCampIds.Count != 4)
            {
                Log.Console($"unique camp count mismatch: count={uniqueCampIds.Count}");
                return 60;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
