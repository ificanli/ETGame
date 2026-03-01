using ET.Server;

namespace ET.Test
{
    public class Test_MapManager_SpecifiedMapIdCreate_Test: ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_MapManager_SpecifiedMapIdCreate_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber mapManagerFiber = testFiber.GetFiber("MapManager");
            if (mapManagerFiber == null)
            {
                Log.Console($"mapManagerFiber is null");
                return 1;
            }

            Scene mapManagerRoot = mapManagerFiber.Root;
            MapManagerComponent mapManagerComponent = mapManagerRoot.GetComponent<MapManagerComponent>();
            if (mapManagerComponent == null)
            {
                Log.Console($"mapManagerComponent is null");
                return 2;
            }

            long mapId = IdGenerater.Instance.GenerateId();
            MapCopy mapCopy = mapManagerComponent.GetMap("Home", mapId);
            if (mapCopy != null)
            {
                Log.Console($"map copy already exists before create, mapId={mapId}");
                return 3;
            }

            EntityRef<MapManagerComponent> mapManagerComponentRef = mapManagerComponent;
            mapCopy = await mapManagerComponent.GetMapAsync("Home", mapId);
            mapManagerComponent = mapManagerComponentRef;
            if (mapCopy == null)
            {
                Log.Console($"get map async returns null, mapId={mapId}");
                return 4;
            }

            MapCopy createdMapCopy = mapManagerComponent.GetMap("Home", mapId);
            if (createdMapCopy == null)
            {
                Log.Console($"map copy not found after create, mapId={mapId}");
                return 5;
            }

            if (createdMapCopy.Id != mapId)
            {
                Log.Console($"map copy id mismatch, expected={mapId}, actual={createdMapCopy.Id}");
                return 6;
            }

            if (createdMapCopy.FiberId == 0)
            {
                Log.Console($"map copy fiber id invalid, mapId={mapId}");
                return 7;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
