using System;
using System.Runtime.CompilerServices;

namespace ET
{
    public static class CoroutineLockHelper
    {
        public static async ETTask LockTime(Fiber fiber, EntityRef<CoroutineLock> coroutineLockRef, int lockTime, 
            [CallerLineNumber] int line = 0, [CallerFilePath] string filePath = "")
        {
            TimerComponent timerComponent = fiber.Root.GetComponent<TimerComponent>();
            await timerComponent.WaitAsync(lockTime);
            CoroutineLock coroutineLock = coroutineLockRef;
            if (coroutineLock != null)
            {
                throw new Exception($"Coroutine lock time timeout {filePath}:{line}");
            }
        }
    }
}