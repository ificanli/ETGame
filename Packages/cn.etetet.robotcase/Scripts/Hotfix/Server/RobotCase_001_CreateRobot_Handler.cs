using System.Threading;
using ET.Client;

namespace ET.Server
{
    [Invoke(RobotCaseType.CreateRobot)]
    public class RobotCase_001_CreateRobot_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            // robotcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.Robot, "RobotCase_001");
            // robotcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            // 如果需要发送消息给服务端，可以这样做：
            
            // 调用机器人的发送消息方法，注意这里是父fiber调用了子fiber的call方法，按理来说，孩子的Call方法返回的时候，同步上下文会变成孩子的上下文
            // 由于这里父子fiber的同步上下文一样，所以没有问题
            RobotCase_001_PrepareData_Request prepareData001Request = RobotCase_001_PrepareData_Request.Create();
            await robot.Root.GetComponent<ClientSenderComponent>().Call(prepareData001Request);

            // 也可以直接访问服务器的数据，直接设置数据
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