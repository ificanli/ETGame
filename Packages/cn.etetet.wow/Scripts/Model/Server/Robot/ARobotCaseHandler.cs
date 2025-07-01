using System;

namespace ET.Server
{
    /// <summary>
    /// 机器人测试用例回调参数
    /// </summary>
    public struct RobotCaseContext
    {
        public Fiber Fiber;
        public RobotCaseArgs Args;
    }

    /// <summary>
    /// 机器人测试用例处理器抽象基类
    /// 继承自AInvokeHandler，使用Invoke机制进行分发
    /// </summary>
    public abstract class ARobotCaseHandler: AInvokeHandler<RobotCaseContext, ETTask<int>>
    {
        public override async ETTask<int> Handle(RobotCaseContext context)
        {
            return await this.Run(context.Fiber, context.Args);
        }

        protected abstract ETTask<int> Run(Fiber fiber, RobotCaseArgs args);
    }
}