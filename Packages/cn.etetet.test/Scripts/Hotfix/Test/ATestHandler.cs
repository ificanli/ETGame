using System;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例处理器抽象基类
    /// 继承自ATestHandler，使用Invoke机制进行分发
    /// </summary>
    [Test]
    public abstract class ATestHandler: HandlerObject, ITestHandler
    {
        public async ETTask<int> Handle(TestContext context)
        {
            Fiber parentFiber = context.Fiber;
            int serviceDiscoveryFiberId = 0;
            int subFiberId = 0;
            try
            {
                // 创建ServiceDiscovery Fiber,因为要最后释放，所以在这里创建，等其它fiber全部释放，再释放
                StartSceneConfig startConfig = StartSceneConfigCategory.Instance.GetBySceneName(nameof(SceneType.ServiceDiscovery));
                serviceDiscoveryFiberId = await parentFiber.CreateFiberWithId(
                    Const.ServiceDiscoveryFiberId, SchedulerType.Parent, startConfig.Id, startConfig.Zone,
                    SceneType.ServiceDiscovery, startConfig.Name);
                
                Fiber subFiber = await parentFiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.TestCase, this.GetType().Name);
                
                subFiberId = subFiber.Id;
                int ret = await this.Run(subFiber, context.Args);
                return ret;
            }
            finally
            {
                // case跑完会删除Test Fiber
                await parentFiber.RemoveFiber(subFiberId);
                await parentFiber.RemoveFiber(serviceDiscoveryFiberId);
            }
        }

        protected abstract ETTask<int> Run(Fiber fiber, TestArgs args);
    }
}