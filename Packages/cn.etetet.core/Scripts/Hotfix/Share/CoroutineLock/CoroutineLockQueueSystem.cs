namespace ET
{
    [EntitySystemOf(typeof(CoroutineLockQueue))]
    public static partial class CoroutineLockQueueSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockQueue self, long type)
        {
            self.Type = type;
            self.RunningCount = 0;
            self.MaxConcurrency = 1;
        }
        
        [EntitySystem]
        private static void Destroy(this CoroutineLockQueue self)
        {
            self.Queue.Clear();
            self.Type = 0;
            self.MaxConcurrency = 0;
            self.RunningCount = 0;
        }
        
        public static async ETTask<CoroutineLock> Wait(this CoroutineLockQueue self, int time)
        {
            CoroutineLock coroutineLock = null;
            if (self.RunningCount < self.MaxConcurrency)
            {
                ++self.RunningCount;
                coroutineLock = self.AddChild<CoroutineLock, long, long, int>(self.Type, self.Id, 1, true);
                return coroutineLock;
            }

            WaitCoroutineLock waitCoroutineLock = self.AddChild<WaitCoroutineLock>(true);
            self.Queue.Enqueue(waitCoroutineLock);
            if (time > 0)
            {
                long tillTime = TimeInfo.Instance.ServerFrameTime() + time;
                self.Root().GetComponent<TimerComponent>().NewOnceTimer(tillTime, TimerCoreInvokeType.CoroutineTimeout, waitCoroutineLock);
            }
            coroutineLock = await waitCoroutineLock.Wait();
            return coroutineLock;
        }

        // 返回值，是否找到了一个有效的协程锁
        public static bool Notify(this CoroutineLockQueue self, int level)
        {
            --self.RunningCount;
            
            // 尝试唤醒等待队列中的协程，直到达到并发上限
            while (self.Queue.Count > 0)
            {
                WaitCoroutineLock waitCoroutineLock = self.Queue.Dequeue();

                if (waitCoroutineLock == null)
                {
                    continue;
                }

                CoroutineLock coroutineLock = self.AddChild<CoroutineLock, long, long, int>(self.Type, self.Id, level, true);
                waitCoroutineLock.SetResult(coroutineLock);
                
                // 达到最大并发数量
                if (++self.RunningCount >= self.MaxConcurrency)
                {
                    break;
                }
            }
            
            // 如果还有运行中的协程或等待队列不为空，则队列继续存活
            return self.RunningCount > 0 || self.Queue.Count > 0;
        }
    }
}