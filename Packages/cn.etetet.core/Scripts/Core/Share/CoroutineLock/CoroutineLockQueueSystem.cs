namespace ET
{
    [EntitySystemOf(typeof(CoroutineLockQueue))]
    public static partial class CoroutineLockQueueSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockQueue self, long type)
        {
            self.type = type;
            self.runningCount = 0;
            self.maxConcurrency = 1;
        }
        
        [EntitySystem]
        private static void Destroy(this CoroutineLockQueue self)
        {
            self.queue.Clear();
            self.type = 0;
            self.maxConcurrency = 0;
            self.runningCount = 0;
        }
        
        public static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockQueue self)
        {
            CoroutineLock coroutineLock = null;
            if (self.runningCount < self.maxConcurrency)
            {
                ++self.runningCount;
                coroutineLock = self.AddChild<CoroutineLock, long, long, int>(self.type, self.Id, 1, true);
                return coroutineLock;
            }

            WaitCoroutineLock waitCoroutineLock = self.AddChild<WaitCoroutineLock>(true);
            self.queue.Enqueue(waitCoroutineLock);
            coroutineLock = await waitCoroutineLock.Wait();
            return coroutineLock;
        }

        // 返回值，是否找到了一个有效的协程锁
        public static bool Notify(this CoroutineLockQueue self, int level)
        {
            --self.runningCount;
            
            // 尝试唤醒等待队列中的协程，直到达到并发上限
            while (self.queue.Count > 0)
            {
                WaitCoroutineLock waitCoroutineLock = self.queue.Dequeue();

                if (waitCoroutineLock == null)
                {
                    continue;
                }

                CoroutineLock coroutineLock = self.AddChild<CoroutineLock, long, long, int>(self.type, self.Id, level, true);
                waitCoroutineLock.SetResult(coroutineLock);
                
                // 达到最大并发数量
                if (++self.runningCount >= self.maxConcurrency)
                {
                    break;
                }
            }
            
            // 如果还有运行中的协程或等待队列不为空，则队列继续存活
            return self.runningCount > 0 || self.queue.Count > 0;
        }
    }
}