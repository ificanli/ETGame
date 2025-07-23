using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    internal class MainThreadScheduler: IScheduler
    {
        private readonly ConcurrentQueue<Fiber> fiberQueue = new();
        private readonly ConcurrentQueue<Fiber> addQueue = new();


        public void Dispose()
        {
            this.addQueue.Clear();
            this.fiberQueue.Clear();
        }

        public void Update()
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
                this.fiberQueue.Enqueue(fiber);
                
                fiber.Update();
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

                this.fiberQueue.Enqueue(fiber);
                
                fiber.LateUpdate();
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