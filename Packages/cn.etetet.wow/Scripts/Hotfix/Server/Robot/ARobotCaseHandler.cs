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
            // 使用安全的重置方法，直接获取新的Main Fiber
            Fiber fiber = await FiberManager.CreateRoot(SceneType.Main);
            return await this.Run(fiber, context.Args);
        }

        protected abstract ETTask<int> Run(Fiber fiber, RobotCaseArgs args);
    }
}