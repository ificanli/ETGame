namespace ET.Server
{
    [Invoke(RobotCaseType.CreateRobot)]
    public class RobotCase_001_CreateRobot_Handler: ARobotCaseHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, RobotCaseArgs args)
        {
            RobotManagerComponent robotManagerComponent = fiber.Root.GetComponent<RobotManagerComponent>();
            await robotManagerComponent.NewRobot(SchedulerType.Parent, "Robot_001");
            return ErrorCode.ERR_Success;
        }
    }
}