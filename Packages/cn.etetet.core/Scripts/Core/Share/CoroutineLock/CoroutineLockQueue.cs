using System;
using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(CoroutineLockQueueType))]
    public class CoroutineLockQueue: Entity, IAwake<long>, IDestroy
    {
        internal long type;

        internal int maxConcurrency { get; set; }

        internal int runningCount;
        
        internal Queue<EntityRef<WaitCoroutineLock>> queue = new();

        internal int Count
        {
            get
            {
                return this.queue.Count;
            }
        }
    }
}