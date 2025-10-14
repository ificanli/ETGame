using System;
using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(TimerAction))]
    public static partial class TimerActionSystem
    {
        [EntitySystem]
        private static void Awake(this TimerAction self)
        {
        }

        [EntitySystem]
        private static void Destroy(this TimerAction self)
        {
            self.TimerClass = TimerClass.None;
            self.StartTime = 0;
            self.Time = 0;
            self.Type = 0;

            if (self.Object is IDisposable disposable)
            {
                disposable.Dispose();
            }

            self.Object = null;
        }

        public static Entity GetEntity(this TimerAction self)
        {
            var wrap = (ValueTypeWrap<EntityRef<Entity>>)self.Object;
            return wrap.Value;
        }
    }

    [EntitySystemOf(typeof(TimerComponent))]
    public static partial class TimerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this TimerComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this TimerComponent self)
        {
            if (self.timeId.Count == 0)
            {
                return;
            }

            long timeNow = self.GetNow();

            if (timeNow < self.minTime)
            {
                return;
            }

            foreach (var kv in self.timeId)
            {
                long k = kv.Key;
                if (k > timeNow)
                {
                    self.minTime = k;
                    break;
                }

                self.timeOutTime.Enqueue(k);
            }

            while (self.timeOutTime.Count > 0)
            {
                long time = self.timeOutTime.Dequeue();
                List<long> list = self.timeId[time];
                for (int i = 0; i < list.Count; ++i)
                {
                    long timerId = list[i];
                    self.timeOutTimerIds.Enqueue(timerId);
                }

                self.timeId.Remove(time);
            }

            if (self.timeId.Count == 0)
            {
                self.minTime = long.MaxValue;
            }

            while (self.timeOutTimerIds.Count > 0)
            {
                long timerId = self.timeOutTimerIds.Dequeue();
                self.Run(timerId);
            }
        }

        private static long GetNow(this TimerComponent self)
        {
            return TimeInfo.Instance.ServerNow();
        }

        private static void Run(this TimerComponent self, long timerId)
        {
            TimerAction timerAction = self.GetChild<TimerAction>(timerId);
            if (timerAction == null)
            {
                return;
            }

            switch (timerAction.TimerClass)
            {
                case TimerClass.OnceTimer:
                {
                    Entity entity = timerAction.GetEntity();
                    int timerActionType = timerAction.Type;
                    self.RemoveChild(timerId);
                    EventSystem.Instance.Invoke(timerActionType, new TimerCallback() { Args = entity });
                    break;
                }
                case TimerClass.OnceWaitTimer:
                {
                    ETTask tcs = timerAction.Object as ETTask;
                    self.RemoveChild(timerId);
                    tcs.SetResult();
                    break;
                }
                case TimerClass.RepeatedTimer:
                {
                    int timerActionType = timerAction.Type;
                    Entity entity = timerAction.GetEntity();

                    timerAction.StartTime = self.GetNow();
                    self.AddTimer(timerAction);

                    EventSystem.Instance.Invoke(timerActionType, new TimerCallback() { Args = entity });
                    break;
                }
            }
        }

        private static TimerAction CreateTimerAction(this TimerComponent self, TimerClass timerClass, long startTime, long time, int type, object obj)
        {
            TimerAction timer = self.AddChild<TimerAction>(true);
            timer.TimerClass = timerClass;
            timer.StartTime = startTime;
            timer.Object = obj;
            timer.Time = time;
            timer.Type = type;

            self.AddTimer(timer);
            return timer;
        }

        private static void AddTimer(this TimerComponent self, TimerAction timer)
        {
            long tillTime = timer.StartTime + timer.Time;
            self.timeId.Add(tillTime, timer.Id);
            if (tillTime < self.minTime)
            {
                self.minTime = tillTime;
            }
        }

        public static bool Remove(this TimerComponent self, ref long id)
        {
            long i = id;
            id = 0;
            return self.Remove(i);
        }

        private static bool Remove(this TimerComponent self, long id)
        {
            if (id == 0)
            {
                return false;
            }

            return self.RemoveChild(id);
        }

        public static async ETTask WaitTillAsync(this TimerComponent self, long tillTime)
        {
            long timeNow = self.GetNow();
            if (timeNow >= tillTime)
            {
                return;
            }

            ETTask tcs = ETTask.Create(true);
            TimerAction timer = self.CreateTimerAction(TimerClass.OnceWaitTimer, timeNow, tillTime - timeNow, 0, tcs);
            long timerId = timer.Id;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            try
            {
                cancellationToken?.Add(CancelAction);
                await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }

            return;

            void CancelAction()
            {
                if (!self.Remove(timerId))
                {
                    return;
                }

                tcs.SetResult();
            }
        }

        public static async ETTask WaitFrameAsync(this TimerComponent self)
        {
            await self.WaitAsync(1);
        }

        public static async ETTask WaitAsync(this TimerComponent self, long time)
        {
            if (time == 0)
            {
                return;
            }

            long timeNow = self.GetNow();

            ETTask tcs = ETTask.Create(true);
            TimerAction timer = self.CreateTimerAction(TimerClass.OnceWaitTimer, timeNow, time, 0, tcs);
            long timerId = timer.Id;

            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            try
            {
                cancellationToken?.Add(CancelAction);
                await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);
            }

            return;

            void CancelAction()
            {
                if (!self.Remove(timerId))
                {
                    return;
                }

                tcs.SetResult();
            }
        }

        // 用这个优点是可以热更，缺点是回调式的写法，逻辑不连贯。WaitTillAsync不能热更，优点是逻辑连贯。
        // wait时间短并且逻辑需要连贯的建议WaitTillAsync
        // wait时间长不需要逻辑连贯的建议用NewOnceTimer
        public static long NewOnceTimer(this TimerComponent self, long tillTime, int type, Entity args)
        {
            long timeNow = self.GetNow();
            if (tillTime < timeNow)
            {
                Log.Error($"new once time too small: {tillTime}");
            }

            EntityRef<Entity> entityRef = args;
            ValueTypeWrap<EntityRef<Entity>> wrap = ValueTypeWrap<EntityRef<Entity>>.Create(entityRef);
            TimerAction timer = self.CreateTimerAction(TimerClass.OnceTimer, timeNow, tillTime - timeNow, type, wrap);
            return timer.Id;
        }

        public static long NewFrameTimer(this TimerComponent self, int type, Entity args)
        {
#if DOTNET
            return self.NewRepeatedTimerInner(100, type, args);
#else
            return self.NewRepeatedTimerInner(0, type, args);
#endif
        }

        /// <summary>
        /// 创建一个RepeatedTimer
        /// </summary>
        private static long NewRepeatedTimerInner(this TimerComponent self, long time, int type, Entity args)
        {
#if DOTNET
            if (time < 100)
            {
                throw new Exception($"repeated timer < 100, timerType: time: {time}");
            }
#endif

            long timeNow = self.GetNow();
            EntityRef<Entity> entityRef = args;
            ValueTypeWrap<EntityRef<Entity>> wrap = ValueTypeWrap<EntityRef<Entity>>.Create(entityRef);
            TimerAction timer = self.CreateTimerAction(TimerClass.RepeatedTimer, timeNow, time, type, wrap);
            return timer.Id;
        }

        public static long NewRepeatedTimer(this TimerComponent self, long time, int type, Entity args)
        {
            if (time < 100)
            {
                Log.Error($"time too small: {time}");
                return 0;
            }

            return self.NewRepeatedTimerInner(time, type, args);
        }
    }
}
