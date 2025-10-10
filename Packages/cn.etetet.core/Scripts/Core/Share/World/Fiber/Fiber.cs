using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ET
{
    public struct FiberDestroyEvent
    {
    }
    
    public class Fiber: IScheduler
    {
        // 该字段只能框架使用，绝对不能改成public，改了后果自负
        [StaticField]
        [ThreadStatic]
        private static Fiber instance;

        [StaticField]
        public static Fiber Instance
        {
            get
            {
                return instance;
            }
            internal set
            {
                instance = value;
                
                if (instance != null)
                {
                    instance.Log.SceneName = instance.Root.Name;
                    SynchronizationContext.SetSynchronizationContext(instance.ThreadSynchronizationContext);
                }
            }
        }
        
        public bool IsDisposed { get; private set; }
        
        public int Id { get; }

        public int Zone { get; }

        private EntityRef<Scene> root;
        
        public SchedulerType SchedulerType { get; }
        
        public int ParentFiberId { get; }

        private FiberMonitorInfo fiberMonitorInfo;

        private int instanceIdGenerator = 1;
        
        public int NewInstanceId()
        {
            return instanceIdGenerator++;
        }

        public Scene Root
        {
            get
            {
                return this.root;
            }
            private set
            {
                this.root = value;
            }
        }

        public EntitySystem EntitySystem { get; }
        public Mailboxes Mailboxes { get; private set; }
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; }
        public ILog Log { get; }

        private readonly Queue<ETTask> frameFinishTasks = new();
        
        private readonly Queue<Fiber> schedulerQueue = new();

        private readonly Dictionary<int, Fiber> children = new();
        
        internal Fiber(int id, long rootId, int zone, int sceneType, string name, SchedulerType schedulerType, Fiber parent)
        {
            this.Id = id;
            this.Zone = zone;
            this.SchedulerType = schedulerType;
            this.EntitySystem = new EntitySystem();
            this.Mailboxes = new Mailboxes();
            this.ParentFiberId = parent?.Id ?? 0;
            
            if (schedulerType == SchedulerType.Parent)
            {
                this.Log = parent.Log;
                // 注意调度是Parent的纤程，同步上下文必须是父亲的同步上下文
                // 因为父亲有可能会调用其方法，如果同步上下文不一样则父亲调用的上下文会发生改变
                // 比如RobotCase_001中，父亲会调用创建出来的子Fiber的发送消息方法，那么子fiber等call回来之后，当前上下文变成了子fiber的了
                // 这样导致父亲在调用前的上下文跟调用后的上下文发生了改变
                this.ThreadSynchronizationContext = parent.ThreadSynchronizationContext;
            }
            else
            {
                this.ThreadSynchronizationContext = new ThreadSynchronizationContext();
                
                LogInvoker logInvoker = new() { SceneName = name };
                this.Log = EventSystem.Instance.Invoke<LogInvoker, ILog>(logInvoker);
            }
            
            this.Root = new Scene(this, rootId, sceneType, name);
        }

        internal void Update()
        {
            Fiber saveInstance = Instance;
            Instance = this;
            
            try
            {
                this.fiberMonitorInfo = new FiberMonitorInfo(this.Root.Name);
                
                int t1 = Environment.TickCount;
                
                this.EntitySystem.Publish(new UpdateEvent());
                
                int t2 = Environment.TickCount;
                this.fiberMonitorInfo.UpdateTimeUsed = t2 - t1;

                int count = this.schedulerQueue.Count;
                while (count-- > 0)
                {
                    Fiber fiber = this.schedulerQueue.Dequeue();
                    
                    if (fiber.IsDisposed)
                    {
                        this.children.Remove(fiber.Id);
                        continue;
                    }
                    
                    this.schedulerQueue.Enqueue(fiber);

                    fiber.Update();                    
                }
            }
            catch (Exception e)
            {
                this.Log.Error(e);
            }
            finally
            {
                Instance = saveInstance;
            }
        }
        
        internal void LateUpdate()
        {
            Fiber saveInstance = Instance;
            Instance = this;
            
            try
            {
                int t1 = Environment.TickCount;
                
                this.EntitySystem.Publish(new LateUpdateEvent());
                FrameFinishUpdate();
                this.ThreadSynchronizationContext.Update();
                
                int t2 = Environment.TickCount;
                this.fiberMonitorInfo.LateUpdateTimeUsed = t2 - t1;

                FiberManager.Instance?.AddMonitor(this.Id, ref this.fiberMonitorInfo);

                int count = this.schedulerQueue.Count;
                while (count-- > 0)
                {
                    Fiber fiber = this.schedulerQueue.Dequeue();
                    
                    if (fiber.IsDisposed)
                    {
                        this.children.Remove(fiber.Id);
                        continue;
                    }
                    
                    this.schedulerQueue.Enqueue(fiber);
                    
                    fiber.LateUpdate();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                Instance = saveInstance;
            }
        }

        public async ETTask WaitFrameFinish()
        {
            ETTask task = ETTask.Create(true);
            this.frameFinishTasks.Enqueue(task);
            await task;
        }

        private void FrameFinishUpdate()
        {
            while (this.frameFinishTasks.Count > 0)
            {
                ETTask task = this.frameFinishTasks.Dequeue();
                task.SetResult();
            }
        }

        /// <summary>
        /// 创建的fiber在由该fiber调度，所以可以返回Fiber，父Fiber可以直接操作子Fiber
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="sceneType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async ETTask<Fiber> CreateFiber(long rootId, int zone, int sceneType, string name)
        {
            Fiber fiber = await FiberManager.Instance.CreateFiber(SchedulerType.Parent, rootId, zone, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber;
        }

        /// <summary>
        /// 这个会跟parent fiber在同一线程调度，所以可以返回Fiber，父Fiber可以直接操作子Fiber
        /// </summary>
        /// <param name="fiberId"></param>
        /// <param name="rootId"></param>
        /// <param name="zone"></param>
        /// <param name="sceneType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async ETTask<Fiber> CreateFiberWithId(int fiberId, long rootId, int zone, int sceneType, string name)
        {
            Fiber fiber = await FiberManager.Instance.CreateFiber(fiberId, SchedulerType.Parent, rootId, zone, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber;
        }

        public async ETTask<int> CreateFiber(SchedulerType schedulerType, long rootId, int zone, int sceneType, string name)
        {
            // 如果是单线程模式，强制设置为父fiber调度
            if (Options.Instance.SingleThread == 1)
            {
                schedulerType = SchedulerType.Parent;
            }
            
            Fiber fiber = await FiberManager.Instance.CreateFiber(schedulerType, rootId, zone, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber.Id;
        }
        
        public async ETTask<int> CreateFiberWithId(int fiberId, SchedulerType schedulerType, long rootId, int zone, int sceneType, string name)
        {
            // 如果是单线程模式，强制设置为父fiber调度
            if (Options.Instance.SingleThread == 1)
            {
                schedulerType = SchedulerType.Parent;
            }
            
            Fiber fiber = await FiberManager.Instance.CreateFiber(fiberId, schedulerType, rootId, zone, sceneType, name, this);
            this.children.Add(fiber.Id, fiber);
            return fiber.Id;
        }
        
        public async ETTask RemoveFiber(int fiberId)
        {
            if (!this.children.Remove(fiberId, out Fiber fiber))
            {
                return;
            }
            
            // 整个FiberManager释放了
            IScheduler scheduler = fiber;
            if (FiberManager.Instance == null)
            {
                scheduler.Dispose();
                return;
            }
            
            if (fiber.SchedulerType == SchedulerType.Parent)
            {
                foreach (int child in fiber.children.Keys.ToArray())
                {
                    await fiber.RemoveFiber(child);
                }
                await EventSystem.Instance.PublishAsync(fiber.Root, new FiberDestroyEvent());
                scheduler.Dispose();
                return;
            }
            
            TaskCompletionSource<bool> tcs = new();
            // 要扔到fiber线程执行，否则会出现线程竞争
            fiber.ThreadSynchronizationContext.Post(() =>
            {
                FiberDestroy().NoContext();
                return;

                async ETTask FiberDestroy()
                {
                    foreach (int child in fiber.children.Keys.ToArray())
                    {
                        await fiber.RemoveFiber(child);
                    }
                    Scene fiberRoot = (scheduler as Fiber).Root;
                    await EventSystem.Instance.PublishAsync(fiberRoot, new FiberDestroyEvent());
                    scheduler.Dispose();
                    tcs.SetResult(true);
                }
            });
            await tcs.Task;
        }

        public async ETTask RemoveFibers()
        {
            foreach (int i in this.children.Keys.ToArray())
            {
                await this.RemoveFiber(i);
            }
        }
        
        public Fiber GetFiber(int id)
        {
            if (!this.children.TryGetValue(id, out Fiber fiber))
            {
                return null;
            }

            if (fiber.SchedulerType != SchedulerType.Parent)
            {
                return null;
            }
            return fiber;
        }
        
        public Fiber GetFiber(string name)
        {
            foreach (Fiber fiber in this.children.Values)
            {
                if (fiber.SchedulerType != SchedulerType.Parent)
                {
                    continue;
                }
                if (fiber.Root.Name != name)
                {
                    continue;
                }

                return fiber;
            }
            return null;
        }

        /// <summary>
        /// 禁止逻辑调用此方法，应该由Fiber.Remove去移除一个fiber
        /// </summary>
        void IScheduler.Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            this.IsDisposed = true;
            try
            {
                this.Root.Dispose();
            }
            catch (Exception e)
            {
                this.Log.Error(e);
            }
            
            FiberManager.Instance?.RemoveMonitor(this.Id);
            
            foreach (int child in this.children.Keys.ToArray())
            {
                this.RemoveFiber(child).NoContext();
            }
        }

        public void AddToScheduler(Fiber fiber)
        {
            this.schedulerQueue.Enqueue(fiber);
        }
    }
}