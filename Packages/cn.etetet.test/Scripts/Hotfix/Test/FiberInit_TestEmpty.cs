namespace ET.Test
{
    [Invoke(SceneType.TestEmpty)]
    public class FiberInit_TestEmpty: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            await ETTask.CompletedTask;
        }
    }
}
