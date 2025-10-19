using System;
using System.Threading;

namespace ET
{
    [ChildOf(typeof(CoroutineLockQueue))]
    public class WaitCoroutineLock: Entity, IAwake, IDestroy
    {
        public object tcs;
    }
}