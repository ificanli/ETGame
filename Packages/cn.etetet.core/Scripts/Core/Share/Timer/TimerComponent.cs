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
}