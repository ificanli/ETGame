using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    internal class MainThreadScheduler: IScheduler
    {
        private readonly ConcurrentQueue<Fiber> fiberQueue = new();
        private readonly ConcurrentQueue<Fiber> addQueue = new();

        // Fiber还没创建之前需要同步上下文，否则MainFiber task无法回调
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
            threadSynchronizationContext.Update();
            
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
            
            // 还原成原始上下文，unity的回调可能用到
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
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
            
            // 还原成原始上下文，unity的回调可能用到
            SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
        }


        public void AddToScheduler(Fiber fiber)
        {
            this.addQueue.Enqueue(fiber);
        }
    }
}