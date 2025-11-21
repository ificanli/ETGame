using ET.Client;

namespace ET.Test
{
    public class Test_CreateRobot_Test: ATestHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, TestArgs args)
        {
            // robotcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Robot, nameof(Test_CreateRobot_Test));
            // robotcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            // 直接访问服务器的数据，直接设置数据
            string mapName = robot.Root.CurrentScene().Name;
            
            Fiber map = fiber.GetFiber("MapManager").GetFiber(mapName);
            if (map == null)
            {
                Log.Error($"not found robot map {mapName}");
            }
            
            // 获取Unit的Id
            Client.PlayerComponent playerComponent = robot.Root.GetComponent<Client.PlayerComponent>();
            
            // 获取服务端Unit
            Unit serverUnit = map.Root.GetComponent<UnitComponent>().Get(playerComponent.MyId);
            
            return ErrorCode.ERR_Success;
        }
    }
}