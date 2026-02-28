namespace ET
{
    /// <summary>
    /// 匹配请求，MatchQueueComponent 的子 Entity
    /// 每个请求代表一个正在排队的玩家
    /// </summary>
    [ChildOf(typeof(MatchQueueComponent))]
    public class MatchRequest : Entity, IAwake<long, int>, IDestroy
    {
        /// <summary>
        /// 玩家 ID
        /// </summary>
        public long PlayerId;

        /// <summary>
        /// 游戏模式
        /// </summary>
        public int GameMode;

        /// <summary>
        /// 入队时间戳（毫秒）
        /// </summary>
        public long EnqueueTime;

        /// <summary>
        /// 匹配状态，见 MatchState
        /// </summary>
        public int State;
    }
}
