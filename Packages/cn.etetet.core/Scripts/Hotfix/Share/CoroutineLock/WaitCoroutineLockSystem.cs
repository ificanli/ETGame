using System;

namespace ET
{
    [EntitySystemOf(typeof(WaitCoroutineLock))]
    public static partial class WaitCoroutineLockSystem
    {
        [Invoke(TimerCoreInvokeType.CoroutineTimeout)]
        public class WaitCoroutineLockTimer: ATimer<WaitCoroutineLock>
        {
            protected override void Run(WaitCoroutineLock self)
            {
                if (self == null)
                {
                    return;
                }
                self.SetException(new Exception("coroutine is timeout!"));
            }
        }
        
        [EntitySystem]
        private static void Awake(this WaitCoroutineLock self)
        {
            self.tcs = ETTask<CoroutineLock>.Create(true);
        }
        [EntitySystem]
        private static void Destroy(this WaitCoroutineLock self)
        {
            self.tcs = null;
        }
        
        public static void SetResult(this WaitCoroutineLock self, CoroutineLock coroutineLock)
        {
            if (self.tcs == null)
            {
                throw new NullReferenceException("SetResult tcs is null");
            }
            ETTask<CoroutineLock> t = (ETTask<CoroutineLock>)self.tcs;
            self.Dispose();
            t.SetResult(coroutineLock);
        }

        public static void SetException(this WaitCoroutineLock self, Exception exception)
        {
            if (self.tcs == null)
            {
                throw new NullReferenceException("SetException tcs is null");
            }
            ETTask<CoroutineLock> t = (ETTask<CoroutineLock>)self.tcs;
            self.Dispose();
            t.SetException(exception);
        }
        
        public static async ETTask<CoroutineLock> Wait(this WaitCoroutineLock self)
        {
            ETTask<CoroutineLock> t = (ETTask<CoroutineLock>)self.tcs;
            return await t;
        }
    }
}