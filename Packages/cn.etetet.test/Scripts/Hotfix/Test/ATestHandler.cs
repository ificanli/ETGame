using System;

namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例处理器抽象基类
    /// 继承自ATestHandler，使用Invoke机制进行分发
    /// </summary>
    public abstract class ATestHandler: HandlerObject, ITestHandler
    {
        public async ETTask<int> Handle(TestContext context)
        {
            Fiber parentFiber = context.Fiber;
            // 使用安全的重置方法，直接获取新的Main Fiber
            Fiber subFiber = await parentFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.TestCase, $"Test_{context.Args.Package}_{context.Args.Name}");
            int subFiberId = subFiber.Id;
            int ret = await this.Run(subFiber, context.Args);
            // case跑完会删除Test Fiber
            await parentFiber.RemoveFiber(subFiberId);
            return ret;
        }

        protected abstract ETTask<int> Run(Fiber fiber, TestArgs args);
    }
}