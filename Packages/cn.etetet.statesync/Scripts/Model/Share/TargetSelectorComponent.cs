namespace ET
{
    /// <summary>
    /// 目标选择组件，挂载在 Unit 上，负责自动索敌、当前目标续锁校验和遮挡缓存。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class TargetSelectorComponent : Entity, IAwake, IDestroy
    {
        /// <summary>当前目标的Unit ID（0表示无目标）</summary>
        public long CurrentTargetId;

        /// <summary>上次选择目标的时间（毫秒）</summary>
        public long LastSelectTime;

        /// <summary>目标选择间隔（毫秒），默认1秒</summary>
        public int SelectIntervalMs;

        /// <summary>最大索敌范围（米）</summary>
        public float MaxRange;

        /// <summary>上次 LoS 校验时间（毫秒）</summary>
        public long LastLineOfSightCheckTime;

        /// <summary>上次 LoS 校验的目标 ID</summary>
        public long LastLineOfSightTargetId;

        /// <summary>上次 LoS 校验是否通过</summary>
        public bool LastLineOfSightPassed;

        /// <summary>当前目标连续被遮挡的次数，用于最小去抖</summary>
        public int ConsecutiveLineOfSightBlockedCount;
    }
}
