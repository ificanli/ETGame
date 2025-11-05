using System.Runtime.CompilerServices;

namespace ET
{
    [EntitySystemOf(typeof(CoroutineLockComponent))]
    public static partial class CoroutineLockComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLockComponent self)
        {
        }
        
        [EntitySystem]
        private static void Update(this CoroutineLockComponent self)
        {
            // 循环过程中会有对象继续加入队列，这里避免死循环，每帧处理当前队列长度的对象
            // 因为协程锁可能是同步处理完成，这样会导致await之后的逻辑立即加入队列，从而导致一帧处理过多
            // 比如map收到发给unit的消息，大部分消息都是同步的，假如一帧收到1000个同步消息，那么之前这里一帧就会处理1000次
            int count = self.nextFrameRun.Count;
            for (int i = 0; i < count; i++)
            {                
                (long coroutineLockType, long key, int level) = self.nextFrameRun.Dequeue();
                self.Notify(coroutineLockType, key, level);
            }       
        }

        internal static void RunNextCoroutine(this CoroutineLockComponent self, long coroutineLockType, long key, int level)
        {
            // 一个协程队列一帧处理超过100个,说明比较多了,打个warning,检查一下是否够正常
            if (level == 100)
            {
                Log.Warning($"too much coroutine level: {coroutineLockType} {key}");
            }

            self.nextFrameRun.Enqueue((coroutineLockType, key, level));
        }

        private static CoroutineLockQueueType Get(this CoroutineLockComponent self, long coroutineLockType)
        {
            return self.GetChild<CoroutineLockQueueType>(coroutineLockType) ?? self.AddChildWithId<CoroutineLockQueueType>(coroutineLockType);
        }

        /// <summary>
        /// 控制并发执行数量
        /// </summary>
        public static void SetMaxConcurrency(this CoroutineLockComponent self, long coroutineLockType, long key, int maxConcurrency)
        {
            CoroutineLockQueueType coroutineLockQueueType = self.Get(coroutineLockType);
            coroutineLockQueueType.SetMaxConcurrency(key, maxConcurrency);
        }

        /// <summary>
        /// 等待协程锁，带超时参数
        /// </summary>
        /// <param name="self">协程锁组件</param>
        /// <param name="coroutineLockType">锁类型</param>
        /// <param name="key">锁键值</param>
        /// <param name="timeout">协程锁占用时间，超时则自动释放</param>
        /// <param name="line"></param>
        /// <param name="filePath"></param>
        /// <returns>协程锁引用</returns>
        public static async ETTask<EntityRef<CoroutineLock>> Wait(this CoroutineLockComponent self, long coroutineLockType, long key, int timeout = 30000, [CallerLineNumber] int line = 0, [CallerFilePath] string filePath = "")
        {
            if (timeout < 0)
            {
                timeout = 30000;
            }
            
            CoroutineLockQueueType coroutineLockQueueType = self.Get(coroutineLockType);
            return await coroutineLockQueueType.Wait(key, timeout, line, filePath);
        }

        private static void Notify(this CoroutineLockComponent self, long coroutineLockType, long key, int level)
        {
            CoroutineLockQueueType coroutineLockQueueType = self.GetChild<CoroutineLockQueueType>(coroutineLockType);
            if (coroutineLockQueueType == null)
            {
                return;
            }
            coroutineLockQueueType.Notify(key, level);
        }
    }
}