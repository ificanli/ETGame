using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 匹配队列组件，挂在 Scene 上
    /// 按游戏模式管理多个匹配队列
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MatchQueueComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 游戏模式 -> 该模式需要的玩家人数
        /// 从配置表加载，不 hard code
        /// </summary>
        public Dictionary<int, int> ModePlayerCountDict;

        /// <summary>
        /// 玩家去重索引：PlayerId -> MatchRequest 的 EntityId
        /// 防止同一玩家重复排队
        /// </summary>
        public Dictionary<long, long> PlayerRequestDict;

        /// <summary>
        /// 匹配超时时间（毫秒），默认 30000
        /// </summary>
        public long MatchTimeoutMs;

        /// <summary>
        /// 定时器 Id（用于取消定时器）
        /// </summary>
        public long TimerId;
    }
}
