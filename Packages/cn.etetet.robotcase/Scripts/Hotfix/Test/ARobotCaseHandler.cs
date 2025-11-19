using System;

namespace ET.Test
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
            // 使用安全的重置方法，直接获取新的Main Fiber
            Fiber subFiber = await parentFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.RobotCase, $"RobotCaseServer_{context.Args.Id}");
            int subFiberId = subFiber.Id;
            int ret = await this.Run(subFiber, context.Args);
            // case跑完会删除RobotCase Fiber
            await parentFiber.RemoveFiber(subFiberId);
            return ret;
        }

        protected abstract ETTask<int> Run(Fiber fiber, RobotCaseArgs args);
    }
}