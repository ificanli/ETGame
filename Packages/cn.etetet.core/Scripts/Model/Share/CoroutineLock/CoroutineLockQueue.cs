using System;
using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(CoroutineLockQueueType))]
    public class CoroutineLockQueue: Entity, IAwake<long>, IDestroy
    {
        public long Type;

        public int MaxConcurrency { get; set; }

        public int RunningCount;
        
        public Queue<EntityRef<WaitCoroutineLock>> Queue = new();

        public int Count
        {
            get
            {
                return this.Queue.Count;
            }
        }
    }
}