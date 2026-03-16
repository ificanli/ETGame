using ET;

namespace ET.Server
{
    /// <summary>
    /// ECA 流程图计时器上下文
    /// </summary>
    [ChildOf(typeof(Scene))]
    public class ECAFlowTimerComponent : Entity, IAwake<string, string, long>
    {
        /// <summary>
        /// 计时器标识
        /// </summary>
        public string TimerId { get; set; }

        /// <summary>
        /// 计时器唯一键（同一个timer_id按玩家隔离）
        /// </summary>
        public string TimerKey { get; set; }

        /// <summary>
        /// 点位 UnitId
        /// </summary>
        public long PointUnitId { get; set; }

        /// <summary>
        /// 触发玩家 UnitId
        /// </summary>
        public long PlayerUnitId { get; set; }

        /// <summary>
        /// TimerAction Id
        /// </summary>
        public long TimerActionId { get; set; }
    }
}
