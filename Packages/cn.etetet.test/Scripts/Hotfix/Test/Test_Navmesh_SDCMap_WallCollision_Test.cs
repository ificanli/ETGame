using Unity.Mathematics;
using ET.Server;

namespace ET.Test
{
    public class Test_Navmesh_SDCMap_WallCollision_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Navmesh_SDCMap_WallCollision_Test));
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
                Log.Console("failed to create probe monster unit");
                return 5;
            }

            PathfindingComponent pathfinding = probeUnit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                Log.Console("probe monster has no PathfindingComponent");
                return 6;
            }

            float unitRadius = probeUnit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            if (unitRadius <= 0f)
            {
                unitRadius = 0.5f;
            }

            // 这组坐标来自真实撞墙日志，预期结果是“部分前进但绝不穿墙”。
            float3 startPos = new float3(58.09703f, 0.3677568f, 25.00241f);
            float3 endPos = new float3(58.09703f, 0.3677568f, 25.17641f);

            if (!pathfinding.TryRecastFindNearestPointForMovement(startPos, unitRadius, out float3 projectedStart, out float projectedStartDistance))
            {
                Log.Console("wall collision start pos is not on navmesh");
                return 7;
            }

            if (projectedStartDistance > 0.05f)
            {
                Log.Console($"wall collision start projection too far: dist={projectedStartDistance:F4}, projected={projectedStart}");
                return 8;
            }

            bool reachedExpectedPos = pathfinding.TryMoveAlongSurface(startPos, endPos, out float3 safePos);
            if (reachedExpectedPos)
            {
                Log.Console($"expected wall hit but move reached target, start={startPos}, end={endPos}, safe={safePos}");
                return 9;
            }

            float requestedDistance = math.distance(new float2(startPos.x, startPos.z), new float2(endPos.x, endPos.z));
            float actualDistance = math.distance(new float2(startPos.x, startPos.z), new float2(safePos.x, safePos.z));
            float remainingDistance = math.distance(new float2(safePos.x, safePos.z), new float2(endPos.x, endPos.z));
            if (actualDistance <= 0.0005f)
            {
                Log.Console($"wall collision moved too little, actual={actualDistance:F4}, safe={safePos}");
                return 10;
            }

            if (actualDistance >= requestedDistance - 0.001f)
            {
                Log.Console($"wall collision moved too far, requested={requestedDistance:F4}, actual={actualDistance:F4}, safe={safePos}");
                return 11;
            }

            if (remainingDistance <= 0.01f)
            {
                Log.Console($"wall collision remaining distance too small, remaining={remainingDistance:F4}, safe={safePos}");
                return 12;
            }

            if (!pathfinding.TryRecastFindNearestPointForMovement(safePos, unitRadius, out float3 projectedSafe, out float projectedSafeDistance))
            {
                Log.Console($"safe pos is not on navmesh, safe={safePos}");
                return 13;
            }

            if (projectedSafeDistance > 0.05f)
            {
                Log.Console($"safe pos projection too far: dist={projectedSafeDistance:F4}, projected={projectedSafe}, safe={safePos}");
                return 14;
            }

            Log.Console(
                $"SDCMap wall collision passed: start={startPos}, end={endPos}, safe={safePos}, requested={requestedDistance:F4}, actual={actualDistance:F4}, remaining={remainingDistance:F4}");
            return ErrorCode.ERR_Success;
        }
    }
}
