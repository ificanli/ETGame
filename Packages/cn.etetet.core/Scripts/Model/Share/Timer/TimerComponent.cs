using System;
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

    public class TimerAction: DisposeObject, IPool
    {
        public static TimerAction Create(TimerClass timerClass, long startTime, long time, int type, object obj)
        {
            TimerAction self = ObjectPool.Fetch<TimerAction>();
            self.TimerClass = timerClass;
            self.StartTime = startTime;
            self.Object = obj;
            self.Time = time;
            self.Type = type;
            return self;
        }

        public override void Dispose()
        {
            this.TimerClass = TimerClass.None;
            this.StartTime = 0;
            this.Type = 0;
            this.Time = 0;
            
            if (this.Object is ValueTypeWrap<EntityRef<Entity>> wrap)
            {
                wrap.Dispose();
            }
            this.Object = null;
            
            ObjectPool.Recycle(this);
        }

        public Entity GetEntity()
        {
            var wrap = (ValueTypeWrap<EntityRef<Entity>>)this.Object;
            return wrap.Value;
        }

        public TimerClass TimerClass;
        
        public int Type;

        public object Object;

        public long StartTime;

        public long Time;
        
        public bool IsFromPool { get; set; }
    }

    public struct TimerCallback
    {
        public Entity Args;
    }

    [SkipAwaitEntityCheck]
    [ComponentOf(typeof(Scene))]
    public class TimerComponent: Entity, IAwake, IUpdate
    {
        /// <summary>
        /// key: time, value: timer id
        /// </summary>
        public readonly MultiMap<long, long> timeId = new();

        public readonly Queue<long> timeOutTime = new();

        public readonly Queue<long> timeOutTimerIds = new();

        public readonly Dictionary<long, TimerAction> timerActions = new();

        public long idGenerator;

        // 记录最小时间，不用每次都去MultiMap取第一个值
        public long minTime = long.MaxValue;
    }
}