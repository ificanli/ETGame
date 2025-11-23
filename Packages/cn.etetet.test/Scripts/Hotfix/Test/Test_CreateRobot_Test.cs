using ET.Client;

namespace ET.Test
{
    public class Test_CreateRobot_Test: ATestHandler
    {
        protected override async ETTask<int> Run(Fiber fiber, TestArgs args)
        {
            // testcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            Fiber robot = await TestHelper.CreateRobot(fiber, nameof(Test_CreateRobot_Test));
            // testcase可以直接用subfiber，这样最简单，可以直接拿到机器人的Fiber操作机器人数据，不需要使用消息通信
            
            // 直接访问服务器的数据，直接设置数据
            Fiber map = TestHelper.GetMap(fiber, robot);

            TestHelper.GetServerUnit(fiber, robot);
            
            return ErrorCode.ERR_Success;
        }
    }
}