namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例处理器抽象基类
    /// 继承自ATestHandler，使用Invoke机制进行分发
    /// </summary>
    [Test]
    public abstract class ATestHandler: HandlerObject, ITestHandler
    {
        public abstract ETTask<int> Handle(TestContext context);
    }
}
