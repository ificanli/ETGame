using System;

namespace ET
{
    [EntitySystemOf(typeof(CoroutineLock))]
    public static partial class CoroutineLockSystem
    {
        [EntitySystem]
        private static void Awake(this CoroutineLock self, long type, long k, int count)
        {
            self.type = type;
            self.key = k;
            self.level = count;
            self.timerId = 0;
        }
        
        [EntitySystem]
        private static void Destroy(this CoroutineLock self)
        {
            // 清理超时计时器
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }

            if (self.timerId != 0)
            {
                TimerComponent timerComponent = root.GetComponent<TimerComponent>();
                timerComponent?.Remove(ref self.timerId);
            }
            
            self.Scene<CoroutineLockComponent>().RunNextCoroutine(self.type, self.key, self.level + 1);
            self.type = 0;
            self.key = 0;
            self.level = 0;
        }
                
        /// <summary>
        /// 超时处理任务
        /// </summary>
        internal static async ETTask SetTimeout(this CoroutineLock self, int timeout, int line, string filePath)
        {
            // 在await前创建EntityRef
            EntityRef<CoroutineLock> selfRef = self;
            long type = self.type;
            long key = self.key;
            int level = self.level;
            
            TimerComponent timerComponent = self.Root().GetComponent<TimerComponent>();
            await timerComponent.WaitAsync(timeout);
            
            // await后通过EntityRef重新获取Entity
            self = selfRef;
            
            // 超时后检查锁是否还存在
            if (self != null)
            {
                Log.Error($"{filePath}:{line} Coroutine lock timeout after {timeout}ms, type: {type}, key: {key}, level: {level}");
                self.Dispose();
            }
        }
    }
}