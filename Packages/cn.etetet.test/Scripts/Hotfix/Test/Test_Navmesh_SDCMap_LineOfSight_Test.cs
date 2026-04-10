using Unity.Mathematics;
using ET.Server;

namespace ET.Test
{
    public class Test_Navmesh_SDCMap_LineOfSight_Test : ATestHandler
    {
        private const float MaxVerticalDelta = 4f;

        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Navmesh_SDCMap_LineOfSight_Test));
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
            Unit probeUnit = UnitFactory.Create(mapScene, IdGenerater.Instance.GenerateId(), 1003);
            if (probeUnit == null)
            {
                Log.Console("failed to create probe unit");
                return 5;
            }

            PathfindingComponent pathfinding = probeUnit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                Log.Console("probe unit has no pathfinding component");
                return 6;
            }

            float unitRadius = probeUnit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            if (unitRadius <= 0f)
            {
                unitRadius = 0.5f;
            }

            float3[] clearStarts =
            {
                new float3(44.78777f, 0f, 42.37098f),
                new float3(30.16146f, 0f, 55.59687f),
                new float3(77.6f, 0f, 8.4f),
            };

            float3[] clearEnds =
            {
                new float3(45.28777f, 0f, 42.37098f),
                new float3(30.66146f, 0f, 55.59687f),
                new float3(78.1f, 0f, 8.4f),
            };

            bool foundClearCandidate = false;
            bool clearPassed = false;
            float clearHitT = 0f;

            for (int i = 0; i < clearStarts.Length; ++i)
            {
                if (!pathfinding.TryRecastFindNearestPointForMovement(clearStarts[i], unitRadius, out _, out float startDistance))
                {
                    continue;
                }

                if (!pathfinding.TryRecastFindNearestPointForMovement(clearEnds[i], unitRadius, out _, out float endDistance))
                {
                    continue;
                }

                if (startDistance > 0.1f || endDistance > 0.1f)
                {
                    continue;
                }

                foundClearCandidate = true;
                clearPassed = pathfinding.HasLineOfSight(clearStarts[i], clearEnds[i], unitRadius, MaxVerticalDelta, out clearHitT);
                if (clearPassed)
                {
                    break;
                }
            }

            if (!foundClearCandidate)
            {
                Log.Console("no clear line-of-sight candidate projected onto navmesh");
                return 7;
            }

            if (!clearPassed)
            {
                Log.Console($"expected a clear line-of-sight candidate, lastHitT={clearHitT}");
                return 8;
            }

            float3 blockedStart = new float3(58.09703f, 0.3677568f, 25.00241f);
            float3 blockedEnd = new float3(58.09703f, 0.3677568f, 25.17641f);
            bool blockedPassed = pathfinding.HasLineOfSight(blockedStart, blockedEnd, unitRadius, MaxVerticalDelta, out float blockedHitT);
            if (blockedPassed)
            {
                Log.Console($"expected blocked line-of-sight, hitT={blockedHitT}");
                return 9;
            }

            bool verticalPassed = pathfinding.HasLineOfSight(clearStarts[0], new float3(clearStarts[0].x, clearStarts[0].y + 6f, clearStarts[0].z),
                unitRadius, MaxVerticalDelta, out float verticalHitT);
            if (verticalPassed)
            {
                Log.Console($"vertical delta should be blocked, hitT={verticalHitT}");
                return 10;
            }

            PathfindingComponent missingPathfinding = null;
            bool fallbackPassed = missingPathfinding.HasLineOfSight(clearStarts[0], clearEnds[0], unitRadius, MaxVerticalDelta, out float fallbackHitT);
            if (!fallbackPassed || fallbackHitT != float.MaxValue)
            {
                Log.Console($"missing pathfinding should fallback pass, passed={fallbackPassed}, hitT={fallbackHitT}");
                return 11;
            }

            Log.Console($"Test_Navmesh_SDCMap_LineOfSight_Test PASSED: clearHitT={clearHitT}, blockedHitT={blockedHitT}, fallbackHitT={fallbackHitT}");
            return ErrorCode.ERR_Success;
        }
    }
}
