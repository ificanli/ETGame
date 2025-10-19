using System;
using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(CoroutineLockQueueType))]
    public class CoroutineLockQueue: Entity, IAwake<long>, IDestroy
    {
        public long type;

        public int maxConcurrency { get; set; }

        public int runningCount;
        
        public Queue<EntityRef<WaitCoroutineLock>> queue = new();

        public int Count
        {
            get
            {
                return this.queue.Count;
            }
        }
    }
}