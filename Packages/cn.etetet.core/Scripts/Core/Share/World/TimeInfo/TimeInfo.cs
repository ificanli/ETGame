using System;

namespace ET
{
    public class TimeInfo: Singleton<TimeInfo>, ISingletonAwake
    {
        private int timeZone;
        
        public int TimeZone
        {
            get
            {
                return this.timeZone;
            }
            set
            {
                this.timeZone = value;
            }
        }
        
        private DateTime dt1970;
        private long tick1970;
        
        // ping消息会设置该值，原子操作
        public long ServerMinusClientTime { private get; set; }

        public long FrameTime { get; private set; }
        
        public void Awake()
        {
            this.dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.tick1970 = GetTick() - (DateTime.UtcNow.Ticks - this.dt1970.Ticks) / 10000;
            this.FrameTime = this.ClientNow();
        }

        public void Update()
        {
            // 赋值long型是原子操作，线程安全
            this.FrameTime = this.ClientNow();
        }

        /// <summary>
        /// 返回毫秒数（跨平台，无状态，超长运行）
        /// </summary>
        private static long GetTick()
        {
#if UNITY
            // Unity 下使用 Stopwatch.GetTimestamp()
            long ticks = System.Diagnostics.Stopwatch.GetTimestamp();
            // 换算为毫秒
            return ticks * 1000 / System.Diagnostics.Stopwatch.Frequency;
#else
            // 服务端直接用 TickCount64
            return Environment.TickCount64;
#endif
        }
        
        // 线程安全
        public long ClientNow()
        {
            return GetTick() - this.tick1970;
        }
        
        public long ServerNow()
        {
            return ClientNow() + this.ServerMinusClientTime;
        }
        
        public long ClientFrameTime()
        {
            return this.FrameTime;
        }
        
        public long ServerFrameTime()
        {
            return this.FrameTime + this.ServerMinusClientTime;
        }
    }
}