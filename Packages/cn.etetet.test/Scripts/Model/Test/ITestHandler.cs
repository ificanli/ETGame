namespace ET.Test
{
    public interface ITestHandler
    {
        ETTask<int> Handle(TestContext context);
    }
}