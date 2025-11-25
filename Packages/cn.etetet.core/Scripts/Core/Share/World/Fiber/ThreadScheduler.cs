using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    // 一个Fiber一个固定的线程
    internal class ThreadScheduler: IScheduler
    {
        private bool isDisposed;
        
        private readonly ConcurrentDictionary<int, Thread> dictionary = new();

        private void Update(Fiber fiber)
        {
            int fiberId = fiber.Id;
            while (true)
            {
                if (this.isDisposed)
                {
                    return;
                }
                
                if (fiber.IsDisposed)
                {
                    this.dictionary.Remove(fiberId, out _);
                    return;
                }
                
                fiber.Update();
                fiber.LateUpdate();

                Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }
            this.isDisposed = true;
            
            foreach (var kv in this.dictionary.ToArray())
            {
                kv.Value.Join();
            }
            this.dictionary.Clear();
        }

        public void AddToScheduler(Fiber fiber)
        {
            Thread thread = new(() => this.Update(fiber));
            
            fiber.ThreadId = thread.ManagedThreadId;
            
            this.dictionary.TryAdd(fiber.Id, thread);
            thread.Start();
        }
    }
}