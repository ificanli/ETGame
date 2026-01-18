using System;
using System.Threading.Tasks;
using ET.Server;

namespace ET.Test
{
    public struct TestFiberScope(Fiber fiber) : IAsyncDisposable
    {
        public Fiber TestFiber { get; private set; }
        private int serviceDiscoveryFiberId;

        public static async ETTask<TestFiberScope> Create(Fiber fiber, string testName)
        {
            TestFiberScope scope = new(fiber);
            
            StartSceneConfig startConfig = StartSceneConfigCategory.Instance.GetBySceneName(nameof(SceneType.ServiceDiscovery));
            scope.serviceDiscoveryFiberId = await fiber.CreateFiberWithId(
                ConstFiberId.ServiceDiscoveryFiberId, SchedulerType.Parent, startConfig.Id, startConfig.Zone, SceneType.ServiceDiscovery, startConfig.Name);

            scope.TestFiber = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.TestCase, testName);
            
            return scope;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // 注意这里因为是在ValueTask里面，ValueTask不带上下文，所以必须设置上下文，否则会卡住await无法回调回来
                await fiber.RemoveFiber(this.TestFiber.Id).NewContext(null);
                await fiber.RemoveFiber(this.serviceDiscoveryFiberId).NewContext(null);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
