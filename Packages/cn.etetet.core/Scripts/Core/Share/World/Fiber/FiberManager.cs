using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ET
{
    public static class SchedulerType
    {
        public const int Main = -1;
        public const int Thread = -2;
        public const int ThreadPool = -3;
    }
    
    public class FiberManager: Singleton<FiberManager>, ISingletonAwake, ISingletonReverseDispose
    {
        private int idGenerator; 
        
        private readonly ConcurrentDictionary<int, IScheduler> schedulers = new();
        
        private MainThreadScheduler mainThreadScheduler;
        private ThreadPoolScheduler threadPoolScheduler;
        private ThreadScheduler threadScheduler;
        
        public void Awake()
        {
            this.idGenerator = 10000000; // 10000000以下为保留的用于StartSceneConfig的fiber id, 1个区配置1000个纤程，可以配置10000个区
            
            this.schedulers.Clear();
            
            this.mainThreadScheduler = new MainThreadScheduler(this);
            this.schedulers[SchedulerType.Main] = this.mainThreadScheduler;
            
#if (ENABLE_VIEW && UNITY_EDITOR) || UNITY_WEBGL
            this.schedulers[SchedulerType.Thread] = this.mainThreadScheduler;
            this.schedulers[SchedulerType.ThreadPool] = this.mainThreadScheduler;
#else
            this.threadPoolScheduler = new ThreadPoolScheduler(this);
            this.schedulers[SchedulerType.Thread] = this.threadPoolScheduler;
            
            this.threadScheduler = new ThreadScheduler(this);
            this.schedulers[SchedulerType.ThreadPool] = this.threadScheduler;
#endif
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
            this.mainThreadScheduler?.Dispose();
            this.threadPoolScheduler?.Dispose();
            this.threadScheduler?.Dispose();
            
            foreach ((int key, IScheduler value) in this.schedulers)
            {
                if (key < 0)
                {
                    continue;
                }
                value.Dispose();
            }
        }

        /// <summary>
        /// 创建纤程
        /// </summary>
        /// <param name="scheduler">纤程调度器，-1: 主线程，-2: 当前线程，-3: 线程池, 其它: 纤程</param>
        /// <param name="fiberId">纤程id</param>
        /// <param name="zone">区</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="iLog"></param>
        public async ETTask<int> CreateFiber(int scheduler, int fiberId, int zone, int sceneType, string name, ILog iLog = null)
        {
            if (sceneType == 0)
            {
                throw new Exception("fiberId is 0");
            }
            try
            {
                Log.Debug($"create fiber: {fiberId} {zone} {sceneType} {name} {scheduler}");
                Fiber fiber = new(fiberId, zone, sceneType, name, scheduler, iLog);

                if (!this.schedulers.TryAdd(fiberId, fiber))
                {
                    throw new Exception($"same fiber already existed, if you remove, please await Remove then Create fiber! {fiberId}");
                }
                
                // 检查，如果是sub fiber，zone必须跟parent相同
                if (scheduler > 0)
                {
                    Fiber parent = this.GetFiber(scheduler);
                    if (parent != null && parent.Zone != zone)
                    {
                        throw new Exception($"sub fiber zone must be same as parent: {zone} {parent.Zone}");
                    }
                }
                this.schedulers[scheduler].Add(fiberId);
                
                TaskCompletionSource<bool> tcs = new ();

                fiber.ThreadSynchronizationContext.Post(() =>
                {
                    Action().NoContext();
                });
                await tcs.Task;
                return fiberId;

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
        /// <param name="scheduler">纤程调度器，-1: 主线程，-2: 当前线程，-3: 线程池, 其它: 纤程</param>
        /// <param name="zone">区</param>
        /// <param name="sceneType">场景类型</param>
        /// <param name="name">纤程名称</param>
        /// <param name="ilog"></param>
        /// <returns>纤程id</returns>
        public async ETTask<int> CreateFiber(int scheduler, int zone, int sceneType, string name, ILog ilog = null)
        {
            int fiberId = this.GetFiberId();
            return await this.CreateFiber(scheduler, fiberId, zone, sceneType, name);
        }
        
        public async ETTask RemoveFiber(int id)
        {
            if (id < 0)
            {
                throw new Exception($"fiber id error, {id}");
            }
            
            Fiber fiber = this.GetFiber(id);
            TaskCompletionSource<bool> tcs = new();
            // 要扔到fiber线程执行，否则会出现线程竞争
            fiber.ThreadSynchronizationContext.Post(() =>
            {
                if (this.schedulers.Remove(id, out IScheduler f))
                {
                    f.Dispose();
                }
                tcs.SetResult(true);
            });
            await tcs.Task;
        }

        // 不允许外部调用，容易出现多线程问题, 只能通过消息通信，不允许直接获取其它Fiber引用
        internal Fiber GetFiber(int id)
        {
            if (id < 0)
            {
                throw new Exception($"fiber id error, {id}");
            }
            this.schedulers.TryGetValue(id, out IScheduler fiber);
            return fiber as Fiber;
        }

        public int Count()
        {
            return this.schedulers.Count - 3;
        }

        /// <summary>
        /// 重置FiberManager，主要是给机器人测试用例重置测试环境使用
        /// </summary>
        private void Reset()
        {
            this.isDisposed = true;
            this.Destroy();

            this.isDisposed = false;
            this.Awake();
        }

        /// <summary>
        /// 专门为机器人测试用例提供的安全重置方法
        /// </summary>
        public async ETTask<Fiber> ResetAndCreateFiber(int sceneType, string name)
        {
            this.Reset();
            int mainFiberId = await this.CreateFiber(SchedulerType.Main, sceneType, 0, sceneType, name);
            return this.GetFiber(mainFiberId);
        }
    }
}