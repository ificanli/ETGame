using ET.Client;
using ET.Server;

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
            ET.Client.PlayerComponent player = clientScene.GetComponent<ET.Client.PlayerComponent>();
            Unit unit = map.Root.GetComponent<UnitComponent>().Get(player.MyId);
            return unit;
        }

        public static UnitConfig FindUnitConfig(UnitType unitType)
        {
            UnitConfig fallback = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config == null)
                {
                    continue;
                }

                if (fallback == null && config.UnitType == UnitType.Player)
                {
                    fallback = config;
                }

                if (config.UnitType == unitType)
                {
                    return config;
                }
            }

            return fallback;
        }

        public static Unit CreateServerUnit(
            Scene scene,
            UnitType unitType,
            bool addBuffComponent = true,
            bool addProgress = false,
            bool addItemComponent = false,
            int campId = 0)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            UnitConfig unitConfig = FindUnitConfig(unitType);
            if (unitConfig == null)
            {
                return null;
            }

            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), unitConfig.Id);
            unit.UnitType = unitType;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in unitConfig.KV)
            {
                numeric.SetNoEvent(numericType, numericValue);
            }

            if (addBuffComponent)
            {
                unit.AddComponent<BuffComponent>();
            }

            if (addProgress)
            {
                RogueProgressHelper.EnsureProgress(unit, false);
            }

            if (addItemComponent)
            {
                unit.AddComponent<ET.Server.ItemComponent>();
            }

            if (campId > 0)
            {
                unit.AddComponent<CampComponent, int>(campId);
            }

            return unit;
        }
    }
}
