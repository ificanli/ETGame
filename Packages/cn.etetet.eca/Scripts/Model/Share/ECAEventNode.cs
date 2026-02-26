using System;

namespace ET
{
    /// <summary>
    /// ECA 事件节点
    /// 表示触发状态转换的事件
    /// </summary>
    [Serializable]
    [EnableClass]
    public class ECAEventNode : ECANode
    {
        /// <summary>
        /// 事件类型
        /// 1=OnPlayerEnter, 2=OnPlayerLeave, 3=OnPlayerInteract, 4=OnTimerTick, 5=OnMonsterDead
        /// </summary>
        public int EventType;

        /// <summary>
        /// 事件参数（JSON格式）
        /// </summary>
        public string EventParams;
    }
}
