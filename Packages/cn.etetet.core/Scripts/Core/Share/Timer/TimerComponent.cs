using System.Collections.Generic;

namespace ET
{
    public enum TimerClass
    {
        None,
        OnceTimer,
        OnceWaitTimer,
        RepeatedTimer,
    }

    [ChildOf(typeof(TimerComponent))]
    public class TimerAction: Entity, IAwake, IDestroy
    {
        public TimerClass TimerClass;
        
        public int Type;

        public object Object;

        public long StartTime;

        public long Time;
    }

    public struct TimerCallback
    {
        public EntityRef<Entity> Args;
    }

    public static class HighFrequencyChannelId
    {
        public const int Move16ms = 1;
        public const int Bullet33ms = 2;
    }

    public static class HighFrequencyInvokeType
    {
        // 1200~1299 预留给 core 高频调度 invoke。
        public const int Move16msTick = 1200;
        public const int Move16msRemoved = 1201;
        public const int Bullet33msTick = 1202;
    }

    public struct HighFrequencyChannelConfig
    {
        public int ChannelId;
        public int IntervalMs;
        public int MaxCatchUpCount;
        public int TickInvokeType;
        public int RemovedInvokeType;
        public int WarningBudgetMs;
    }

    public static class HighFrequencyChannelConfigFactory
    {
        public static HighFrequencyChannelConfig CreateMove16ms()
        {
            return new HighFrequencyChannelConfig
            {
                ChannelId = HighFrequencyChannelId.Move16ms,
                IntervalMs = 16,
                MaxCatchUpCount = 3,
                TickInvokeType = HighFrequencyInvokeType.Move16msTick,
                RemovedInvokeType = HighFrequencyInvokeType.Move16msRemoved,
                WarningBudgetMs = 8,
            };
        }

        public static HighFrequencyChannelConfig CreateBullet33ms()
        {
            return new HighFrequencyChannelConfig
            {
                ChannelId = HighFrequencyChannelId.Bullet33ms,
                IntervalMs = 33,
                MaxCatchUpCount = 2,
                TickInvokeType = HighFrequencyInvokeType.Bullet33msTick,
                RemovedInvokeType = 0,
                WarningBudgetMs = 16,
            };
        }
    }

    public struct HighFrequencyTickCallback
    {
        public EntityRef<Entity> Entity;
        public int ChannelId;
        public long DeltaTimeMs;
        public float DeltaTimeSeconds;
        public long TickIndex;
        public long NowMs;
    }

    public struct HighFrequencyEntityRemovedCallback
    {
        public EntityRef<Entity> Entity;
        public int ChannelId;
        public long NowMs;
        public bool IsDeferredCommit;
    }

    public partial class Scene
    {
        private EntityRef<TimerComponent> timerComponent;

        public TimerComponent TimerComponent
        {
            get
            {
                return this.timerComponent;
            }
            set
            {
                this.timerComponent = value;
            }
        }
    }

    [DisableGetComponent]
    [SkipAwaitEntityCheck]
    [ComponentOf(typeof(Scene))]
    public class TimerComponent: Entity, IAwake, IUpdate
    {
        /// <summary>
        /// key: time, value: timer id
        /// </summary>
        public readonly MultiMap<long, long> timeId = new(1000);

        public readonly Queue<long> timeOutTime = new();

        public readonly Queue<long> timeOutTimerIds = new();
        
        // 记录最小时间，不用每次都去MultiMap取第一个值
        public long minTime = long.MaxValue;
    }

    [ChildOf(typeof(HighFrequencySchedulerComponent))]
    public class HighFrequencyChannelComponent : Entity, IAwake<HighFrequencyChannelConfig>, IDestroy
    {
        public int ChannelId;
        public int IntervalMs;
        public int MaxCatchUpCount;
        public int TickInvokeType;
        public int RemovedInvokeType;
        public int WarningBudgetMs;
        public long AccumulatorMs;
        public long LastUpdateTime;
        public long TickIndex;
        public long TotalTickCount;
        public long DroppedCatchUpCount;
        public long OverBudgetCount;
        public long LastFrameCostMs;
        public long TotalCostMs;
        public int LastFrameTickCount;
        public bool IsTicking;
        public Dictionary<long, EntityRef<Entity>> ActiveEntities { get; set; } = new();
        public HashSet<long> PendingRemoveEntityIds { get; set; } = new();
    }

    [SkipAwaitEntityCheck]
    [ComponentOf(typeof(Scene))]
    public class HighFrequencySchedulerComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public int ActiveChannelCount;
        public long LastUpdateTime;
        public long TotalTickCount;
    }
}
