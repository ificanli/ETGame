using System;
using System.Collections.Generic;

namespace ET
{
    public static class FiberHelper
    {
        public static ActorId GetActorId(this Entity self)
        {
            Fiber root = self.Fiber();
            return new ActorId(root.Process, root.Id, self.InstanceId);
        }
    }
    
    public class Fiber: IScheduler
    {
        // 该字段只能框架使用，绝对不能改成public，改了后果自负
        [StaticField]
        [ThreadStatic]
        public static Fiber Instance;
        
        public bool IsDisposed;
        
        public int Id;

        public int Zone;

        public int Scheduler {get; internal set;}

        private EntityRef<Scene> root;

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

        public Address Address
        {
            get
            {
                return new Address(this.Process, this.Id);
            }
        }

        public int Process
        {
            get
            {
                return Options.Instance.Process;
            }
        }

        public EntitySystem EntitySystem { get; }
        public Mailboxes Mailboxes { get; private set; }
        public ThreadSynchronizationContext ThreadSynchronizationContext { get; }
        public ILog Log { get; }

        private readonly Queue<ETTask> frameFinishTasks = new();
        
        // 子Fiber, 子Fiber的Log使用父Fiber的Log, 子Fiber是由父Fiber调度
        private readonly Queue<int> subFibers = new();
        
        internal Fiber(int id, int zone, int sceneType, string name, int scheduler, ILog log = null)
        {
            this.Id = id;
            this.Zone = zone;
            this.EntitySystem = new EntitySystem();
            this.Mailboxes = new Mailboxes();
            this.ThreadSynchronizationContext = new ThreadSynchronizationContext();
            this.Scheduler = scheduler;

            if (log != null)
            {
                this.Log = log;
            }
            else
            {
                LogInvoker logInvoker = new() { Fiber = this.Id, Process = this.Process, SceneName = name };
                this.Log = EventSystem.Instance.Invoke<LogInvoker, ILog>(logInvoker);
            }
            
            this.Root = new Scene(this, id, 1, sceneType, name);
        }

        internal void Update()
        {
            try
            {
                Instance = this;
                this.EntitySystem.Publish(new UpdateEvent());

                int count = this.subFibers.Count;
                while (count-- > 0)
                {
                    int fiberId = this.subFibers.Dequeue();
                    this.subFibers.Enqueue(fiberId);

                    Fiber fiber = FiberManager.Instance.GetFiber(fiberId);
                    if (fiber == null)
                    {
                        continue;
                    }
                    fiber.Update();                    
                }
            }
            catch (Exception e)
            {
                this.Log.Error(e);
            }
            finally
            {
                Instance = null;
            }
        }
        
        internal void LateUpdate()
        {
            try
            {
                Instance = this;
                this.EntitySystem.Publish(new LateUpdateEvent());
                FrameFinishUpdate();
                this.ThreadSynchronizationContext.Update();

                int count = this.subFibers.Count;
                while (count-- > 0)
                {
                    int fiberId = this.subFibers.Dequeue();
                    this.subFibers.Enqueue(fiberId);
                    
                    Fiber fiber = FiberManager.Instance.GetFiber(fiberId);
                    if (fiber == null)
                    {
                        continue;
                    }
                    fiber.LateUpdate();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                Instance = null;
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

        public async ETTask<int> CreateFiber(int sceneType, string name)
        {
            return await FiberManager.Instance.CreateFiber(this.Id, this.Zone, sceneType, name, this.Log);
        }
        
        public Fiber GetFiber(int id)
        {
            Fiber fiber = FiberManager.Instance.GetFiber(id);
            if (fiber == null || fiber.Scheduler != this.Id)
            {
                throw new Exception($"get sub fiber error: {id}, parent: {this.Id}");
            }
            return fiber;
        }

        public void Dispose()
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

            while (this.subFibers.Count > 0)
            {
                int fiberId = this.subFibers.Dequeue();
                Fiber subFiber = FiberManager.Instance.GetFiber(fiberId);
                if (subFiber == null)
                {
                    continue;
                }
                subFiber.Dispose();
            }
        }

        public void Add(int fiberId)
        {
            this.subFibers.Enqueue(fiberId);
        }
    }
}