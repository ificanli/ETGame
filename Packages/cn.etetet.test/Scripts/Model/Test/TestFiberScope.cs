using System;
using System.Threading.Tasks;
using ET.Server;

namespace ET.Test
{
    public struct TestFiberScope : IAsyncDisposable
    {
        private readonly Fiber fiber;
        public Fiber TestFiber { get; private set; }
        private int serviceDiscoveryFiberId;

        public TestFiberScope(Fiber fiber)
        {
            this.fiber = fiber;
            this.TestFiber = null;
            this.serviceDiscoveryFiberId = 0;
        }

        public static async ETTask<TestFiberScope> Create(Fiber fiber, string testName)
        {
            TestFiberScope scope = new(fiber);
            
            StartSceneConfig startConfig = StartSceneConfigCategory.Instance.GetBySceneName(nameof(SceneType.ServiceDiscovery));
            scope.serviceDiscoveryFiberId = await fiber.CreateFiberWithId(
                ServiceDiscovery.ServiceDiscoveryFiberId, SchedulerType.Parent, startConfig.Id, startConfig.Zone,
                SceneType.ServiceDiscovery, startConfig.Name);

            scope.TestFiber = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, SceneType.TestCase, testName);
            
            return scope;
        }

        public static async ETTask<TestFiberScope> CreateOneFiber(Fiber fiber, int sceneType, string testName)
        {
            TestFiberScope scope = new(fiber)
            {
                TestFiber = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), 0, sceneType, testName)
            };

            return scope;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // 注意这里因为是在ValueTask里面，ValueTask不带上下文，所以必须设置上下文，否则会卡住await无法回调回来
                await fiber.RemoveFiber(this.TestFiber.Id).NewContext(null);
                if (this.serviceDiscoveryFiberId != 0)
                {
                    await fiber.RemoveFiber(this.serviceDiscoveryFiberId).NewContext(null);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
