using System;

namespace ET.Server
{
    /// <summary>
    /// 机器人测试用例处理器抽象基类
    /// 继承自AInvokeHandler，使用Invoke机制进行分发
    /// </summary>
    public abstract class ARobotCaseHandler: AInvokeHandler<RobotCaseContext, ETTask<int>>
    {
        public override async ETTask<int> Handle(RobotCaseContext context)
        {
            Fiber parentFiber = context.Fiber;
            int subFiberId = 0;
            try
            {
                // 使用安全的重置方法，直接获取新的Main Fiber
                Fiber subFiber = await context.Fiber.CreateFiber(0, SceneType.RobotCase, $"{context.Args.Id}");
                subFiberId = subFiber.Id;
                int ret = await this.Run(subFiber, context.Args);
                return ret;
            }
            finally
            {
                // case跑完会删除RobotCase Fiber
                await parentFiber.RemoveFiber(subFiberId);
            }
        }

        protected abstract ETTask<int> Run(Fiber fiber, RobotCaseArgs args);
    }
}