using ET.Client;

namespace ET.Test
{
    public static class TestHelper
    {
        public static async ETTask<Fiber> CreateRobot(Fiber fiber, string robotName)
        {
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Client, robotName);
            Scene root = robot.Root;
            EntityRef<Scene> rootRef = root;
            root = rootRef;
            await LoginHelper.Login(root, "127.0.0.1:10101", robotName, "");
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
            return robot;
        }
        
        public static Fiber GetMap(Fiber testFiber, Fiber robotFiber)
        {
            Scene clientScene = robotFiber.Root;
            string mapName = clientScene.CurrentScene().Name;
            Fiber map = testFiber.GetFiber("MapManager").GetFiber(mapName);
            return map;
        }

        public static Unit GetServerUnit(Fiber testFiber, Fiber robotFiber)
        {
            Scene clientScene = robotFiber.Root;
            string mapName = clientScene.CurrentScene().Name;
            Fiber map = testFiber.GetFiber("MapManager").GetFiber(mapName);
            PlayerComponent player = clientScene.GetComponent<PlayerComponent>();
            Unit unit = map.Root.GetComponent<UnitComponent>().Get(player.MyId);
            return unit;
        }
    }
}