namespace ET.Server
{
    /// <summary>
    /// 战斗状态组件。跟踪玩家的战斗/脱战状态。
    /// 用于猎杀者"脱战后隐身"等效果。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CombatStateComponent : Entity, IAwake, IDestroy
    {
        /// <summary>最后一次造成伤害的时间（毫秒）</summary>
        public long LastDealDamageTime { get; set; }

        /// <summary>最后一次受到伤害的时间（毫秒）</summary>
        public long LastTakeDamageTime { get; set; }

        /// <summary>脱战延迟（毫秒），默认 5000ms</summary>
        public int OutOfCombatDelayMs { get; set; } = 5000;

        /// <summary>当前是否处于战斗状态</summary>
        public bool InCombat { get; set; }

        /// <summary>脱战检测定时器 ID</summary>
        public long CheckTimerId { get; set; }
    }

    /// <summary>
    /// 玩家跑局时限组件。到达截止时间后触发死亡结算。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class RunTimeLimitComponent : Entity, IAwake<long, string>, IDestroy
    {
        /// <summary>计时开始时间（毫秒）</summary>
        public long StartTime { get; set; }

        /// <summary>计时持续时长（毫秒）</summary>
        public long DurationMs { get; set; }

        /// <summary>死亡触发时间（毫秒时间戳）</summary>
        public long DeadlineTime { get; set; }

        /// <summary>当前地图名，仅用于日志</summary>
        public string MapName { get; set; }

        /// <summary>超时一次性定时器 ID</summary>
        public long TimerId { get; set; }
    }
}
