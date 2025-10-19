using System;

namespace ET
{
    [ChildOf(typeof(CoroutineLockQueue))]
    public class CoroutineLock: Entity, IAwake<long, long, int>, IDestroy
    {
        internal long type;
        internal long key;
        internal int level;
    }
}