using System;
using System.Threading;
using System.Threading.Tasks;

namespace ET
{
    public enum SchedulerType
    {
        Parent = -1,
        Main = 0,
        Thread = 1,
        ThreadPool = 2,
    }
    
    public class FiberManager: Singleton<FiberManager>, ISingletonAwake
    {
        private int idGenerator; 
        
        private readonly IScheduler[] schedulers = new IScheduler[3];
        
        private MainThreadScheduler mainThreadScheduler;

        private Fiber mainFiber;
        
        public void Awake()
        {
            this.idGenerator = 10000000; // 10000000以下为保留的用于StartSceneConfig的fiber id, 1个区配置1000个纤程，可以配置10000个区
            
            this.mainThreadScheduler = new MainThreadScheduler();
            this.schedulers[(int)SchedulerType.Main] = this.mainThreadScheduler;
            
#if (ENABLE_VIEW && UNITY_EDITOR) || UNITY_WEBGL
            this.schedulers[(int)SchedulerType.Thread] = this.mainThreadScheduler;
            this.schedulers[(int)SchedulerType.ThreadPool] = this.mainThreadScheduler;
#else
            this.schedulers[(int)SchedulerType.Thread] = new ThreadScheduler();
            this.schedulers[(int)SchedulerType.ThreadPool] = new ThreadPoolScheduler();
#endif
        }

        public override int RemoveOrder()
        {
            return 0;
        }
        
        public void Update()
        {
            this.mainThreadScheduler.Update();
        }

        public void LateUpdate()
        {
            this.mainThreadScheduler.LateUpdate();
        }

        protected override void Destroy()
        {
            // 把调度器全部停止
            foreach (IScheduler scheduler in this.schedulers)
            {
                scheduler.Dispose();
            }
            ((IScheduler)this.mainFiber).Dispose();
        }

        public async ETTask<int> CreateMainFiber(int sceneType)
        {
            this.mainFiber = await this.CreateFiber(SchedulerType.Main, 0, sceneType, "Main", null);
            return this.mainFiber.Id;
        }
        
        /// <summary>
        /// 创建纤程
        /// </summary>
        /// <param name="schedulerType">纤程调度器，-1: 主线程，-2: 当前线程，-3: 线程池, 其它: 纤程</param>
        /// <param name="fiberId">纤程id</param>
        /// <param name="zone">区</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="parent"></param>
        internal async ETTask<Fiber> CreateFiber(int fiberId, SchedulerType schedulerType, int zone, int sceneType, string name, Fiber parent)
        {
            if (sceneType == 0)
            {
                throw new Exception("fiberId is 0");
            }
            try
            {
                int parentId = parent?.Id ?? 0;
                Log.Debug($"create fiber: {fiberId} {zone} {sceneType} {name} {schedulerType} {parentId}");
                
                // 如果调度器是父fiber，那么日志也是父fiber的日志
                Fiber fiber = new(fiberId, zone, sceneType, name, schedulerType, parent);

                IScheduler iScheduler = schedulerType == SchedulerType.Parent ? parent : this.schedulers[(int)schedulerType];
                iScheduler.AddToScheduler(fiber);
                
                TaskCompletionSource<bool> tcs = new ();

                fiber.ThreadSynchronizationContext.Post(() =>
                {
                    Action().NoContext();
                });
                await tcs.Task;
                return fiber;

                async ETTask Action()
                {
                    try
                    {
                        // 根据Fiber的SceneType分发Init,必须在Fiber线程中执行
                        await EventSystem.Instance.Invoke<FiberInit, ETTask>(sceneType, new FiberInit() {Fiber = fiber});
                        tcs.SetResult(true);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"init fiber fail: {sceneType} {e}");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"create fiber error: {fiberId} {sceneType}", e);
            }
        }

        private int GetFiberId()
        {
            return Interlocked.Increment(ref this.idGenerator);
        }

        /// <summary>
        /// 创建纤程
        /// </summary>
        /// <param name="schedulerType">纤程调度器，-1: 主线程，-2: 当前线程，-3: 线程池, 其它: 纤程</param>
        /// <param name="zone">区</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="parent"></param>
        /// <returns>纤程id</returns>
        internal async ETTask<Fiber> CreateFiber(SchedulerType schedulerType, int zone, int sceneType, string name, Fiber parent)
        {
            int fiberId = this.GetFiberId();
            return await this.CreateFiber(fiberId, schedulerType, zone, sceneType, name, parent);
        }
    }
}