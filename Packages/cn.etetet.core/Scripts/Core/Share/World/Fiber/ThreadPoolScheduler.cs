using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ET
{
    internal class ThreadPoolScheduler: IScheduler
    {
        private bool isDisposed;
        
        private readonly List<Thread> threads;

        private readonly ConcurrentQueue<Fiber> fiberQueue = new();

        public ThreadPoolScheduler()
        {
            int threadCount = Environment.ProcessorCount;
            this.threads = new List<Thread>(threadCount);
            for (int i = 0; i < threadCount; ++i)
            {
                Thread thread = new(this.Update);
                this.threads.Add(thread);
                thread.Start();
            }
        }

        private void Update()
        {
            int count = 0;
            int threadId = Environment.CurrentManagedThreadId;
            
            while (true)
            {
                if (count <= 0)
                {
                    Thread.Sleep(1);
                    
                    // count最小为1
                    count = this.fiberQueue.Count / this.threads.Count + 1;
                }

                --count;
                
                if (this.isDisposed)
                {
                    return;
                }
                
                if (!this.fiberQueue.TryDequeue(out Fiber fiber))
                {
                    Thread.Sleep(1);
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

                fiber.ThreadSynchronizationContext.ThreadId = threadId;
                fiber.Update();
                fiber.LateUpdate();
                fiber.ThreadSynchronizationContext.ThreadId = 0;
                
                this.fiberQueue.Enqueue(fiber);
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }
            
            this.isDisposed = true;
            
            foreach (Thread thread in this.threads)
            {
                thread.Join();
            }
            this.threads.Clear();
        }

        public void AddToScheduler(Fiber fiber)
        {
            this.fiberQueue.Enqueue(fiber);
        }
    }
}