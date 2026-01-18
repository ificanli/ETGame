namespace ET.Test
{
    public class Test_CreateRobot_Test: ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_CreateRobot_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_CreateRobot_Test));
            
            Fiber map = TestHelper.GetMap(testFiber, robot);

            TestHelper.GetServerUnit(testFiber, robot);
            
            return ErrorCode.ERR_Success;
        }
    }
}
