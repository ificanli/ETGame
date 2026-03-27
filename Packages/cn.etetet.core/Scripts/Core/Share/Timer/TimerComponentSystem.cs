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

        internal static Entity GetEntity(this TimerAction self)
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
            self.GetParent<Scene>().TimerComponent = self;
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

                    if (entity != null)
                    {
                        EventSystem.Instance.Invoke(timerActionType, new TimerCallback() { Args = entity });
                    }                    
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

                    if (entity != null)
                    {
                        timerAction.StartTime = self.GetNow();
                        self.AddTimer(timerAction);
                        EventSystem.Instance.Invoke(timerActionType, new TimerCallback() { Args = entity });
                    }
                    else
                    {
                        self.RemoveChild(timerId);
                    }
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
            if (time < 50)
            {
                throw new Exception($"repeated timer < 50, timerType: time: {time}");
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
            return self.NewRepeatedTimerInner(time, type, args);
        }
    }

    [EntitySystemOf(typeof(HighFrequencySchedulerComponent))]
    public static partial class HighFrequencySchedulerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HighFrequencySchedulerComponent self)
        {
            self.ActiveChannelCount = 0;
            self.LastUpdateTime = TimeInfo.Instance.ServerNow();
            self.TotalTickCount = 0;
        }

        [EntitySystem]
        private static void Destroy(this HighFrequencySchedulerComponent self)
        {
            self.ActiveChannelCount = 0;
            self.LastUpdateTime = 0;
            self.TotalTickCount = 0;
        }

        [EntitySystem]
        private static void Update(this HighFrequencySchedulerComponent self)
        {
            if (self.Children == null || self.Children.Count == 0)
            {
                return;
            }

            long nowMs = TimeInfo.Instance.ServerNow();
            if (self.LastUpdateTime == 0)
            {
                self.LastUpdateTime = nowMs;
                return;
            }

            long elapsedMs = nowMs - self.LastUpdateTime;
            if (elapsedMs <= 0)
            {
                return;
            }

            self.LastUpdateTime = nowMs;

            foreach (Entity child in self.Children.Values)
            {
                if (child is not HighFrequencyChannelComponent channel)
                {
                    continue;
                }

                self.UpdateChannel(channel, nowMs, elapsedMs);
            }
        }

        public static HighFrequencyChannelComponent RegisterChannel(this HighFrequencySchedulerComponent self, HighFrequencyChannelConfig config)
        {
            self.ValidateChannelConfig(config);

            HighFrequencyChannelComponent existing = self.GetChild<HighFrequencyChannelComponent>(config.ChannelId);
            if (existing != null)
            {
                self.ValidateChannelMatch(existing, config);
                return existing;
            }

            return self.AddChildWithId<HighFrequencyChannelComponent, HighFrequencyChannelConfig>(config.ChannelId, config);
        }

        public static bool AddEntity(this HighFrequencySchedulerComponent self, int channelId, Entity entity)
        {
            if (!self.TryGetChannel(channelId, out HighFrequencyChannelComponent channel) || entity == null || entity.IsDisposed)
            {
                return false;
            }

            Scene scene = self.GetParent<Scene>();
            if (entity.Scene() != scene)
            {
                Log.Warning($"[HighFreq] reject cross-scene add. channel={channelId}, entityId={entity.Id}, scene={scene?.Name}, entityScene={entity.Scene()?.Name}");
                return false;
            }

            int oldCount = channel.ActiveEntities.Count;
            channel.ActiveEntities[entity.Id] = entity;
            channel.PendingRemoveEntityIds.Remove(entity.Id);

            if (oldCount == 0 && channel.ActiveEntities.Count > 0)
            {
                channel.AccumulatorMs = 0;
            }

            self.UpdateActiveChannelCount(oldCount, channel.ActiveEntities.Count);
            return true;
        }

        public static bool RequestRemoveEntity(this HighFrequencySchedulerComponent self, int channelId, Entity entity, out bool deferred)
        {
            deferred = false;
            if (entity == null)
            {
                return false;
            }

            if (!self.TryGetChannel(channelId, out HighFrequencyChannelComponent channel))
            {
                return false;
            }

            if (!channel.ActiveEntities.ContainsKey(entity.Id))
            {
                return false;
            }

            if (channel.IsTicking)
            {
                channel.PendingRemoveEntityIds.Add(entity.Id);
                deferred = true;
                return true;
            }

            int oldCount = channel.ActiveEntities.Count;
            channel.PendingRemoveEntityIds.Remove(entity.Id);
            channel.ActiveEntities.Remove(entity.Id);
            self.UpdateActiveChannelCount(oldCount, channel.ActiveEntities.Count);
            if (channel.ActiveEntities.Count == 0)
            {
                channel.AccumulatorMs = 0;
            }

            return true;
        }

        public static bool IsEntityRegistered(this HighFrequencySchedulerComponent self, int channelId, Entity entity)
        {
            return entity != null &&
                !entity.IsDisposed &&
                self.TryGetChannel(channelId, out HighFrequencyChannelComponent channel) &&
                channel.ActiveEntities.ContainsKey(entity.Id) &&
                !channel.PendingRemoveEntityIds.Contains(entity.Id);
        }

        private static void UpdateChannel(this HighFrequencySchedulerComponent self, HighFrequencyChannelComponent channel, long nowMs, long elapsedMs)
        {
            channel.LastUpdateTime = nowMs;

            if (channel.PendingRemoveEntityIds.Count > 0 && !channel.IsTicking)
            {
                self.CommitPendingRemoves(channel, nowMs, false);
            }

            if (channel.ActiveEntities.Count == 0)
            {
                channel.AccumulatorMs = 0;
                channel.LastFrameCostMs = 0;
                channel.LastFrameTickCount = 0;
                return;
            }

            channel.AccumulatorMs += elapsedMs;
            if (channel.AccumulatorMs < channel.IntervalMs)
            {
                channel.LastFrameCostMs = 0;
                channel.LastFrameTickCount = 0;
                return;
            }

            long frameStartMs = TimeInfo.Instance.ServerNow();
            int frameTickCount = 0;
            using ListComponent<long> invalidEntityIds = ListComponent<long>.Create();

            channel.IsTicking = true;
            while (channel.AccumulatorMs >= channel.IntervalMs && frameTickCount < channel.MaxCatchUpCount)
            {
                ++channel.TickIndex;
                ++channel.TotalTickCount;
                ++self.TotalTickCount;
                ++frameTickCount;

                foreach (KeyValuePair<long, EntityRef<Entity>> kv in channel.ActiveEntities)
                {
                    if (channel.PendingRemoveEntityIds.Contains(kv.Key))
                    {
                        continue;
                    }

                    Entity entity = kv.Value;
                    if (entity == null || entity.IsDisposed)
                    {
                        invalidEntityIds.Add(kv.Key);
                        continue;
                    }

                    EventSystem.Instance.TryInvoke(
                        channel.TickInvokeType,
                        new HighFrequencyTickCallback
                        {
                            Entity = kv.Value,
                            ChannelId = channel.ChannelId,
                            DeltaTimeMs = channel.IntervalMs,
                            DeltaTimeSeconds = channel.IntervalMs / 1000f,
                            TickIndex = channel.TickIndex,
                            NowMs = nowMs,
                        });
                }

                channel.AccumulatorMs -= channel.IntervalMs;
            }

            channel.IsTicking = false;

            foreach (long entityId in invalidEntityIds)
            {
                channel.PendingRemoveEntityIds.Add(entityId);
            }

            if (channel.PendingRemoveEntityIds.Count > 0)
            {
                self.CommitPendingRemoves(channel, nowMs, true);
            }

            if (channel.AccumulatorMs >= channel.IntervalMs)
            {
                long droppedTickCount = channel.AccumulatorMs / channel.IntervalMs;
                channel.AccumulatorMs %= channel.IntervalMs;
                channel.DroppedCatchUpCount += droppedTickCount;
                Log.Warning(
                    $"[HighFreq][Drop] channel={channel.ChannelId}, droppedTicks={droppedTickCount}, active={channel.ActiveEntities.Count}, maxCatchUp={channel.MaxCatchUpCount}");
            }

            long frameCostMs = TimeInfo.Instance.ServerNow() - frameStartMs;
            channel.LastFrameCostMs = frameCostMs;
            channel.TotalCostMs += frameCostMs;
            channel.LastFrameTickCount = frameTickCount;

            if (frameCostMs >= channel.WarningBudgetMs)
            {
                ++channel.OverBudgetCount;
                Log.Warning(
                    $"[HighFreq][Budget] channel={channel.ChannelId}, frameCostMs={frameCostMs}, budgetMs={channel.WarningBudgetMs}, ticks={frameTickCount}, active={channel.ActiveEntities.Count}");
            }
        }

        private static void CommitPendingRemoves(this HighFrequencySchedulerComponent self, HighFrequencyChannelComponent channel, long nowMs, bool isDeferredCommit)
        {
            if (channel.PendingRemoveEntityIds.Count == 0)
            {
                return;
            }

            int oldCount = channel.ActiveEntities.Count;
            using ListComponent<long> toRemove = ListComponent<long>.Create();
            toRemove.AddRange(channel.PendingRemoveEntityIds);

            foreach (long entityId in toRemove)
            {
                if (!channel.ActiveEntities.TryGetValue(entityId, out EntityRef<Entity> entityRef))
                {
                    continue;
                }

                if (channel.RemovedInvokeType != 0)
                {
                    EventSystem.Instance.TryInvoke(
                        channel.RemovedInvokeType,
                        new HighFrequencyEntityRemovedCallback
                        {
                            Entity = entityRef,
                            ChannelId = channel.ChannelId,
                            NowMs = nowMs,
                            IsDeferredCommit = isDeferredCommit,
                        });
                }

                channel.ActiveEntities.Remove(entityId);
            }

            channel.PendingRemoveEntityIds.Clear();
            self.UpdateActiveChannelCount(oldCount, channel.ActiveEntities.Count);
            if (channel.ActiveEntities.Count == 0)
            {
                channel.AccumulatorMs = 0;
            }
        }

        private static bool TryGetChannel(this HighFrequencySchedulerComponent self, int channelId, out HighFrequencyChannelComponent channel)
        {
            channel = self.GetChild<HighFrequencyChannelComponent>(channelId);
            return channel != null;
        }

        private static void UpdateActiveChannelCount(this HighFrequencySchedulerComponent self, int oldCount, int newCount)
        {
            if (oldCount == 0 && newCount > 0)
            {
                ++self.ActiveChannelCount;
            }
            else if (oldCount > 0 && newCount == 0)
            {
                --self.ActiveChannelCount;
            }
        }

        private static void ValidateChannelConfig(this HighFrequencySchedulerComponent self, HighFrequencyChannelConfig config)
        {
            if (config.ChannelId <= 0)
            {
                throw new Exception("high frequency channel id must be > 0");
            }

            if (config.IntervalMs <= 0)
            {
                throw new Exception($"high frequency channel interval invalid: {config.IntervalMs}");
            }

            if (config.MaxCatchUpCount <= 0)
            {
                throw new Exception($"high frequency channel catch up invalid: {config.MaxCatchUpCount}");
            }

            if (config.TickInvokeType == 0)
            {
                throw new Exception($"high frequency channel tick invoke type invalid: channel={config.ChannelId}");
            }

            if (config.WarningBudgetMs <= 0)
            {
                throw new Exception($"high frequency channel warning budget invalid: channel={config.ChannelId}, budget={config.WarningBudgetMs}");
            }
        }

        private static void ValidateChannelMatch(this HighFrequencySchedulerComponent self, HighFrequencyChannelComponent existing, HighFrequencyChannelConfig config)
        {
            if (existing.IntervalMs != config.IntervalMs ||
                existing.MaxCatchUpCount != config.MaxCatchUpCount ||
                existing.TickInvokeType != config.TickInvokeType ||
                existing.RemovedInvokeType != config.RemovedInvokeType ||
                existing.WarningBudgetMs != config.WarningBudgetMs)
            {
                throw new Exception(
                    $"high frequency channel config mismatch: channel={config.ChannelId}, existingInterval={existing.IntervalMs}, newInterval={config.IntervalMs}");
            }
        }
    }

    [EntitySystemOf(typeof(HighFrequencyChannelComponent))]
    public static partial class HighFrequencyChannelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HighFrequencyChannelComponent self, HighFrequencyChannelConfig config)
        {
            self.ChannelId = config.ChannelId;
            self.IntervalMs = config.IntervalMs;
            self.MaxCatchUpCount = config.MaxCatchUpCount;
            self.TickInvokeType = config.TickInvokeType;
            self.RemovedInvokeType = config.RemovedInvokeType;
            self.WarningBudgetMs = config.WarningBudgetMs;
            self.AccumulatorMs = 0;
            self.LastUpdateTime = 0;
            self.TickIndex = 0;
            self.TotalTickCount = 0;
            self.DroppedCatchUpCount = 0;
            self.OverBudgetCount = 0;
            self.LastFrameCostMs = 0;
            self.TotalCostMs = 0;
            self.LastFrameTickCount = 0;
            self.IsTicking = false;
            self.ActiveEntities.Clear();
            self.PendingRemoveEntityIds.Clear();
        }

        [EntitySystem]
        private static void Destroy(this HighFrequencyChannelComponent self)
        {
            self.ChannelId = 0;
            self.IntervalMs = 0;
            self.MaxCatchUpCount = 0;
            self.TickInvokeType = 0;
            self.RemovedInvokeType = 0;
            self.WarningBudgetMs = 0;
            self.AccumulatorMs = 0;
            self.LastUpdateTime = 0;
            self.TickIndex = 0;
            self.TotalTickCount = 0;
            self.DroppedCatchUpCount = 0;
            self.OverBudgetCount = 0;
            self.LastFrameCostMs = 0;
            self.TotalCostMs = 0;
            self.LastFrameTickCount = 0;
            self.IsTicking = false;
            self.ActiveEntities.Clear();
            self.PendingRemoveEntityIds.Clear();
        }
    }
}
