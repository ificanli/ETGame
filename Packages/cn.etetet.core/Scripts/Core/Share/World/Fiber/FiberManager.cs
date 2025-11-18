using System;
using System.Collections.Concurrent;
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
    
    public struct FiberMonitorInfo
    {
        public string Name;

        public int UpdateTimeUsed;
        
        public int LateUpdateTimeUsed;
        
        public FiberMonitorInfo(string name)
        {
            this.Name = name;
            this.UpdateTimeUsed = 0;
            this.LateUpdateTimeUsed = 0;
        }
    }
    
    public class FiberManager: Singleton<FiberManager>, ISingletonAwake
    {
        private int idGenerator;
        
        private readonly IScheduler[] schedulers = new IScheduler[3];
        
        private MainThreadScheduler mainThreadScheduler;

        private Fiber mainFiber;
        
        private readonly ThreadSynchronizationContext context = new();
        
        private readonly ConcurrentDictionary<int, FiberMonitorInfo> fiberMonitorInfos = new();
        
        public void AddMonitor(int fiberId, ref FiberMonitorInfo fiberMonitorInfo)
        {
            //if (fiberMonitorInfo.UpdateTimeUsed + fiberMonitorInfo.LateUpdateTimeUsed > 5)
            //{
            //    Log.Warning($"FiberMonitor Warn! FiberId: {fiberId} Name: {fiberMonitorInfo.Name} UpdateTimeUsed: {fiberMonitorInfo.UpdateTimeUsed} LateUpdateTimeUsed: {fiberMonitorInfo.LateUpdateTimeUsed}");
            //}
#if !UNITY  // 仅在非Unity环境下启用纤程监控，Unity下这个方法有GC开销
            this.fiberMonitorInfos[fiberId] = fiberMonitorInfo;
#endif
        }
        
        public void RemoveMonitor(int fiberId)
        {
#if !UNITY
            this.fiberMonitorInfos.TryRemove(fiberId, out _);
#endif
        }
        
        public void Awake()
        {
            SynchronizationContext.SetSynchronizationContext(context);
            
            this.mainThreadScheduler = new MainThreadScheduler();
            this.schedulers[(int)SchedulerType.Main] = this.mainThreadScheduler;

            if (Options.Instance.SingleThread == 1)
            {
                this.schedulers[(int)SchedulerType.Thread] = this.mainThreadScheduler;
                this.schedulers[(int)SchedulerType.ThreadPool] = this.mainThreadScheduler;
            }
            else
            {
                this.schedulers[(int)SchedulerType.Thread] = new ThreadScheduler();
                this.schedulers[(int)SchedulerType.ThreadPool] = new ThreadPoolScheduler();
            }
        }

        public override int RemoveOrder()
        {
            return 0;
        }
        
        public void Update()
        {
            this.context.Update();
            
            this.mainThreadScheduler.Update();
            
            // unity的回调需要用到Instance
            Fiber.Instance = this.mainFiber;
        }

        public void LateUpdate()
        {
            this.mainThreadScheduler.LateUpdate();
            
            // unity的回调需要用到Instance
            Fiber.Instance = this.mainFiber;
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

        public async ETTask<int> CreateMainFiber(int sceneType, string sceneName)
        {
            if (this.mainFiber != null)
            {
                throw new Exception("FiberManager is already created");
            }
            this.mainFiber = await this.CreateFiber(SchedulerType.Main, IdGenerater.Instance.GenerateId(), 0, sceneType, sceneName, null);
            return this.mainFiber.Id;
        }

        /// <summary>
        /// 创建纤程
        /// </summary>
        /// <param name="schedulerType">纤程调度器，-1: 主线程，-2: 当前线程，-3: 线程池, 其它: 纤程</param>
        /// <param name="fiberId">纤程id</param>
        /// <param name="rootId"></param>
        /// <param name="zone">区</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="parent"></param>
        internal async ETTask<Fiber> CreateFiber(int fiberId, SchedulerType schedulerType, long rootId, int zone, int sceneType, string name, Fiber parent)
        {
            if (sceneType == 0)
            {
                throw new Exception("fiberId is 0");
            }
            try
            {
                int parentId = parent?.Id ?? 0;
                Log.Debug($"create fiber: {name} {fiberId} {zone} {sceneType} {schedulerType} {parentId}");
                
                // 如果调度器是父fiber，那么日志也是父fiber的日志
                Fiber fiber = new(fiberId, rootId, zone, sceneType, name, schedulerType, parent);

                IScheduler iScheduler = schedulerType == SchedulerType.Parent ? parent : this.schedulers[(int)schedulerType];
                iScheduler.AddToScheduler(fiber);
                
                TaskCompletionSource<bool> tcs = new ();

                fiber.ThreadSynchronizationContext.Post(() =>
                {
                    Action().NoContext();
                });
                await tcs.Task;
                
                Log.Debug($"create fiber {name} finish");
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
        /// <param name="rootId"></param>
        /// <param name="zone">区</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="parent"></param>
        /// <returns>纤程id</returns>
        internal async ETTask<Fiber> CreateFiber(SchedulerType schedulerType, long rootId, int zone, int sceneType, string name, Fiber parent)
        {
            int fiberId = this.GetFiberId();
            return await this.CreateFiber(fiberId, schedulerType, rootId, zone, sceneType, name, parent);
        }
    }
}