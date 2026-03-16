using Unity.Mathematics;
using ET.Server;

namespace ET.Test
{
    public class Test_Navmesh_SDCMap_Query_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Navmesh_SDCMap_Query_Test));
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
            SceneNavmeshComponent sceneNavmesh = mapScene.GetComponent<SceneNavmeshComponent>();
            if (sceneNavmesh == null)
            {
                Log.Console("scene navmesh component is null on SDCMap");
                return 5;
            }

            if (sceneNavmesh.NavPolyCount <= 0)
            {
                Log.Console($"scene navmesh has no polygons: polyCount={sceneNavmesh.NavPolyCount}, useAllPass={sceneNavmesh.UseAllPassQueryFilter}");
                return 6;
            }

            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Console("unit component is null on SDCMap");
                return 7;
            }

            Unit probeUnit = UnitFactory.Create(mapScene, IdGenerater.Instance.GenerateId(), 1003);
            if (probeUnit == null)
            {
                Log.Console("failed to create probe monster unit");
                return 8;
            }

            PathfindingComponent pathfinding = probeUnit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                Log.Console("probe monster has no PathfindingComponent");
                return 9;
            }

            float unitRadius = probeUnit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            if (unitRadius <= 0f)
            {
                unitRadius = 0.5f;
            }

            float3[] probes =
            {
                new float3(8.2f, 0f, 12.7f),
                new float3(77.6f, 0f, 8.4f),
                new float3(99.33467f, 0f, 95.66857f),
                new float3(30.16146f, 0f, 55.59687f),
                new float3(61.6f, 0f, 111f),
                new float3(44.78777f, 0f, 42.37098f),
            };

            int projectedHits = 0;
            for (int i = 0; i < probes.Length; ++i)
            {
                if (pathfinding.TryRecastFindNearestPointForMovement(probes[i], unitRadius, out _, out _))
                {
                    projectedHits++;
                }
            }

            using ListComponent<float3> path = ListComponent<float3>.Create();
            pathfinding.Find(probes[0], probes[1], path, unitRadius);
            if (path.Count < 2)
            {
                Log.Console($"pathfinding result invalid: points={path.Count}");
                return 10;
            }

            if (projectedHits == 0)
            {
                Log.Console($"nearest poly projection failed on all probes: hits={projectedHits}/{probes.Length}, pathPoints={path.Count}, navPolyCount={sceneNavmesh.NavPolyCount}");
                return 11;
            }

            Log.Console($"SDCMap nav query basic check passed: projectedHits={projectedHits}/{probes.Length}, pathPoints={path.Count}");
            return ErrorCode.ERR_Success;
        }
    }
}
