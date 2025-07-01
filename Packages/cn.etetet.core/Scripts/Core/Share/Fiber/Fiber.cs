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
    
    public class Fiber: IDisposable
    {
        // 该字段只能框架使用，绝对不能改成public，改了后果自负
        [StaticField]
        [ThreadStatic]
        public static Fiber Instance;
        
        public bool IsDisposed;
        
        public int Id;

        public int Zone;

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
        private readonly Dictionary<int, Fiber> subFibers = new();
        
        internal Fiber(int id, int zone, int sceneType, string name, ILog log = null)
        {
            this.Id = id;
            this.Zone = zone;
            this.EntitySystem = new EntitySystem();
            this.Mailboxes = new Mailboxes();
            this.ThreadSynchronizationContext = new ThreadSynchronizationContext();

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

                foreach ((int _, Fiber fiber) in this.subFibers)
                {
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

                foreach ((int _, Fiber fiber) in this.subFibers)
                {
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
        
        public Fiber GetSubFiber(int id)
        {
            return this.subFibers[id];
        }

        /// <summary>
        /// 创建子Fiber，子Fiber是由父Fiber驱动的
        /// </summary>
        public async ETTask<int> CreateSubFiber(int sceneType, string name)
        {
            if (sceneType == 0)
            {
                throw new Exception("sceneType is 0");
            }
            
            int fiberId = FiberManager.Instance.GetFiberId();
            try
            {
                Log.Info($"create sub fiber: {fiberId} {this.Zone} {sceneType} {name}, parent: {this.Id} {this.Root.SceneType} {this.Root.Name}");
                Fiber fiber = new(fiberId, this.Zone, sceneType, name, this.Log); // 子Fiber使用父Fiber的Log
                
                this.subFibers.Add(fiber.Id, fiber);

                await EventSystem.Instance.Invoke<FiberInit, ETTask>(sceneType, new FiberInit() {Fiber = fiber});
                
                return fiberId;
            }
            catch (Exception e)
            {
                throw new Exception($"create sub fiber error: {fiberId} {sceneType}", e);
            }
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

            foreach ((int _, Fiber fiber) in this.subFibers)
            {
                fiber.Dispose();
            }
        }
    }
}