using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    internal class MainThreadScheduler: IScheduler
    {
        private readonly ConcurrentQueue<Fiber> fiberQueue = new();
        private readonly ConcurrentQueue<Fiber> addQueue = new();
        private readonly ThreadSynchronizationContext threadSynchronizationContext = new();

        public MainThreadScheduler()
        {
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
        }

        public void Dispose()
        {
            this.addQueue.Clear();
            this.fiberQueue.Clear();
        }

        public void Update()
        {
            this.threadSynchronizationContext.Update();
            
            int count = this.fiberQueue.Count;
            while (count-- > 0)
            {
                if (!this.fiberQueue.TryDequeue(out Fiber fiber))
                {
                    continue;
                }
                if (fiber == null)
                {
                    continue;
                }
                
                if (fiber.IsDisposed)
                {
                    continue;
                }
                
                fiber.Update();
                this.fiberQueue.Enqueue(fiber);
            }
        }

        public void LateUpdate()
        {
            int count = this.fiberQueue.Count;
            while (count-- > 0)
            {
                if (!this.fiberQueue.TryDequeue(out Fiber fiber))
                {
                    continue;
                }
                if (fiber == null)
                {
                    continue;
                }

                if (fiber.IsDisposed)
                {
                    continue;
                }

                fiber.LateUpdate();
                this.fiberQueue.Enqueue(fiber);
            }

            while (this.addQueue.Count > 0)
            {
                this.addQueue.TryDequeue(out Fiber fiber);
                this.fiberQueue.Enqueue(fiber);
            }
        }


        public void AddToScheduler(Fiber fiber)
        {
            this.addQueue.Enqueue(fiber);
        }
    }
}