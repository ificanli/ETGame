namespace ET.Server
{
    [Invoke(RobotCaseType.CreateRobot)]
    public class RobotCase_001_CreateRobot_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            // robotcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            Fiber robot = await fiber.CreateFiber(0, SceneType.Robot, "RobotCase_001");
            return ErrorCode.ERR_Success;
        }
    }
}